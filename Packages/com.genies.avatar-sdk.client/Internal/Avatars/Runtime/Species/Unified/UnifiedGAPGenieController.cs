using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Genies.Avatars;
using Genies.PerformanceMonitoring;
using Genies.Refs;
using Genies.UGCW.Data;
using Genies.UGCW.Data.DecoratedSkin;
using Newtonsoft.Json;
using Unity.Collections;
using UnityEngine;
using Genies.Assets.Services;

namespace Genies.Avatars
{
    using AvatarDefinition = Genies.Avatars.AvatarDefinition;

    /// <summary>
    /// Genie controller for the unified GAP species.
    /// </summary>
    public sealed class UnifiedGAPGenieController : ISpeciesGenieController
    {
        private const string BodyAttributeConfigPath = "Body/BodyAttributesConfigs/ChildJointRegionalized-BodyConfig";
        private static BodyAttributesConfig _bodyAttributesConfig;

        public IGenie Genie => _genie;

        // exposed feature controllers
        public IBodyVariationController                BodyVariation => _genie.IsDisposed ? null : _bodyVariationController;
        public IBlendShapeController                   BlendShapes   => _genie.IsDisposed ? null : _blendShapeController;
        public IAssetsController<OutfitAsset>          Outfit        => _genie.IsDisposed ? null : _outfitController;

        // events
        public event Action Updated;

        // dependencies
        private readonly IEditableGenie _genie;
        private readonly IAssetLoader<BlendShapePresetAsset> _blendShapePresetLoader;

        // feature controllers
        private readonly BodyVariationController _bodyVariationController;
        private readonly BlendShapeController _blendShapeController;
        private readonly OutfitController _outfitController;
        private readonly SubSpeciesController _subSpeciesController;

        // state
        private readonly EquipUnderwearIfNaked _equipUnderwearIfNakedRule;
        private readonly RemoveSuppressingSlots _removeSuppressingSlotsRule;
        private readonly RemoveDnaForGAP _removeDnaFromGapRule;
        private bool _unequipSuppressingOutfitAssets;
        private bool _disposed = true; // set by constructor when successful

        // performance monitoring
        private CustomInstrumentationManager _InstrumentationManager => CustomInstrumentationManager.Instance;
        private string _rootTransactionName => CustomInstrumentationOperations.LoadAvatarTransaction;

        public UnifiedGAPGenieController(IEditableGenie genie, AvatarsContext context = null)
        {
            context ??= DefaultAvatarsContext.Instance;
            if (context is null || genie is null || genie.Species != GenieSpecies.UnifiedGAP)
            {
                genie?.Dispose();
                return;
            }

            if (!context.OutfitMetadataServicesBySpecies.TryGetValue(genie.Species, out IOutfitAssetMetadataService outfitAssetMetadataService))
            {
                Debug.LogError($"Could not find outfit asset metadata service for species {genie.Species}.");
                genie.Dispose();
                return;
            }

            _disposed = false;
            _genie = genie;
            _genie.Disposed += Dispose; // if for any reason the Genie GameObject is destroyed, then we need to make sure that we also dispose all controllers
            _blendShapePresetLoader = context.BlendShapePresetLoader;

            // load body attributes config
            if (!_bodyAttributesConfig)
            {
                _bodyAttributesConfig = Resources.Load<BodyAttributesConfig>(BodyAttributeConfigPath);
            }

            // instantiate subSpeciesController
            _subSpeciesController = new SubSpeciesController((EditableGenie) genie, (SubSpeciesLoader) context.SubSpeciesLoader, _genie.SubSpecies);

            // generate new RefittingService and update context
            context.RefittingService = new RsRefittingService(_subSpeciesController.CreateReferenceShape());

            // get available body variations, which should initially just be the initialized subSpecies
            List<string> bodyVariations = new List<string> { _genie.SubSpecies };

            // instantiate other controllers
            _bodyVariationController = new BodyVariationController(genie, context.RefittingService, bodyVariations, _bodyAttributesConfig);
            _blendShapeController = new BlendShapeController(genie, context.BlendShapeLoader, context.BlendShapePresetLoader);
            _outfitController = new OutfitController(genie, context.OutfitAssetLoader, outfitAssetMetadataService);

            // instantiate some rules that must be initialized later
            _equipUnderwearIfNakedRule = new EquipUnderwearIfNaked(context.OutfitAssetLoader, outfitAssetMetadataService, genie.Lod);
            _removeSuppressingSlotsRule = new RemoveSuppressingSlots(UnifiedOutfitSlotsData.Instance);
            _removeDnaFromGapRule = new RemoveDnaForGAP();

            // subscribe to all controllers updated events
            _bodyVariationController.Updated += OnAnyControllerUpdated;
            _blendShapeController.Updated += OnAnyControllerUpdated;
            _outfitController.Updated += OnAnyControllerUpdated;
        }

