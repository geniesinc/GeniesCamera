using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Genies.Avatars;
using Genies.Refs;
using Genies.Utilities;
using Genies.Assets.Services;
using UMA;
using UMA.CharacterSystem;
using UnityEngine;

namespace Genies.Ugc
{
    /// <summary>
    /// Capable of building a UGC <see cref="OutfitAsset"/> for one specific LOD.
    /// </summary>
#if GENIES_INTERNAL
    [CreateAssetMenu(fileName = "LOD-UgcOutfitAssetBuilder", menuName = "Genies/LOD UGC OutfitAsset Builder")]
#endif
    public sealed class LodUgcOutfitAssetBuilder : ScriptableObject
    {
        public enum Mode
        {
            DontBake = 0, // one submesh per split using the MegaShader
            BakeSplits = 1, // one submesh per split using the MegaSimple (baked MegaShader), no merging from UMA
            MergeSplits = 2, // one submesh per wearable using the MegaSimple (bakes each split and merges them)
            MergeAll = 3, // one submesh for any equipped UGC wearable (bakes each split and merges all equipped wearables)
        }

        public string lod;
        public Mode mode;
        public Material material;
        public SplitTextureSettings splitTextureSettings;
        public Material postProcessingMaterial;
        public List<UmaMaterialExportMap> maps = new();
        public MaterialProperties mapExportingProperties = new();

        // dependencies
        private IAssetLoader<UgcTemplateAsset> _templateLoader;
        private IAssetLoader<UgcElementAsset> _elementLoader;
        private IMegaMaterialBuilder _megaMaterialBuilder;

        // state
        private UMAMaterial _umaMaterial;

        // helpers
        private readonly MaterialProperties _properties = new();
        private readonly Stack<TextureSettings> _mapTextureSettingsPool = new();

        public void Initialize(
            IAssetLoader<UgcTemplateAsset> templateLoader,
            IAssetLoader<UgcElementAsset> elementLoader,
            IMegaMaterialBuilder megaMaterialBuilder)
        {
            // initialize once
            if (_umaMaterial)
            {
                return;
            }

            _templateLoader = templateLoader;
            _elementLoader = elementLoader;
            _megaMaterialBuilder = megaMaterialBuilder;

            _umaMaterial = CreateInstance<UMAMaterial>();
            _umaMaterial.material = material;
            _umaMaterial.materialType = UMAMaterial.MaterialType.Atlas;
            _umaMaterial.channels = maps.Select(channel => channel.UmaChannel).ToArray();
            _umaMaterial.shaderParms = Array.Empty<UMAMaterial.ShaderParms>();

            if (material)
            {
                _umaMaterial.name = material.name;
            }
            else
            {
                _umaMaterial.name = "UgcMaterial";
            }
        }

        public async UniTask<OutfitAsset> BuildOutfitAssetAsync(string wearableId, Wearable wearable, OutfitAssetMetadata metadata)
        {
            if (wearable?.Splits is null)
            {
                return null;
            }

            // try to load the UGC template
            Ref<UgcTemplateAsset> templateRef = await _templateLoader.LoadAsync(wearable.TemplateId);
            if (!templateRef.IsAlive)
            {
                return null;
            }

            // create the build data object. It holds the data required to build the recipe and OutfitAsset and owns the references for all generated resources
            var buildData = new BuildData();

            // add the component creators from the template asset
            buildData.ComponentCreators.AddRange(templateRef.Item.ComponentCreators);

            // create the UMA material to be used by the splits based on current mode
            Ref<UMAMaterial> umaMaterialRef = mode switch
            {
                // each split will create its own material instance
                Mode.DontBake => default,
                Mode.BakeSplits => default,

                // build a new instance for all the splits
                Mode.MergeSplits => BuildUmaMaterial(wearable.TemplateId.Replace("_template", string.Empty), mergeable: true),

                // use the global instance
                Mode.MergeAll => CreateRef.FromAny(_umaMaterial),
                _ => default
            };

            if (umaMaterialRef.IsAlive)
            {
                buildData.Refs.Add(umaMaterialRef);
            }

            // build all splits in parallel
            UniTask ClosureBuildSplitAsync(Split split) => BuildSplitAsync(split, umaMaterialRef.Item, buildData);
            await UniTask.WhenAll(wearable.Splits.Select(ClosureBuildSplitAsync));

            // build the UMA wardrobe recipe (it will own the template ref now)
            Ref<UMAWardrobeRecipe> recipeRef = BuildUmaWardrobeRecipe(templateRef, metadata.Species, buildData.Slots);
            recipeRef.Item.name = $"{wearable.TemplateId}({wearableId})";
            buildData.Refs.Add(recipeRef);

            // create and return the outfit item asset with the build data as the dependencies
            var asset = new OutfitAsset(
                GenieTypeName.NonUma,
                lod,
                metadata,
                recipeRef.Item,
                buildData.SlotAssets.ToArray(),
                buildData.OverlayAssets.ToArray(),
                buildData.ComponentCreators.ToArray(),
                buildData
            );

            return asset;
        }

