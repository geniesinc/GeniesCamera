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
    /// <summary>
    /// <see cref="IUgcOutfitAssetBuilder"/> implementation without baking support (uses the MegaShader). It also contains some utility methods
    /// for lower level manipulation like building elements separately or creating UMA recipes from custom built elements. Those utility methods
    /// are currently used for UGC editing preview.
    /// <br/><br/>
    /// This <see cref="IUgcOutfitAssetBuilder"/> implementation does not support LODs.
    /// </summary>
    public sealed class NonBakedUgcOutfitAssetBuilder : IUgcOutfitAssetBuilder
    {
        // dependencies
        private readonly IAssetLoader<UgcTemplateAsset> _templateLoader;
        private readonly IAssetLoader<UgcElementAsset> _elementLoader;
        private readonly IMegaMaterialBuilder _megaMaterialBuilder;

        public NonBakedUgcOutfitAssetBuilder(IAssetLoader<UgcTemplateAsset> templateLoader, IAssetLoader<UgcElementAsset> elementLoader, IMegaMaterialBuilder megaMaterialBuilder)
        {
            _templateLoader = templateLoader;
            _elementLoader = elementLoader;
            _megaMaterialBuilder = megaMaterialBuilder;
        }

        public async UniTask<OutfitAsset> BuildOutfitAssetAsync(string wearableId, Wearable wearable, OutfitAssetMetadata metadata, string lod = AssetLod.Default)
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

            var slots = new List<SlotDataAsset>();
            var overlays = new List<OverlayDataAsset>();
            var elements = new List<OutfitAssetElement>(wearable.Splits.Count);
            var componentCreators = new List<IGenieComponentCreator>();
            var references = new List<Ref>();

            // add the template's component creators
            componentCreators.AddRange(templateRef.Item.ComponentCreators);

            // process each split
            foreach (Split split in wearable.Splits)
            {
                // build the element for this split and apply the split to the megamaterial
                OutfitAssetElement element = await BuildElementAsync(split.ElementId, split.MaterialVersion);
                await element.MegaMaterial.ApplySplitAsync(split);

                // add the element's slots and overlays
                slots.AddRange(element.Slots);
                overlays.AddRange(element.Overlays);

                // add the element's component creators
                componentCreators.AddRange(element.ComponentCreators);

                // create a reference to the element and store it to the references list
                Ref<OutfitAssetElement> elementRef = CreateRef.FromDisposable(element);
                references.Add(elementRef);

                elements.Add(element);
            }

            // create a UMA wardrobe recipe
            Ref<UMAWardrobeRecipe> recipeRef = BuildUmaWardrobeRecipe(templateRef, metadata.Species, elements);
            recipeRef.Item.name = $"uh_{wearable.TemplateId}_{wearableId}_recipe";
            references.Add(recipeRef);

            // create and return the outfit item asset with the dependencies
            var dependencies = new Dependencies(references);
            var asset = new OutfitAsset(
                GenieTypeName.NonUma,
                AssetLod.Default,
                metadata,
                recipeRef.Item,
                slots.ToArray(),
                overlays.ToArray(),
                componentCreators.ToArray(),
                dependencies
            );

            return asset;
        }

        public async UniTask<OutfitAssetElement> BuildElementAsync(string elementId, string materialVersion = null)
        {
            Ref<UgcElementAsset> assetRef = await _elementLoader.LoadAsync(elementId);
            return BuildElement(elementId, assetRef, materialVersion);
        }

        public OutfitAssetElement BuildElement(string elementId, Ref<UgcElementAsset> assetRef, string materialVersion = null)
        {
            if (!assetRef.IsAlive)
            {
                return null;
            }

            // instantiate a mega material which is disposable and owns the asset reference now
            MegaMaterial megaMaterial = _megaMaterialBuilder.BuildMegaMaterial(assetRef, materialVersion);
            megaMaterial.Material.name = elementId;
            // create the uma material from the mega material, the returned ref owns the mega material
            Ref<UMAMaterial> umaMaterialRef = CreateUmaMaterial(elementId, megaMaterial);
            // create the overlay data asset from the uma material, the returned ref owns the uma material ref
            Ref<OverlayDataAsset> overlayDataAssetRef = CreateOverlayDataAsset(umaMaterialRef, elementId);

            // process all slot data assets
            SlotDataAsset[] slotDataAssets = assetRef.Item.SlotDataAssets;
            var slotsArray = new SlotDataAsset[slotDataAssets.Length];
            var overlaysArray = new OverlayDataAsset[slotDataAssets.Length];
            for (int i = 0; i < slotDataAssets.Length; ++i)
            {
                SlotDataAsset slotDataAsset = slotDataAssets[i];

                slotDataAsset.material = umaMaterialRef.Item;
                slotsArray[i] = slotDataAsset; // this slotDataAsset comes with the element asset, meaning that it will disposed when the asset is
                overlaysArray[i] = overlayDataAssetRef.Item;
            }

            // create the slot data instances
            var slotDataArray = new SlotData[slotsArray.Length];
            for (int i = 0; i < slotsArray.Length; i++)
            {
                var slotData = new SlotData(slotsArray[i]);
                var overlayData = new OverlayData(overlaysArray[i]);
                slotData.SetOverlay(0, overlayData);

                slotDataArray[i] = slotData;
            }

            // create the element and use only the overlayDataAssetRef as its dependencies since that ref is owning the rest
            IGenieComponentCreator[] componentCreators = assetRef.Item.ComponentCreators;
            GetElementAssetExtraData(assetRef.Item, out Bounds bounds, out List<Vector3> vertices);

            var element = new OutfitAssetElement(elementId, slotsArray, overlaysArray, slotDataArray,
                componentCreators, megaMaterial, elementId, assetRef.Item.Data.Regions, bounds,
                vertices, overlayDataAssetRef);

            return element;
        }

        public async UniTask<(Ref<UMAWardrobeRecipe>, UgcTemplateAsset)> BuildUmaWardrobeRecipeAsync(string templateId, string species, IEnumerable<OutfitAssetElement> elements)
        {
            Ref<UgcTemplateAsset> templateRef = await _templateLoader.LoadAsync(templateId);
            if (!templateRef.IsAlive)
            {
                return default;
            }

            return (BuildUmaWardrobeRecipe(templateRef, species, elements), templateRef.Item);
        }

        public Ref<UMAWardrobeRecipe> BuildUmaWardrobeRecipe(Ref<UgcTemplateAsset> templateRef, string species, IEnumerable<OutfitAssetElement> elements)
        {
            if (!templateRef.IsAlive)
            {
                return default;
            }

            UgcTemplateAsset template = templateRef.Item;
            var recipe = ScriptableObject.CreateInstance<UMAWardrobeRecipe>();
            recipe.wardrobeSlot = template.Data.Slot;

            SetUmaWardrobeRecipeElements(recipe, elements);

            // set the mesh hide assets and suppress slots
            recipe.MeshHideAssets.Clear();
            recipe.MeshHideAssets.AddRange(template.MeshHideAssets);
            SetSuppressWardrobeSlots(recipe.suppressWardrobeSlots, template.Data.Slot, species);

            // create the recipe ref that will own the template ref
            Ref<UMAWardrobeRecipe> recipeRef = CreateRef.FromUnityObject(recipe);
            Ref<UMAWardrobeRecipe> finalRef = CreateRef.FromDependentResource(recipeRef, templateRef);
            return finalRef;
        }

        public void SetUmaWardrobeRecipeElements(UMAWardrobeRecipe recipe, IEnumerable<OutfitAssetElement> elements)
        {
            if (!recipe)
            {
                return;
            }

            // get all the slot data instances from all elements
            var slots = new List<SlotData>();
            if (elements != null)
            {
                foreach (OutfitAssetElement element in elements)
                {
                    slots.AddRange(element.SlotData);
                }
            }

            // get the UMA recipe and populate the slot data list
            UMAData.UMARecipe umaRecipe = recipe.GetUMARecipe();
            umaRecipe.slotDataList = slots.ToArray();
            UMAPackedRecipeBase.UMAPackRecipe packRecipe = UMAPackedRecipeBase.PackRecipeV3(umaRecipe);
            recipe.PackedSave(packRecipe, UMAContextBase.FindInstance());
        }

        private static Ref<UMAMaterial> CreateUmaMaterial(string elementId, MegaMaterial megaMaterial)
        {
            var umaMaterial = ScriptableObject.CreateInstance<UMAMaterial>();
            umaMaterial.name = elementId;
            umaMaterial.materialType = UMAMaterial.MaterialType.NoAtlas;
            umaMaterial.channels = Array.Empty<UMAMaterial.MaterialChannel>();
            umaMaterial.shaderParms = Array.Empty<UMAMaterial.ShaderParms>();
            umaMaterial.material = megaMaterial.Material;

            Ref<UMAMaterial> umaMaterialRef = CreateRef.FromUnityObject(umaMaterial);
            Ref<MegaMaterial> megaMaterialRef = CreateRef.FromDisposable(megaMaterial);
            Ref<Object> indexRef = GeniesAssetIndexer.Instance.Index(umaMaterial);
            return CreateRef.FromDependentResource(umaMaterialRef, megaMaterialRef, indexRef);
        }

        private static Ref<OverlayDataAsset> CreateOverlayDataAsset(Ref<UMAMaterial> umaMaterialRef, string elementId)
        {
            var overlayDataAsset = ScriptableObject.CreateInstance<OverlayDataAsset>();
            overlayDataAsset.overlayName = $"{elementId}_generated-overlay";
            overlayDataAsset.material = umaMaterialRef.Item;

            Ref<OverlayDataAsset> overlayDataAssetRef = CreateRef.FromUnityObject(overlayDataAsset);
            return CreateRef.FromDependentResource(overlayDataAssetRef, umaMaterialRef);
        }

        private static void SetSuppressWardrobeSlots(List<string> suppressWardrobeSlots, string slot, string species)
        {
            OutfitSlotsData slotsData = GenieSpecies.GetOutfitSlotsData(species);

            if (slotsData != null && slotsData.TryGetSlot(slot, out OutfitSlotsData.Slot slotData))
            {
                suppressWardrobeSlots.AddRange(slotData.SuppressedSlots);
            }
        }

        private static void GetElementAssetExtraData(UgcElementAsset asset, out Bounds bounds, out List<Vector3> vertices)
        {
            // build each slot
            vertices = new List<Vector3>();
            foreach (SlotDataAsset slotDataAsset in asset.SlotDataAssets)
            {
                if (slotDataAsset?.meshData?.vertices is null)
                {
                    continue;
                }

                vertices.AddRange(slotDataAsset.meshData.vertices);
            }

            bounds = GetBoundsFromVertices(vertices);
        }

        private static Bounds GetBoundsFromVertices(List<Vector3> vertices)
        {
            if (vertices is null || vertices.Count == 0)
            {
                return new Bounds();
            }

            var min = new Vector3(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity);
            var max = new Vector3(float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity);

            for (int i = 0; i < vertices.Count; ++i)
            {
                Vector3 vertex = vertices[i];

                if (vertex.x < min.x)
                {
                    min.x = vertex.x;
                }

                if (vertex.y < min.y)
                {
                    min.y = vertex.y;
                }

                if (vertex.z < min.z)
                {
                    min.z = vertex.z;
                }

                if (vertex.x > max.x)
                {
                    max.x = vertex.x;
                }

                if (vertex.y > max.y)
                {
                    max.y = vertex.y;
                }

                if (vertex.z > max.z)
                {
                    max.z = vertex.z;
                }
            }

            var bounds = new Bounds();
            bounds.SetMinMax(min, max);
            return bounds;
        }

        private class Dependencies : IDisposable
        {
            private readonly List<Ref> _references;

            public Dependencies(List<Ref> references)
            {
                _references = references;
            }

            public void Dispose()
            {
                foreach (Ref reference in _references)
                {
                    reference.Dispose();
                }

                _references.Clear();
            }
        }
    }
}
