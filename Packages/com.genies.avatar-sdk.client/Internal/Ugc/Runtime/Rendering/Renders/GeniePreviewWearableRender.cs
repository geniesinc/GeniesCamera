using System;
using System.Collections.Generic;
using System.Threading;
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
    /// <see cref="IWearableRender"/> implementation that allows you to apply <see cref="Wearable"/> definitions to a wearable
    /// directly equipped to an avatar.
    /// </summary>
    public sealed class GeniePreviewWearableRender : IWearableRender
    {
        public bool IsAlive { get; private set; }
        public GameObject Root => null;
        public Bounds Bounds { get { RecalculateBounds(); return _bounds; } }
        public bool RegionDebugging
        {
            get => throw new NotImplementedException();
            set => throw new NotImplementedException();
        }

        private readonly UnifiedGenieController _genieController;
        private readonly IAssetsController<OutfitAsset> _outfitController;
        private readonly NonBakedUgcOutfitAssetBuilder _ugcAssetBuilder;
        private readonly IUgcTemplateDataService _templateDataService;
        private readonly Renderer _renderer;

        private readonly Dictionary<string, Element> _elements;
        private readonly HashSet<string> _previousRenderedElementIds;
        private readonly HashSet<string> _renderedElementIds;
        private readonly HashSet<string> _boundedElementIds;
        private Bounds _bounds;
        private CancellationTokenSource _renderCancellationSource = new CancellationTokenSource();
        private UniTaskCompletionSource _renderCompletionSource;
        private bool _regionDebugging;

        private OutfitAsset _currentOutfitAsset;
        private Ref<UMAWardrobeRecipe> _recipeRef;
        private UgcTemplateAsset _currentTemplateAsset;
        private OutfitAssetMetadata _outfitAssetMetadata;

        public GeniePreviewWearableRender(ISpeciesGenieController genieController,
            NonBakedUgcOutfitAssetBuilder ugcAssetBuilder,
            IUgcTemplateDataService templateDataService)
        {
            if (genieController is not UnifiedGenieController controller)
            {
                throw new ArgumentException("Genie controller must be a UnifiedGenieController. Other controllers are not compatible with this class", nameof(genieController));
            }

            _genieController = controller;
            _outfitController = controller.Outfit;
            _ugcAssetBuilder = ugcAssetBuilder;
            _templateDataService = templateDataService;
            _elements = new Dictionary<string, Element>();
            _previousRenderedElementIds = new HashSet<string>();
            _renderedElementIds = new HashSet<string>();
            _boundedElementIds = new HashSet<string>();

            _renderer = controller.Genie.Root.GetComponentInChildren<Renderer>();

            IsAlive = true;
        }

        public async UniTask ApplyWearableAsync(Wearable wearable)
        {
            if (!IsAlive || wearable is null)
            {
                return;
            }

            StartRender();
            ClearRender();

            // render all the wearable splits (will try to reuse cached element renders)
            if (!(wearable.Splits is null))
            {
                await UniTask.WhenAll(wearable.Splits.Select(RenderSplit));
            }

            string templateId = wearable.TemplateId;
            if (!templateId.EndsWith("_template"))
            {
                templateId += "_template";
            }

            await UpdateOutfitAssetAsync(templateId);

            FinishRender();
        }

        public Bounds GetAlignedBounds(Quaternion rotation)
        {
            // encapsulate the aligned bounds of all rendered elements
            var bounds = new Bounds(Vector3.zero, Vector3.negativeInfinity);
            bool neverEncapsulated = true;

            foreach (IElementRender elementRender in GetAllDisplayedRenders())
            {
                bounds.Encapsulate(elementRender.GetAlignedBounds(rotation));
                neverEncapsulated = false;
            }

            return neverEncapsulated ? new Bounds() : bounds;
        }

        public void SetElementIdSoloRendered(string elementId, bool soloRendered)
        {
            throw new NotImplementedException();
        }

        public void SetElementIdsSoloRendered(IEnumerable<string> elementIds, bool soloRendered)
        {
            throw new NotImplementedException();
        }

        public void ClearAllSoloRenders()
        {
            throw new NotImplementedException();
        }

        public void PlayAnimation(string elementId, ValueAnimation animation)
        {
            if (_renderedElementIds.Contains(elementId) && _elements.TryGetValue(elementId, out Element element))
            {
                element.Render.PlayAnimation(animation);
            }
        }

        public void StopAnimation(string elementId)
        {
            if (_elements.TryGetValue(elementId, out Element element))
            {
                element.Render.StopAnimation();
            }
        }

        public void PlayRegionAnimation(string elementId, int regionIndex, ValueAnimation animation, bool playAlone = false)
        {
            if (_renderedElementIds.Contains(elementId) && _elements.TryGetValue(elementId, out Element element))
            {
                element.Render.PlayRegionAnimation(regionIndex, animation, playAlone);
            }
        }

        public void StopRegionAnimation(string elementId, int regionIndex)
        {
            if (_elements.TryGetValue(elementId, out Element element))
            {
                element.Render.StopRegionAnimation(regionIndex);
            }
        }

        public void StopAllAnimations()
        {
            foreach (IElementRender elementRender in GetAllDisplayedRenders())
            {
                elementRender.StopAnimation();
            }
        }

        public void Dispose()
        {
            DisposeAsync().Forget();
        }

        #region PRIVATE CHANGES
        private async UniTask DisposeAsync()
        {
            if (!IsAlive)
            {
                return;
            }

            foreach (Element element in _elements.Values)
            {
                element.Dispose();
            }

            if (_currentOutfitAsset != null)
            {
                if (!_genieController.Genie.IsDisposed)
                {
                    await _outfitController.UnequipAssetAsync(_currentOutfitAsset.Id);
                    await _genieController.RebuildGenieAsync();
                }

                _currentOutfitAsset.Dispose();
            }

            _recipeRef.Dispose();
            _elements.Clear();
            _renderedElementIds.Clear();
            _boundedElementIds.Clear();
            IsAlive = false;
        }

        private async UniTask RenderSplit(Split split)
        {
            if (split?.ElementId is null)
            {
                return;
            }

            CancellationToken cancellationToken = _renderCancellationSource.Token;

            // if we have not rendered this element before, create a new render
            if (!_elements.TryGetValue(split.ElementId, out Element element))
            {
                OutfitAssetElement elementAsset = await _ugcAssetBuilder.BuildElementAsync(split.ElementId, split.MaterialVersion);
                if (elementAsset is null)
                {
                    return;
                }

                // it could happen that the same element was rendered by the next apply call
                if (cancellationToken.IsCancellationRequested && _elements.TryGetValue(split.ElementId, out _))
                {
                    elementAsset.Dispose();
                    return;
                }

                IElementRender elementRender = new ElementRender(elementAsset.RegionCount, default,
                    elementAsset.MegaMaterial, elementAsset.Bounds, elementAsset.Vertices);

                elementRender.RegionDebugging = _regionDebugging;

                element = new Element(elementRender, elementAsset);
                _elements[split.ElementId] = element;
            }

            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }

            // enable the render and apply the split
            _renderedElementIds.Add(split.ElementId);
            await element.Render.ApplySplitAsync(split);
        }

        private async UniTask UpdateOutfitAssetAsync(string templateId)
        {
            // if all elements are the same as before, do nothing
            if (_previousRenderedElementIds.SetEquals(_renderedElementIds))
            {
                return;
            }

            // update UMA recipe
            await UpdateRecipeAsync(templateId);

            // unequip current outfit asset if any
            bool unequipped = false;
            if (_currentOutfitAsset != null)
            {
                await _outfitController.UnequipAssetAsync(_currentOutfitAsset.Id);
                _currentOutfitAsset.Dispose();
                _currentOutfitAsset = null;
                unequipped = true;
            }

            // check if the recipe update was successful
            if (!_recipeRef.IsAlive)
            {
                // make sure the unequipping takes effect
                if (unequipped)
                {
                    await _genieController.RebuildGenieAsync();
                }

                Debug.LogError($"[{nameof(GeniePreviewWearableRender)}] UMA recipe failed to update");
                return;
            }

            // get all the assets required to build a new OutfitAsset from the element assets
            var elementAssets = new List<OutfitAssetElement>(_renderedElementIds.Count);
            var slots = new List<SlotDataAsset>();
            var overlays = new List<OverlayDataAsset>();
            var componentCreators = new List<IGenieComponentCreator>();

            // add component creators from the template asset
            componentCreators.AddRange(_currentTemplateAsset.ComponentCreators);

            // populate the assets with the rendered elements
            foreach (string elementId in _renderedElementIds)
            {
                if (!_elements.TryGetValue(elementId, out Element element))
                {
                    Debug.LogError($"[{nameof(GeniePreviewWearableRender)}] couldn't render element: {elementId}");
                    continue;
                }

                elementAssets.Add(element.Asset);
                slots.AddRange(element.Asset.Slots);
                overlays.AddRange(element.Asset.Overlays);
                componentCreators.AddRange(element.Asset.ComponentCreators);
            }

            // update uma recipe with the new elements and build the new outfit asset
            _ugcAssetBuilder.SetUmaWardrobeRecipeElements(_recipeRef.Item, elementAssets);
            _currentOutfitAsset = new OutfitAsset(
                GenieTypeName.NonUma,
                AssetLod.Default,
                _outfitAssetMetadata,
                _recipeRef.Item,
                slots.ToArray(),
                overlays.ToArray(),
                componentCreators.ToArray(),
                dependencies: null // we handle disposal of the resources manually on this class
            );

            // equip the new outfit asset
            Ref<OutfitAsset> dummyRef = CreateRef.FromAny(_currentOutfitAsset);
            await _outfitController.EquipAssetAsync(dummyRef);

            CopyMaterials();
            await _genieController.RebuildGenieAsync();
            UpdateMaterials();
        }

        private async UniTask UpdateRecipeAsync(string templateId)
        {
            if (_recipeRef.IsAlive && templateId == _currentTemplateAsset?.Id)
            {
                return;
            }

            // dispose previous recipe
            _recipeRef.Dispose();

            // try to get the template data
            UgcTemplateData templateData = await _templateDataService.FetchTemplateDataAsync(templateId);
            if (templateData is null)
            {
                return;
            }

            // rebuild recipe for the new template id
            UgcTemplateAsset templateAsset;
            (_recipeRef, templateAsset) = await _ugcAssetBuilder.BuildUmaWardrobeRecipeAsync(templateId, _genieController.Genie.Species, null);
            if (!_recipeRef.IsAlive)
            {
                return;
            }

            _recipeRef.Item.name = $"uh_{templateId}_previewRecipe";

            // build the outfit asset metadata for this template so we can build the outfit asset later
            _outfitAssetMetadata = new OutfitAssetMetadata(null)
            {
                Id = templateId,
                Slot = templateData.Slot,
                Subcategory = templateData.Subcategory,
                Type = UgcOutfitAssetType.Ugc,
                CollisionData = templateData.CollisionData,
            };

            _currentTemplateAsset = templateAsset;
        }

        /// <summary>
        /// Goes over the materials on the renderer and for all of them that match a mega material instance we replace it with
        /// a tmp copy so UMA doesn't destroy our mega material instance when rebuilding.
        /// </summary>
        private void CopyMaterials()
        {
            Material[] materials = _renderer.sharedMaterials;

            foreach (Element element in _elements.Values)
            {
                Material megaMaterial = element.Asset.MegaMaterial.Material;
                for (int i = 0; i < materials.Length; ++i)
                {
                    if (materials[i] == megaMaterial)
                    {
                        materials[i] = new Material(megaMaterial);
                    }
                }
            }

            _renderer.sharedMaterials = materials;
        }

        /// <summary>
        /// Subtitutes the renderer materials built by UMA with the megamaterial instances from our element assets. This is so
        /// any material updates are reflected in real time on the avatar.
        /// </summary>
        private void UpdateMaterials()
        {
            Material[] materials = _renderer.sharedMaterials;

            foreach (string elementId in _renderedElementIds)
            {
                if (!_elements.TryGetValue(elementId, out Element element))
                {
                    continue;
                }

                Material megaMaterial = element.Asset.MegaMaterial.Material;
                if (!TryGetMaterialIndex(materials, megaMaterial.name, out int index))
                {
                    continue;
                }

                Object.Destroy(materials[index]);
                materials[index] = megaMaterial;
            }

            _renderer.sharedMaterials = materials;
        }

        private bool TryGetMaterialIndex(Material[] materials, string name, out int index)
        {
            for (int i = 0; i < materials.Length; ++i)
            {
                if (materials[i]?.name != name)
                {
                    continue;
                }

                index = i;
                return true;
            }

            index = -1;
            return false;
        }

        private void ClearRender()
        {
            _previousRenderedElementIds.Clear();
            _previousRenderedElementIds.UnionWith(_renderedElementIds);
            _renderedElementIds.Clear();
        }

        private void RecalculateBounds()
        {
            if (_boundedElementIds.SetEquals(_renderedElementIds))
            {
                return;
            }

            _boundedElementIds.Clear();
            _bounds = new Bounds(Vector3.zero, Vector3.negativeInfinity);

            foreach (string elementId in _renderedElementIds)
            {
                if (!_elements.TryGetValue(elementId, out Element element))
                {
                    continue;
                }

                _bounds.Encapsulate(element.Render.Bounds);
                _boundedElementIds.Add(elementId);
            }

            if (_boundedElementIds.Count == 0)
            {
                _bounds = new Bounds();
            }
        }

        private void StartRender()
        {
            _renderCancellationSource.Cancel();
            _renderCancellationSource = new CancellationTokenSource();
            UniTaskCompletionSource oldCompletionSource = _renderCompletionSource;
            _renderCompletionSource = new UniTaskCompletionSource();
            oldCompletionSource?.TrySetResult();
        }

        private void FinishRender()
        {
            UniTaskCompletionSource oldCompletionSource = _renderCompletionSource;
            _renderCompletionSource = null;
            oldCompletionSource?.TrySetResult();
        }

        /// <summary>
        /// Returns a collection of all currently displayed renders (that is all the solo elements that are rendered or all the renders if there are no solo elements)
        /// </summary>
        private IEnumerable<IElementRender> GetAllDisplayedRenders()
        {
            foreach (string elementId in _renderedElementIds)
            {
                if (_elements.TryGetValue(elementId, out Element element))
                {
                    yield return element.Render;
                }
            }
        }

        private readonly struct Element : IDisposable
        {
            public readonly IElementRender Render;
            public readonly OutfitAssetElement Asset;

            public Element(IElementRender render, OutfitAssetElement asset)
            {
                Render = render;
                Asset = asset;
            }

            public void Dispose()
            {
                Render.Dispose();
                Asset.Dispose();
            }
        }

        #endregion
    }
}