        private async UniTask BuildSplitAsync(Split split, UMAMaterial umaMaterial, BuildData buildData)
        {
            // try to load the asset for the split element
            Ref<UgcElementAsset> assetRef = await _elementLoader.LoadAsync(split.ElementId);
            if (!assetRef.IsAlive)
            {
                Debug.LogError($"[{nameof(LodUgcOutfitAssetBuilder)}] couldn't load split element asset with ID: {split.ElementId}");
                return;
            }

            buildData.Refs.Add(assetRef);

            // add the component creators from the element asset
            buildData.ComponentCreators.AddRange(assetRef.Item.ComponentCreators);

            // when we are not provided with a uma material it means that we have to create our own
            if (!umaMaterial)
            {
                Ref<UMAMaterial> umaMaterialRef = BuildUmaMaterial(split.ElementId, mergeable: false);
                umaMaterial = umaMaterialRef.Item;
                buildData.Refs.Add(umaMaterialRef);
            }

            // create the overlay data asset and overlay data (we only need one for the entire split)
            var overlayDataAsset = CreateInstance<OverlayDataAsset>();
            overlayDataAsset.overlayName = $"{split.ElementId}_overlay";
            overlayDataAsset.material = umaMaterial;
            var overlayData = new OverlayData(overlayDataAsset);

            // build slot data assets and calculate the total split area relative to the texture area (if using surface pixel density)
            float splitSquareMetersPerSquareUVs = 0.0f;
            bool usesSurfacePixelDensity = UsesSurfacePixelDensity();

            foreach (SlotDataAsset slotAsset in assetRef.Item.SlotDataAssets)
            {
                await OperationQueue.EnqueueAsync(OperationCost.Low);

                if (usesSurfacePixelDensity)
                {
                    splitSquareMetersPerSquareUVs += slotAsset.GetSquareMetersPerSquareUVs();
                }

                BuildSlotDataAsset(slotAsset, overlayData, buildData);
            }

            // finish building the overlay (exporting the baked maps)
            Ref<OverlayDataAsset> overlayAssetRef = await BuildSplitOverlayAsync(split, overlayDataAsset, assetRef.New(), splitSquareMetersPerSquareUVs);
            buildData.OverlayAssets.Add(overlayAssetRef.Item);
            buildData.Refs.Add(overlayAssetRef);
        }

        private void BuildSlotDataAsset(SlotDataAsset slotAsset, OverlayData overlayData, BuildData buildData)
        {
            // create a copy of the slot data asset since the same UGC asset could be loaded in other avatar instances
            SlotDataAsset slotAssetCopy = Instantiate(slotAsset);
            slotAssetCopy.material = overlayData.asset.material;
            var slotData = new SlotData(slotAssetCopy);
            slotData.SetOverlay(0, overlayData);

            // populate build data
            buildData.SlotAssets.Add(slotAssetCopy);
            buildData.Slots.Add(slotData);
            buildData.Refs.Add(CreateRef.FromUnityObject(slotAssetCopy));
        }

