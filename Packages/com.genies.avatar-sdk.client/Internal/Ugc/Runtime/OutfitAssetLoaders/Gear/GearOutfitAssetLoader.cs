using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Genies.Assets.Services;
using Genies.Avatars;
using Genies.Refs;
using UMA;
using UMA.CharacterSystem;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Genies.Ugc
{
    public sealed class GearOutfitAssetLoader : OutfitAssetLoaderBase
    {
        private static readonly IReadOnlyList<string> _supportedTypes
            = new List<string> { UgcOutfitAssetType.Gear }.AsReadOnly();

        public override IReadOnlyList<string> SupportedTypes => _supportedTypes;

        // dependencies
        private readonly IAssetsService _assetsService;
        private readonly IAssetLoader<GearAsset> _assetLoader;
        private readonly IAssetLoader<GearElementAsset> _elementLoader;

        public GearOutfitAssetLoader(IAssetLoader<GearAsset> assetLoader, IAssetLoader<GearElementAsset> elementLoader)
        {
            _assetLoader = assetLoader;
            _elementLoader = elementLoader;
        }

        public async UniTask<OutfitAsset> BuildOutfitAssetAsync(GearAssetConfig config, string species, string lod = AssetLod.Default)
        {
            Ref<GearAsset> assetRef = await _assetLoader.LoadAsync(config.AssetAddress, lod);
            if (!assetRef.IsAlive)
            {
                return null;
            }

            GearAsset asset = assetRef.Item;
            var metadata = new OutfitAssetMetadata
            {
                Id = config.AssetAddress,
                Slot = asset.AssetData.Slot,
                Subcategory = asset.AssetData.Subcategory,
                Species = species,
                Type = UgcOutfitAssetType.Gear,
                CollisionData = asset.AssetData.CollisionData,
            };

            OutfitAsset outfitAsset = await BuildOutfitAssetAsync(metadata, config, lod);
            return outfitAsset;
        }

        public override bool IsOutfitAssetTypeSupported(string type)
        {
            return type == UgcOutfitAssetType.Gear;
        }

        protected override UniTask<OutfitAsset> LoadOutfitAssetAsync(OutfitAssetMetadata metadata, string genieType = GenieTypeName.NonUma, string lod = AssetLod.Default)
        {
            // for now we only have non-generative assets with a single element, and the id for the asset and element are the same
            var config = new GearAssetConfig
            {
                AssetAddress = metadata.Id,
                ElementAddresses = new List<string> { metadata.Id },
            };

            return BuildOutfitAssetAsync(metadata, config, lod);
        }

        private async UniTask<OutfitAsset> BuildOutfitAssetAsync(OutfitAssetMetadata metadata, GearAssetConfig config, string lod, bool isNonUma = true)
        {
            // try to load the gear asset
            Ref<GearAsset> assetRef = await _assetLoader.LoadAsync(config.AssetAddress, lod);
            if (!assetRef.IsAlive)
            {
                return null;
            }

            GearAsset asset = assetRef.Item;

            // build each element
            //bool isNonUma = lod == NonUmaGenie.NonUmaLod; // if LOD is non-uma avoid all UMA specific asset creation, this greatly optimizes the outfit asset build process
            var buildData = new BuildData(isNonUma);

            /**
             * For now, we will ignore the element addresses coming from the config since we don't support generative.
             * We will just build all elements coming in the GearAsset instead of separately loading the ones specified
             * in the config.
             */
            // await UniTask.WhenAll(config.elementAddresses.Select(address => BuildElementAsync(address, lod, buildData)));
            foreach (GearElementAsset elementAsset in asset.Elements)
            {
                BuildElement(elementAsset, buildData);
            }

            // build the UMA wardrobe recipe
            UMAWardrobeRecipe recipe = null;

            if (!isNonUma)
            {
                Ref<UMAWardrobeRecipe> recipeRef = BuildUmaWardrobeRecipe(asset, metadata.Species, buildData.Slots);
                buildData.Refs.Add(recipeRef);
                recipe = recipeRef.Item;
                recipe.name = $"{config.GetUniqueName()}_recipe";
            }

            // populate build data
            buildData.ComponentCreators.AddRange(asset.ComponentCreators);
            buildData.Refs.Add(assetRef);

            // create and return the outfit item asset
            var outfitAsset = new OutfitAsset(
                GenieTypeName.NonUma,
                lod,
                metadata,
                recipe,
                buildData.SlotAssets.ToArray(),
                buildData.OverlayAssets.ToArray(),
                buildData.ComponentCreators.ToArray(),
                buildData,
                isNonUma ? buildData.MeshAssets.ToArray() : null,
                isNonUma ? MeshAssetUtility.CreateTriangleFlagsFrom(asset.MeshHideAssets).ToArray() : null
            );

            return outfitAsset;
        }

        private async UniTask BuildElementAsync(string elementAddress, string lod, BuildData buildData)
        {
            Ref<GearElementAsset> elementAssetRef = await _elementLoader.LoadAsync(elementAddress, lod);
            if (!elementAssetRef.IsAlive)
            {
                return;
            }

            BuildElement(elementAssetRef.Item, buildData);
            buildData.Refs.Add(elementAssetRef);
        }

        private void BuildElement(GearElementAsset elementAsset, BuildData buildData)
        {
            foreach (GearSubElement subElement in elementAsset.SubElements)
            {
                BuildSubElement(subElement, buildData);
            }

            // populate build data
            buildData.ComponentCreators.AddRange(elementAsset.ComponentCreators);
        }

        private readonly int MainTexPropertyId = Shader.PropertyToID("_MainTex");

        private void BuildSubElement(GearSubElement subElement, BuildData buildData)
        {
            // TODO there is a non-sense 1K white texture coming with all our gear asset materials that causes an extra render texture for that atlas. We should fix this from the content build, not here
            Material materialToFix = subElement.Material.material;
            if (materialToFix.HasProperty(MainTexPropertyId) && materialToFix.GetTexture(MainTexPropertyId)?.name == "temp_white_2")
            {
                materialToFix.SetTexture(MainTexPropertyId, null);
            }

            // if LOD is non-uma avoid all UMA specific asset creation, this greatly optimizes the outfit asset build process
            if (buildData.IsNonUma)
            {
                Material material = MeshAssetUtility.CreateMaterialFrom(subElement.Material, subElement.Overlay.textureList, out bool noTextureCombine);
                MeshAsset meshAsset = MeshAssetUtility.CreateMeshAssetFrom(subElement.Slot, material);
                meshAsset.NoTextureCombine = noTextureCombine;
                buildData.MeshAssets.Add(meshAsset);
                return;
            }

            SlotDataAsset slotCopy = Object.Instantiate(subElement.Slot);
            OverlayDataAsset overlayCopy = Object.Instantiate(subElement.Overlay);
            slotCopy.material = subElement.Material;
            overlayCopy.material = subElement.Material;

            // create the slot and overlay data
            var overlayData = new OverlayData(overlayCopy);
            var slotData = new SlotData(slotCopy);
            slotData.SetOverlay(0, overlayData);

            // populate build data
            buildData.SlotAssets.Add(slotCopy);
            buildData.OverlayAssets.Add(overlayCopy);
            buildData.Slots.Add(slotData);
            buildData.Refs.Add(CreateRef.FromUnityObject(slotCopy));
            buildData.Refs.Add(CreateRef.FromUnityObject(overlayCopy));
        }

        private static Ref<UMAWardrobeRecipe> BuildUmaWardrobeRecipe(GearAsset asset, string species, List<SlotData> slots)
        {
            var recipe = ScriptableObject.CreateInstance<UMAWardrobeRecipe>();
            recipe.wardrobeSlot = asset.AssetData.Slot;

            // get the UMA recipe and populate the slot data list
            UMAData.UMARecipe umaRecipe = recipe.GetUMARecipe();
            umaRecipe.slotDataList = slots.ToArray();
            UMAPackedRecipeBase.UMAPackRecipe packRecipe = UMAPackedRecipeBase.PackRecipeV3(umaRecipe);
            recipe.PackedSave(packRecipe, UMAContextBase.FindInstance());

            // set the mesh hide assets and suppress slots
            recipe.MeshHideAssets.AddRange(asset.MeshHideAssets);
            OutfitSlotsData slotsData = GenieSpecies.GetOutfitSlotsData(species);
            if (slotsData != null && slotsData.TryGetSlot(asset.AssetData.Slot, out OutfitSlotsData.Slot slotData))
            {
                recipe.suppressWardrobeSlots.AddRange(slotData.SuppressedSlots);
            }

            return CreateRef.FromUnityObject(recipe);
        }

        private sealed class BuildData : IDisposable
        {
            public readonly bool IsNonUma;

            public List<SlotDataAsset>          SlotAssets        = new();
            public readonly List<OverlayDataAsset>       OverlayAssets     = new();
            public readonly List<SlotData>               Slots             = new();
            public readonly List<IGenieComponentCreator> ComponentCreators = new();
            public readonly List<Ref>                    Refs              = new();
            public List<MeshAsset>              MeshAssets        = new();

            public BuildData(bool isNonUma)
            {
                IsNonUma = isNonUma;
            }

            public void Dispose()
            {
                foreach (Ref reference in Refs)
                {
                    reference.Dispose();
                }

                Refs.Clear();

                MeshAssets.Clear();
                MeshAssets = null;
            }
        }
    }
}