        public async UniTask InitializeAsync()
        {
            await _bodyVariationController.SetBodyVariationAsync(_genie.SubSpecies);
            await InitializeOutfitAsync();
        }

        public UniTask RebuildGenieAsync(bool forceRebuild = false, bool spreadCompute = false)
        {
            return _genie.RebuildAsync(forceRebuild, spreadCompute);
        }

        public string GetDefinition()
        {
            // Build the dna dictionary
            var dna = new Dictionary<string, float>()
            {
                { _bodyVariationController.CurrentVariation, 1.0f }
            };

            // add the blend shapes to the dna
            foreach (string assetId in _blendShapeController.EquippedAssetIds)
            {
                if (!_blendShapeController.TryGetEquippedAsset(assetId, out Ref<BlendShapeAsset> assetRef))
                {
                    continue;
                }

                BlendShapeAsset asset = assetRef.Item;
                assetRef.Dispose();

                if (asset.Dna is null)
                {
                    continue;
                }

                foreach (DnaEntry dnaEntry in asset.Dna)
                {
                    dna[dnaEntry.Name] = dnaEntry.Value;
                }
            }

            // add each body attribute to dna
            foreach (BodyAttributeState attributeState in _bodyVariationController.GetAllAttributeStates())
            {
                dna[attributeState.name] = attributeState.weight;
            }

            var definition = new global::Genies.Avatars.AvatarDefinition()
            {
                Species = _genie.Species,
                SubSpecies = _genie.SubSpecies,
                DNA = dna,
                Outfits = new [] { _outfitController.EquippedAssetIds.ToArray() },
            };

            return JsonConvert.SerializeObject(definition);
        }

        public async UniTask SetDefinitionAsync(string definition)
        {
            var unifiedDefinition = await TryToDeserializeDefinition(definition);
            if (unifiedDefinition == null)
            {
                return;
            }

            if (unifiedDefinition.Species != _genie.Species )
            {
                throw new Exception($"[{nameof(UnifiedGAPGenieController)}] cannot set the given avatar definition " +
                               $"because it is not using the unifiedGAP species. Given definition:\n{definition}");
            }

            // sentry performance monitoring
            _InstrumentationManager.SetExtraData(_rootTransactionName, "Definition", definition);

            // refreshing outfit
            var outfit = new List<string>(unifiedDefinition.Outfits[0]);
            await UniTask.WhenAll(
                _outfitController.UnequipAllAssetsAsync(),
                _outfitController.LoadAndSetEquippedAssetsAsync(outfit),
                LoadAndEquipSubSpeciesAsync(unifiedDefinition.SubSpecies),
                _blendShapeController.LoadAndSetEquippedAssetsAsync(unifiedDefinition.FaceVarBlendShapesFromDna()),
                MonitorSetGSkelWeights(unifiedDefinition.GetBodyAttributesPreset(_bodyAttributesConfig)));

            await RebuildGenieAsync();
        }

        /// <summary>
        /// Loads SubSpecies from provided ID and equips it onto the Genie instance via the SubSpeciesController.
        /// Also updates body variation and outfit refitting accordingly.
        /// </summary>
        public async UniTask LoadAndEquipSubSpeciesAsync(string assetId)
        {
            // load and equip asset via controller
            var asset = await _subSpeciesController.LoadAssetAsync(assetId);
            await _subSpeciesController.EquipAssetAsync(asset);

            // apply body variation
            string bodyVariation = asset.Item.Id;
            if (!_bodyVariationController.BodyVariationAvailable(bodyVariation))
            {
                // get new deform shape for refitting service
                var deformShapeArray = new NativeArray<Vector3>(_subSpeciesController.GetEquippedDeformShapeArray(), Allocator.Temp);

                // add deform shape to refitting service and refresh assets
                _bodyVariationController.AddBodyVariation(asset.Item.Id, deformShapeArray);
            }

            // set body variation
            await MonitorSetBodyVariationAsync(bodyVariation);
        }