        private async UniTask<Ref<OverlayDataAsset>> BuildSplitOverlayAsync(
            Split split,
            OverlayDataAsset overlay,
            Ref<UgcElementAsset> assetRef,
            float area)
        {
            await OperationQueue.EnqueueAsync(OperationCost.Low);

            // create the initial overlay ref that destroys the overlay once disposed
            Ref<OverlayDataAsset> overlayRef = CreateRef.FromUnityObject(overlay);

            // build the mega material from the split definition (the mega material owns the assetRef now)
            MegaMaterial megaMaterial = await _megaMaterialBuilder.BuildMegaMaterialAsync(split, assetRef);

            if (mode is Mode.DontBake)
            {
                // we won't perform baking so we don't dispose the mega material and use it directly on the UMA material
                overlay.material.material = megaMaterial.Material;
                megaMaterial.Material.name = split.ElementId;

                // make the overlay ref to own the mega material
                Ref<MegaMaterial> megaMaterialRef = CreateRef.FromDisposable(megaMaterial);
                overlayRef = CreateRef.FromDependentResource(overlayRef, megaMaterialRef);
            }
            else
            {
                // we have to bake the mega material
                Ref<Texture>[] textureRefs = await BakeSplitOverlayAsync(split, overlay, megaMaterial, area);

                // dispose the MegaMaterial since we no longer need it (this will dispose the assetRef too)
                megaMaterial.Dispose();

                // make the overlay ref to own the baked textures
                overlayRef = CreateRef.FromDependentResource(overlayRef, textureRefs);
            }

            return overlayRef;
        }

        // bakes the given mega material for the given split overlay. Returns the references to the generated baked maps
        private async UniTask<Ref<Texture>[]> BakeSplitOverlayAsync(Split split, OverlayDataAsset overlay, MegaMaterial megaMaterial, float area)
        {
            // prepare the material for exporting
            _properties.Set(mapExportingProperties);
            _properties.RemoveUnusedProperties(megaMaterial.Material);
            _properties.WriteValues(megaMaterial.Material);

            // export all maps
            var textureRefs = new Ref<Texture>[maps.Count];

            if (mode is not Mode.BakeSplits)
            {
                overlay.textureList = new Texture[maps.Count];
            }

            for (int i = 0; i < maps.Count; ++i)
            {
                // get the texture settings for the current map (map settings have higher precedence than general builder settings)
                UmaMaterialExportMap map = maps[i];
                SplitTextureSettings settings = map.SplitTextureSettings ? map.SplitTextureSettings : splitTextureSettings;
                TextureSettings textureSettings = settings.TextureSettings;
                Material ppMaterial = map.PostProcessingMaterial ? map.PostProcessingMaterial : postProcessingMaterial;

                if (!textureSettings)
                {
                    Debug.LogError($"[{nameof(LodUgcOutfitAssetBuilder)}] no texture settings defined for map {map.UmaChannel.materialPropertyName}");
                    continue;
                }

                // get a tmp texture settings so we don't modify the resolution for other parallel map bakes that may use a different one
                TextureSettings tmpTextureSettings = GetTmpTextureSettings();
                tmpTextureSettings.CopyFrom(textureSettings);

                // if settings are set for dynamic resolution, then calculate it and update the texture settings
                if (settings.UseSurfacePixelDensity)
                {
                    tmpTextureSettings.width = tmpTextureSettings.height =
                        settings.SurfacePixelDensity.CalculateTextureSize(area);
                }

                // export the map and restore texture settings to previous resolution
                Texture texture = await map.MapExporter.ExportAsync(megaMaterial.Material, tmpTextureSettings, ppMaterial);
                texture.name = $"{split.ElementId}--{map.UmaChannel.materialPropertyName}";

                ReleaseTmpTextureSettings(tmpTextureSettings);

                // create and register a ref for the texture
                textureRefs[i] = CreateRef.FromUnityObject(texture);

                // based on the current mode either set the texture on the overlay or on the material directly
                if (mode is Mode.BakeSplits)
                {
                    overlay.material.material.SetTexture(map.UmaChannel.materialPropertyName, texture);
                }
                else
                {
                    overlay.textureList[i] = texture;
                }
            }

            return textureRefs;
        }

