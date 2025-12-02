using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Unity.Collections;
using UnityEngine;

namespace Genies.Avatars
{
    /// <summary>
    /// Controls the body variation of a <see cref="IEditableGenie"/> instance and takes care of the outfit asset refitting.
    /// </summary>
    public sealed class BodyVariationController : IBodyVariationController, IOutfitAssetProcessor, IDisposable
    {
        public IReadOnlyList<string> Attributes => BodyController.Attributes;
        public string CurrentVariation { get; private set; }

        public event Action Updated = delegate { };

        public readonly BodyController BodyController;
        
        // dependencies
        private readonly IEditableGenie _genie;
        private readonly IRefittingService _refittingService;

        // state
        private readonly HashSet<string> _availableBodyVariations;

        public BodyVariationController(
            IEditableGenie genie,
            IRefittingService refittingService,
            IEnumerable<string> availableBodyVariations,
            BodyAttributesConfig bodyAttributesConfig = null
            )
        {
            //setup body variations
            _genie = genie;
            _refittingService = refittingService;

            _availableBodyVariations = new HashSet<string>(availableBodyVariations);

            _genie.AddOutfitAssetProcessor(this);

            BodyController = new BodyController(_genie, bodyAttributesConfig);
            BodyController.Updated += Updated;
        }

        public async UniTask InitializeAsync()
        {
            CurrentVariation = FindAppliedBodyVariationOnDna() ?? _availableBodyVariations.FirstOrDefault();
            await ApplyBodyVariationAsync(CurrentVariation);
        }

        public async UniTask SetBodyVariationAsync(string bodyVariation)
        {
            if (bodyVariation == CurrentVariation)
            {
                return;
            }

            await ApplyBodyVariationAsync(bodyVariation);

            if (CurrentVariation == bodyVariation)
            {
                Updated?.Invoke();
            }
        }
        
        public void AddBodyVariation(string bodyVariation, NativeArray<Vector3> deformPoints)
        {
            if (_availableBodyVariations.Contains(bodyVariation))
            {
                return;
            }

            // add to available body variations
            _availableBodyVariations.Add(bodyVariation);

            // add deform to refitting service
            ((RsRefittingService)_refittingService).AddDeformShape(bodyVariation, deformPoints);
        }
        
        public bool BodyVariationAvailable(string bodyVariation)
        {
            return _availableBodyVariations.Contains(bodyVariation);
        } 

        private UniTask ApplyBodyVariationAsync(string bodyVariation)
        {
            if (bodyVariation is null || !_availableBodyVariations.Contains(bodyVariation))
            {
                Debug.LogError($"[{nameof(BodyVariationController)}] unknown body variation: {bodyVariation}");
                return UniTask.CompletedTask;
            }

            // clear all body variations and then apply the new variation
            ClearAllBodyVariations();
            _genie.SetDna(bodyVariation, 1.0f);
            EnableBodyRefittingBlendShape(bodyVariation);
            CurrentVariation = bodyVariation;

            // add the body variation blend shape to all the outfit assets
            return UniTask.WhenAll(_genie.OutfitAssets.Select(ProcessAddedAssetAsync));
        }

        public UniTask ProcessAddedAssetAsync(OutfitAsset asset)
        {
            return _refittingService.AddBodyVariationBlendShapeAsync(asset, CurrentVariation);
        }

        public UniTask ProcessRemovedAssetAsync(OutfitAsset asset)
        {
            return UniTask.CompletedTask;
        }

        public void Dispose()
        {
            //chaos mode
            BodyController?.Dispose();

            //body variations
            Updated = null;
            ClearAllBodyVariations();
            _genie.RemoveOutfitAssetProcessor(this);
        }

        private void EnableBodyRefittingBlendShape(string bodyVariation)
        {
            string refittingBlendShapeName = _refittingService.GetBodyVariationBlendShapeName(bodyVariation);
            // bake the refitting blendshape into the mesh so we don't have the performance cost of realtime blendshapes
            _genie.SetBlendShape(refittingBlendShapeName, 1.0f, baked: true);
        }

        private void DisableBodyRefittingBlendShape(string bodyVariation)
        {
            string refittingBlendShapeName = _refittingService.GetBodyVariationBlendShapeName(bodyVariation);

            /**
             * We were previously removing the blendshape but for some reason there are really hard to reproduce edge cases
             * were UMA keeps the blendshape even if we removed it, so just setting the value to 0 is much safer.
             */
            _genie.SetBlendShape(refittingBlendShapeName, 0.0f, baked: true);
        }

        private void ClearAllBodyVariations()
        {
            foreach (string availableBodyVariation in _availableBodyVariations)
            {
                _genie.SetDna(availableBodyVariation, 0.5f);
                DisableBodyRefittingBlendShape(availableBodyVariation);
            }
        }

        // tries to find the current applied body variation from the genie dna
        private string FindAppliedBodyVariationOnDna()
        {
            foreach (string bodyVariation in _availableBodyVariations)
            {
                if (_genie.GetDna(bodyVariation) == 1.0f)
                {
                    return bodyVariation;
                }
            }

            return null;
        }

#region BodyController wrappers
        private static readonly List<BodyAttributeState> Preset = new();

        public GSkelModifierPreset GetCurrentBodyAsPreset()
        {
            GSkelModifierPreset preset = ScriptableObject.CreateInstance<GSkelModifierPreset>();
            preset.StartingBodyVariation = CurrentVariation;
            preset.GSkelModValues ??= new List<GSkelModValue>();

            foreach (string attribute in BodyController.Attributes)
            {
                preset.GSkelModValues.Add(new GSkelModValue
                {
                    Name = attribute,
                    Value = BodyController.GetAttributeWeight(attribute),
                });
            }
            
            return preset;
        }
        
        /// <summary>
        /// Set body variation and gSkel values via a preset
        /// </summary>
        public async UniTask SetPresetAsync(GSkelModifierPreset preset)
        {
            if (BodyController == null)
            {
                return;
            }

            //set body
            await SetBodyVariationAsync(preset.StartingBodyVariation);
            await _genie.RebuildAsync(forceRebuild: true); // do a forced rebuild to ensure refitting blend shapes are added
            
            //set gSkelWeights
            Preset.Clear();
            foreach (GSkelModValue value in preset.GSkelModValues)
            {
                Preset.Add(new BodyAttributeState(value.Name, value.Value));
            }

            BodyController.SetPreset(Preset);
            Preset.Clear();
        }
        
        public bool HasAttribute(string name)
            => BodyController.HasAttribute(name);

        public float GetAttributeWeight(string name)
            => BodyController.GetAttributeWeight(name);

        public void SetAttributeWeight(string name, float weight)
            => BodyController.SetAttributeWeight(name, weight);

        public void SetPreset(IReadOnlyDictionary<string, float> preset)
            => BodyController.SetPreset(preset);

        public void SetPreset(IEnumerable<BodyAttributeState> preset)
            => BodyController.SetPreset(preset);

        public void GetAllAttributeWeights(IDictionary<string, float> results)
            => BodyController.GetAllAttributeWeights(results);

        public Dictionary<string, float> GetAllAttributeWeights()
            => BodyController.GetAllAttributeWeights();

        public void GetAllAttributeStates(ICollection<BodyAttributeState> results)
            => BodyController.GetAllAttributeStates(results);

        public List<BodyAttributeState> GetAllAttributeStates()
            => BodyController.GetAllAttributeStates();
#endregion
    }
}