        private async UniTask MonitorSetBodyVariationAsync(string bodyVariation)
        {
            await _InstrumentationManager.WrapAsyncTaskWithSpan(
                () => _bodyVariationController.SetBodyVariationAsync(bodyVariation), _rootTransactionName,
                "bodyVariationController.SetBodyVariationAsync", $"bodyVariation: {bodyVariation}");
        }

        private async UniTask MonitorSetGSkelWeights(Dictionary<string, float> preset)
        {
            await _InstrumentationManager.WrapAsyncTaskWithSpan(
                () =>
                {
                    _bodyVariationController.SetPreset(preset);
                    return UniTask.CompletedTask;
                },_rootTransactionName,
                "bodyVariationController.SetGSkelWeights", preset);
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;

            // unsubscribe from all controllers updated events
            _bodyVariationController.Updated -= OnAnyControllerUpdated;
            _blendShapeController.Updated -= OnAnyControllerUpdated;
            _outfitController.Updated -= OnAnyControllerUpdated;

            // dispose controllers
            _subSpeciesController.Dispose();
            _bodyVariationController.Dispose();
            _blendShapeController.Dispose();
            _outfitController.Dispose();

            // dispose the rules
            _equipUnderwearIfNakedRule.Dispose();

            // dispose genie
            _genie.Dispose();
        }

#region INITIALIZATION


        private async UniTask InitializeOutfitAsync()
        {
            // initialize rules
            await _equipUnderwearIfNakedRule.InitializeAsync();

            // setup rules to execute before an item is equipped to the outfit
            _outfitController.EquippingAdjustmentRules.Add(new RemoveIncompatibleAssets(UnifiedOutfitSlotsData.Instance));

            // setup rules to execute when the outfit is being validated
            _outfitController.ValidationRules.Add(new EnsureAllAssetsAreCompatible(UnifiedOutfitSlotsData.Instance));

            // setup rules to execute for resolving the validated outfit prior to equipping it to the genie
            _outfitController.ResolutionRules.Add(_equipUnderwearIfNakedRule);
            _outfitController.ResolutionRules.Add(_removeDnaFromGapRule);
            _outfitController.ResolutionRules.Add(new ResolvePantsShoesConflicts(_genie));
            _outfitController.ResolutionRules.Add(new RemoveSuppressedSlots(UnifiedOutfitSlotsData.Instance));

            RefreshSuppressingSlotsRule();

            // validate equipped assets with the new rules so any default assets are loaded and equipped
            await _outfitController.ValidateAndResolveAssetsAsync();
        }
#endregion

        private void RefreshSuppressingSlotsRule()
        {
            if (_unequipSuppressingOutfitAssets && !_outfitController.EquippingAdjustmentRules.Contains(_removeSuppressingSlotsRule))
            {
                _outfitController.EquippingAdjustmentRules.Add(_removeSuppressingSlotsRule);
            }
            else
            {
                _outfitController.EquippingAdjustmentRules.Remove(_removeSuppressingSlotsRule);
            }
        }

        private void OnAnyControllerUpdated()
        {
            Updated?.Invoke();
        }

        public static async UniTask<global::Genies.Avatars.AvatarDefinition> TryToDeserializeDefinition(string definition)
        {
            global::Genies.Avatars.AvatarDefinition unifiedDefinition = null;
            try
            {
                unifiedDefinition = await definition.DeserializeToAvatarDefinitionAsync();
                if (unifiedDefinition == null)
                {
                    Debug.LogError($"[{nameof(UnifiedGAPGenieController)}] could not deserialize unified definition. The result is null");
                }

                return unifiedDefinition;
            }
            catch (Exception exception)
            {
                Debug.LogError($"[{nameof(UnifiedGAPGenieController)}] could not deserialize unified definition:\n{exception}");
                return unifiedDefinition;
            }
        }

    }
}