        private static Ref<UMAWardrobeRecipe> BuildUmaWardrobeRecipe(Ref<UgcTemplateAsset> templateRef, string species, List<SlotData> slots)
        {
            if (!templateRef.IsAlive)
            {
                return default;
            }

            var recipe = CreateInstance<UMAWardrobeRecipe>();
            UgcTemplateAsset template = templateRef.Item;
            recipe.wardrobeSlot = template.Data.Slot;

            // get the UMA recipe and populate the slot data list
            UMAData.UMARecipe umaRecipe = recipe.GetUMARecipe();
            umaRecipe.slotDataList = slots.ToArray();
            UMAPackedRecipeBase.UMAPackRecipe packRecipe = UMAPackedRecipeBase.PackRecipeV3(umaRecipe);
            recipe.PackedSave(packRecipe, UMAContextBase.FindInstance());

            // set the mesh hide assets and suppress slots
            recipe.MeshHideAssets.AddRange(template.MeshHideAssets);
            OutfitSlotsData slotsData = GenieSpecies.GetOutfitSlotsData(species);
            if (slotsData != null && slotsData.TryGetSlot(template.Data.Slot, out OutfitSlotsData.Slot slotData))
            {
                recipe.suppressWardrobeSlots.AddRange(slotData.SuppressedSlots);
            }

            // create the recipe ref that will own the template ref
            Ref<UMAWardrobeRecipe> recipeRef = CreateRef.FromUnityObject(recipe);
            recipeRef = CreateRef.FromDependentResource(recipeRef, templateRef);

            return recipeRef;
        }

        private Ref<UMAMaterial> BuildUmaMaterial(string name, bool mergeable)
        {
            // if not merging all UGC assets then create a unique instance for this wearable
            UMAMaterial umaMaterial = Instantiate(_umaMaterial);
            umaMaterial.name = name;

            Ref<UMAMaterial> umaMaterialRef = CreateRef.FromUnityObject(umaMaterial);

            // if not baking we will set the material later
            if (mode is not Mode.DontBake)
            {
                // it is not really necessary to create this copy since UMA will copy it anyways but this way we can see the wearable name on the renderer
                umaMaterial.material = new Material(material);
                umaMaterial.material.name = name;

                // make sure the new material instance is destroyed with the UMA material
                Ref<Material> materialRef = CreateRef.FromUnityObject(umaMaterial.material);
                umaMaterialRef = CreateRef.FromDependentResource(umaMaterialRef, materialRef);
            }

            if (mergeable)
            {
                umaMaterial.materialType = UMAMaterial.MaterialType.Atlas;
            }
            else
            {
                umaMaterial.materialType = UMAMaterial.MaterialType.UseExistingTexture;
                umaMaterial.channels = Array.Empty<UMAMaterial.MaterialChannel>();
            }

            return umaMaterialRef;
        }

        // whether or not any map will use the surface pixel density
        private bool UsesSurfacePixelDensity()
        {
            foreach (UmaMaterialExportMap map in maps)
            {
                if (map.SplitTextureSettings && map.SplitTextureSettings.UseSurfacePixelDensity
                    || splitTextureSettings && splitTextureSettings.UseSurfacePixelDensity)
                {
                    return true;
                }
            }

            return false;
        }

        private TextureSettings GetTmpTextureSettings()
        {
            return _mapTextureSettingsPool.Count > 0 ? _mapTextureSettingsPool.Pop() : CreateInstance<TextureSettings>();
        }

        private void ReleaseTmpTextureSettings(TextureSettings textureSettings)
        {
            _mapTextureSettingsPool.Push(textureSettings);
        }

        private void OnDestroy()
        {
            foreach (TextureSettings textureSettings in _mapTextureSettingsPool)
            {
                Destroy(textureSettings);
            }

            _mapTextureSettingsPool.Clear();
        }

        private sealed class BuildData : IDisposable
        {
            public readonly List<SlotDataAsset> SlotAssets = new();
            public readonly List<OverlayDataAsset> OverlayAssets = new();
            public readonly List<SlotData> Slots = new();
            public readonly List<IGenieComponentCreator> ComponentCreators = new();
            public readonly List<Ref> Refs = new();

            public void Dispose()
            {
                foreach (Ref reference in Refs)
                {
                    reference.Dispose();
                }

                Refs.Clear();
            }
        }
    }
}
