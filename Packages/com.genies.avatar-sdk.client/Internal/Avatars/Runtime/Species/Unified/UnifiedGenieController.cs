using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Genies.PerformanceMonitoring;
using Genies.Refs;
using Genies.UGCW.Data;
using Genies.UGCW.Data.DecoratedSkin;
using Newtonsoft.Json;
using UnityEngine;
using Genies.Assets.Services;

namespace Genies.Avatars
{
    /// <summary>
    /// Genie controller for the unified species.
    /// </summary>
    public sealed class UnifiedGenieController : ISpeciesGenieController
    {
        private const string BodyAttributeConfigPath = "Body/BodyAttributesConfigs/ChildJointRegionalized-BodyConfig";
        private static BodyAttributesConfig _bodyAttributesConfig;

        public IGenie Genie => _genie;

        /// <summary>
        /// If true, any asset that you equip will have priority over assets that would normally suppress it. I.e.: if you have
        /// a mask equipped and then equip some glasses, if this is set to false then both assets will stay equipped but only
        /// the mask will be visible since it suppresses any glasses (this is the default behaviour). If this is set to true then
        /// the mask will be unequipped and the glasses will be visible.
        /// </summary>
        public bool UnequipSuppressingOutfitAssets
        {
            get => _unequipSuppressingOutfitAssets;
            set
            {
                _unequipSuppressingOutfitAssets = value;
                RefreshSuppressingSlotsRule();
            }
        }

        // exposed feature controllers
        public IBodyVariationController                BodyVariation => _genie.IsDisposed ? null : _bodyVariationController;
        public IBlendShapeController                   BlendShapes   => _genie.IsDisposed ? null : _blendShapeController;
        public IAssetSlotsController<MaterialAsset>    Materials     => _genie.IsDisposed ? null : _materialController;
        public ISkinColorController                    Skin          => _genie.IsDisposed ? null : _skinColorController;
        public IAssetSlotsController<Texture2DAsset>   Tattoos       => _genie.IsDisposed ? null : _tattooController;
        public IAssetSlotsController<Texture2DAsset>   Makeup        => _genie.IsDisposed ? null : _makeupController;
        public IFlairController                        Flair         => _genie.IsDisposed ? null : _flairController;
        public IAssetSlotsController<MakeupColorAsset> MakeupColors  => _genie.IsDisposed ? null : _makeupColorController;
        public IAssetsController<OutfitAsset>          Outfit        => _genie.IsDisposed ? null : _outfitController;

        // events
        public event Action Updated;

        // dependencies
        private readonly IEditableGenie _genie;
        private readonly IAssetLoader<BlendShapePresetAsset> _blendShapePresetLoader;
        private readonly IAssetLoader<MakeupColorAsset> _makeupColorLoader;

        // feature controllers
        private readonly BodyVariationController _bodyVariationController;
        private readonly BlendShapeController _blendShapeController;
        private readonly MaterialController _materialController;
        private readonly SkinColorController _skinColorController;
        private readonly TattooPresetController _tattooController;
        private readonly MakeupController _makeupController;
        private readonly MakeupColorController _makeupColorController;
        private readonly FlairController _flairController;
        private readonly OutfitController _outfitController;

        // state
        private readonly MegaSkinGenieMaterial _skinMaterial;
        private readonly EquipUnderwearIfNaked _equipUnderwearIfNakedRule;
        private readonly ResolveHairHatConflicts _resolveHairHatConflictsRule;
        private readonly ResolveDeprecatedHairs _resolveDeprecatedHairsRule;
        private readonly RemoveSuppressingSlots _removeSuppressingSlotsRule;
        private bool _unequipSuppressingOutfitAssets;
        private bool _disposed = true; // set by constructor when successful

        // performance monitoring
        private CustomInstrumentationManager _InstrumentationManager => CustomInstrumentationManager.Instance;
        private string _rootTransactionName => CustomInstrumentationOperations.LoadAvatarTransaction;

        public UnifiedGenieController(IEditableGenie genie, AvatarsContext context = null)
        {
            context ??= DefaultAvatarsContext.Instance;

            if (context is null || genie is null || genie.Species != GenieSpecies.Unified)
            {
                genie?.Dispose();
                return;
            }

            if (!context.OutfitMetadataServicesBySpecies.TryGetValue(genie.Species, out IOutfitAssetMetadataService outfitAssetMetadataService))
            {
                genie.Dispose();
                return;
            }

            _disposed = false;
            _genie = genie;
            _genie.Disposed += Dispose; // if for any reason the Genie GameObject is destroyed, then we need to make sure that we also dispose all controllers
            _blendShapePresetLoader = context.BlendShapePresetLoader;
            _makeupColorLoader = context.MakeupColorLoader;

            // instantiate the material slot controllers for the material controller
            var materialSlotControllers = new []
            {
                new HairMaterialSlotController(UnifiedMaterialSlot.Hair),
                new HairMaterialSlotController(UnifiedMaterialSlot.FacialHair),
                new MaterialSlotController(UnifiedMaterialSlot.Eyes)
            };

            // instantiate the skin material for the skin controllers
            _skinMaterial = new MegaSkinGenieMaterial(UnifiedMaterialSlot.Skin, genie.Lod);

            // load body attributes config
            if (!_bodyAttributesConfig)
            {
                _bodyAttributesConfig = Resources.Load<BodyAttributesConfig>(BodyAttributeConfigPath);
            }

            // instantiate controllers
            _bodyVariationController = new BodyVariationController(genie, context.RefittingService, UnifiedBodyVariation.All, _bodyAttributesConfig);
            _blendShapeController = new BlendShapeController(genie, context.BlendShapeLoader, context.BlendShapePresetLoader);
            _materialController = new MaterialController(genie, context.MaterialLoader, materialSlotControllers);
            _skinColorController = new SkinColorController(_skinMaterial, context.SkinColorLoader);
            _tattooController = new TattooPresetController(_skinMaterial, context.TattooLoader, UnifiedTattooSlot.TattooTransformPresets);
            _makeupController = new MakeupController(_skinMaterial, context.MakeupLoader);
            _makeupColorController = new MakeupColorController(_skinMaterial, context.MakeupColorLoader);
            _flairController = new FlairController(genie, context.FlairLoader);
            _outfitController = new OutfitController(genie, context.OutfitAssetLoader, outfitAssetMetadataService);

            // add the skin material to the genie and redirect the rebuilt event
            genie.AddMaterial(_skinMaterial);

            // instantiate some rules that must be initialized later
            _equipUnderwearIfNakedRule = new EquipUnderwearIfNaked(context.OutfitAssetLoader, outfitAssetMetadataService, genie.Lod);
            _resolveHairHatConflictsRule = new ResolveHairHatConflicts(_genie, context.OutfitAssetLoader, outfitAssetMetadataService);
            _resolveDeprecatedHairsRule = new ResolveDeprecatedHairs();
            _removeSuppressingSlotsRule = new RemoveSuppressingSlots(UnifiedOutfitSlotsData.Instance);

            // subscribe to all controllers updated events
            _bodyVariationController.Updated += OnAnyControllerUpdated;
            _blendShapeController.Updated += OnAnyControllerUpdated;
            _materialController.Updated += OnAnyControllerUpdated;
            _skinColorController.Updated += OnAnyControllerUpdated;
            _tattooController.Updated += OnAnyControllerUpdated;
            _makeupController.Updated += OnAnyControllerUpdated;
            _makeupColorController.Updated += OnAnyControllerUpdated;
            _flairController.Updated += OnAnyControllerUpdated;
            _outfitController.Updated += OnAnyControllerUpdated;
        }

        public UniTask InitializeAsync()
        {
            // initialize all controllers
            return UniTask.WhenAll
            (
                _bodyVariationController.SetBodyVariationAsync(UnifiedDefaults.DefaultBodyVariation),
                InitializeBlendShapesAsync(),
                InitializeMaterialsAsync(),
                _skinColorController.LoadAndSetSkinColorAsync(UnifiedDefaults.DefaultSkinColor),
                _makeupColorController.LoadAndSetEquippedAssetsAsync(UnifiedDefaults.DefaultMakeupColors),
                InitializeOutfitAsync()
            );
        }

        public UniTask RebuildGenieAsync(bool forceRebuild = false, bool spreadCompute = false)
        {
            return _genie.RebuildAsync(forceRebuild, spreadCompute);
        }

        /// <summary>
        /// Every time you perform any skin changes the skin will switch to use the MegaSkin shader, which has poor performance.
        /// Call this when done doing changes on the skin to optimize the shader (it's a relatively cheap operation). You can keep
        /// editing the skin after wards but it will get suboptimal again.
        /// </summary>
        public void OptimizeSkin()
        {
            _skinMaterial.Bake();
        }

        /// <summary>
        /// Tries to get the current material instance applied to the renderer for the given slot ID.
        /// Be aware that the returned instance may be destroyed every time the genie is rebuilt.
        /// </summary>
        public bool TryGetSharedMaterial(string slotId, out Material material)
        {
            return _genie.TryGetSharedMaterial(slotId, out material);
        }

        public string GetDefinition()
        {
            // build the dna dictionary
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

            var definition = new AvatarDefinition()
            {
                Species = _genie.Species,
                SubSpecies = _genie.SubSpecies,
                DNA = dna,
                SkinMaterial = _skinColorController.CurrentColor.Id,
                Outfits = new [] { _outfitController.EquippedAssetIds.ToArray() },
            };

            // get materials
            _materialController.TryGetEquippedAssetId(UnifiedMaterialSlot.Hair, out definition.HairMaterial);
            _materialController.TryGetEquippedAssetId(UnifiedMaterialSlot.FacialHair, out definition.FacialhairMaterial);
            _materialController.TryGetEquippedAssetId(UnifiedMaterialSlot.Eyes, out definition.EyeMaterial);
            _flairController.TryGetEquippedAssetId(UnifiedMaterialSlot.Eyebrows, out definition.EyebrowFlair);
            _flairController.TryGetEquippedAssetId(UnifiedMaterialSlot.Eyelashes, out definition.EyelashFlair);
            _flairController.TryGetEquippedColorPresetId(UnifiedMaterialSlot.Eyebrows, out definition.EyebrowColorPreset, out definition.EyebrowColors);
            _flairController.TryGetEquippedColorPresetId(UnifiedMaterialSlot.Eyelashes, out definition.EyelashColorPreset, out definition.EyelashColors);


            definition.SetAvatarFeature(GetDecoratedSkinDefinition());

            return JsonConvert.SerializeObject(definition);
        }

        public async UniTask SetDefinitionAsync(string definition)
        {
            var unifiedDefinition = await TryToDeserializeDefinition(definition);
            if (unifiedDefinition == null)
            {
                return;
            }

            if (unifiedDefinition.Species != _genie.Species)
            {
                Debug.LogError($"[{nameof(UnifiedGenieController)}] cannot set the given avatar definition because it is not for the unified species. Given definition:\n{definition}");
                return;
            }

            // sentry performance monitoring
            _InstrumentationManager.SetExtraData(_rootTransactionName, "Definition", definition);

            var materials = new []
            {
                (unifiedDefinition.HairMaterial,       UnifiedMaterialSlot.Hair),
                (unifiedDefinition.FacialhairMaterial, UnifiedMaterialSlot.FacialHair),
                (unifiedDefinition.EyeMaterial,        UnifiedMaterialSlot.Eyes),
            };

            //adding the outfits and eyebrow eyelash gear
            var outfit = new List<string>(unifiedDefinition.Outfits[0]);
            outfit.Add(string.IsNullOrEmpty(unifiedDefinition.EyebrowGear) ? UnifiedDefaults.DefaultEyebrowGear : unifiedDefinition.EyebrowGear);
            outfit.Add(string.IsNullOrEmpty(unifiedDefinition.EyelashGear) ? UnifiedDefaults.DefaultEyelashGear : unifiedDefinition.EyelashGear);

            await UniTask.WhenAll
            (
                MonitorSetBodyVariationAsync(unifiedDefinition),
                _blendShapeController.LoadAndSetEquippedAssetsAsync(unifiedDefinition.FaceVarBlendShapesFromDna()),
                _materialController.LoadAndSetEquippedAssetsAsync(materials),
                MonitorLoadAndSetSkinColorAsync(unifiedDefinition),
                SetDecoratedSkinDefinitionAsync(unifiedDefinition),
                MonitorSetGSkelWeights(unifiedDefinition),
                _outfitController.LoadAndSetEquippedAssetsAsync(outfit),

                //set the default if theres no one from the definition
                _flairController.LoadAndEquipAssetOrDefaultAsync(unifiedDefinition.EyebrowFlair,
                    UnifiedMaterialSlot.Eyebrows),
                _flairController.LoadAndEquipAssetOrDefaultAsync(unifiedDefinition.EyelashFlair,
                    UnifiedMaterialSlot.Eyelashes),

                _flairController.LoadAndEquipColorOrDefaultAsync(unifiedDefinition.EyebrowColorPreset,
                    unifiedDefinition.EyebrowColors,
                    UnifiedMaterialSlot.Eyebrows),
                _flairController.LoadAndEquipColorOrDefaultAsync(unifiedDefinition.EyelashColorPreset,
                    unifiedDefinition.EyelashColors,
                    UnifiedMaterialSlot.Eyelashes)
            );

            await RebuildGenieAsync();
        }

        private async UniTask MonitorSetBodyVariationAsync(AvatarDefinition unifiedDefinition)
        {
            string bodyVariation = unifiedDefinition.BinaryGenderStringFromDna();
            await _InstrumentationManager.WrapAsyncTaskWithSpan(
                () => _bodyVariationController.SetBodyVariationAsync(bodyVariation), _rootTransactionName,
                "bodyVariationController.SetBodyVariationAsync", $"bodyVariation: {bodyVariation}");
        }

        private async UniTask MonitorLoadAndSetSkinColorAsync(AvatarDefinition unifiedDefinition)
        {
            string assetId = unifiedDefinition.SkinMaterial;
            await _InstrumentationManager.WrapAsyncTaskWithSpan(
                () => _skinColorController.LoadAndSetSkinColorAsync(assetId), _rootTransactionName,
                "skinColorController.LoadAndSetSkinColorAsync", assetId);
        }

        private async UniTask MonitorSetGSkelWeights(AvatarDefinition unifiedDefinition)
        {
            Dictionary<string, float> preset = unifiedDefinition.GetBodyAttributesPreset(_bodyAttributesConfig);
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
            _materialController.Updated -= OnAnyControllerUpdated;
            _skinColorController.Updated -= OnAnyControllerUpdated;
            _tattooController.Updated -= OnAnyControllerUpdated;
            _makeupController.Updated -= OnAnyControllerUpdated;
            _makeupColorController.Updated -= OnAnyControllerUpdated;
            _flairController.Updated -= OnAnyControllerUpdated;
            _outfitController.Updated -= OnAnyControllerUpdated;

            // dispose controllers
            _bodyVariationController.Dispose();
            _materialController.Dispose();
            _skinColorController.Dispose();
            _tattooController.Dispose();
            _makeupController.Dispose();
            _makeupColorController.Dispose();
            _flairController.Dispose();
            _blendShapeController.Dispose();
            _outfitController.Dispose();

            // remove skin material from genie and dispose it
            _genie.RemoveMaterial(_skinMaterial);
            _skinMaterial.Dispose();

            // dispose the rules
            _equipUnderwearIfNakedRule.Dispose();
            _resolveHairHatConflictsRule.Dispose();

            // dispose genie
            _genie.Dispose();
        }

#region INITIALIZATION
        private UniTask InitializeMaterialsAsync()
        {
            return _materialController.LoadAndSetEquippedAssetsAsync(new []
            {
                (UnifiedDefaults.DefaultHairColor,       UnifiedMaterialSlot.Hair),
                (UnifiedDefaults.DefaultFacialHairColor, UnifiedMaterialSlot.FacialHair),
                (UnifiedDefaults.DefaultEyesColor,       UnifiedMaterialSlot.Eyes),
            });
        }

        private UniTask InitializeBlendShapesAsync()
        {
            /**
             * For gen12 we decided that default blend shapes will be the base mesh, meaning no default blend shapes
             */
            return UniTask.CompletedTask;

            // for now, Unified species only have face blend shapes, so lets load the default face preset and set its blend shapes as the defaults
            // using Ref<BlendShapePresetAsset> assetRef = await _blendShapePresetLoader.LoadAsync(UnifiedDefaults.DefaultFacePresetId);
            // if (assetRef.IsAlive)
            //     await _blendShapeController.SetDefaultBlendShapesAsync(assetRef.Item.BlendShapeAssets);
        }

        private async UniTask InitializeOutfitAsync()
        {
            // initialize some rules
            await UniTask.WhenAll
            (
                _equipUnderwearIfNakedRule.InitializeAsync(),
                _resolveHairHatConflictsRule.InitializeAsync()
            );

            // setup rules to execute before an item is equipped to the outfit
            _outfitController.EquippingAdjustmentRules.Add(new RemoveIncompatibleAssets(UnifiedOutfitSlotsData.Instance));

            // setup rules to execute when the outfit is being validated
            _outfitController.ValidationRules.Add(new EnsureAllAssetsAreCompatible(UnifiedOutfitSlotsData.Instance));

            // setup rules to execute for resolving the validated outfit prior to equipping it to the genie
            _outfitController.ResolutionRules.Add(_equipUnderwearIfNakedRule);
            _outfitController.ResolutionRules.Add(_resolveHairHatConflictsRule);
            _outfitController.ResolutionRules.Add(_resolveDeprecatedHairsRule);
            _outfitController.ResolutionRules.Add(new ResolvePantsShoesConflicts(_genie));
            _outfitController.ResolutionRules.Add(new RemoveSuppressedSlots(UnifiedOutfitSlotsData.Instance));

            RefreshSuppressingSlotsRule();

            // validate equipped assets with the new rules so any default assets are loaded and equipped
            await _outfitController.ValidateAndResolveAssetsAsync();
        }
#endregion

        private DecoratedSkinDefinition GetDecoratedSkinDefinition()
        {
            IReadOnlyList<string> slots = UnifiedTattooSlot.All;
            var tattoos = new TattooDefinition[slots.Count];
            for (int i = 0; i < slots.Count; ++i)
            {
                if (!_tattooController.TryGetEquippedAssetId(slots[i], out string tattooId))
                {
                    continue;
                }

                tattoos[i] = new TattooDefinition { Name = tattooId, BodyPartId = slots[i] };
            }

            var makeup = new MakeupDefinition();
            _makeupController.TryGetEquippedAssetId(MakeupSlot.Stickers, out makeup.Stickers);
            _makeupController.TryGetEquippedAssetId(MakeupSlot.Lipstick, out makeup.Lipstick);
            _makeupController.TryGetEquippedAssetId(MakeupSlot.Freckles, out makeup.Freckles);
            _makeupController.TryGetEquippedAssetId(MakeupSlot.FaceGems, out makeup.FaceGems);
            _makeupController.TryGetEquippedAssetId(MakeupSlot.Eyeshadow, out makeup.Eyeshadow);
            _makeupController.TryGetEquippedAssetId(MakeupSlot.Blush, out makeup.Blush);

            GetSerializedMakeupColor(MakeupSlot.Lipstick, out makeup.LipstickColor1, out makeup.LipstickColor2, out makeup.LipstickColor3);
            GetSerializedMakeupColor(MakeupSlot.Freckles, out makeup.FrecklesColor, out _, out _);
            GetSerializedMakeupColor(MakeupSlot.FaceGems, out makeup.FaceGemsColor1, out makeup.FaceGemsColor2, out makeup.FaceGemsColor3);
            GetSerializedMakeupColor(MakeupSlot.Eyeshadow, out makeup.EyeshadowColor1, out makeup.EyeshadowColor2, out makeup.EyeshadowColor3);
            GetSerializedMakeupColor(MakeupSlot.Blush, out makeup.BlushColor1, out makeup.BlushColor2, out makeup.BlushColor3);

            return new DecoratedSkinDefinition()
            {
                Tattoos = tattoos,
                Makeup = makeup
            };
        }

        private async UniTask SetDecoratedSkinDefinitionAsync(AvatarDefinition unifiedDefinition)
        {
            if (!unifiedDefinition.TryGetAvatarFeature(AvatarFeatureType.DecoratedSkin, out DecoratedSkinDefinition skinDefinition))
            {
                // if this unified definition does not contain a decorated skin definition, then we must reset tattoos and makeup
                await UniTask.WhenAll
                (
                    _tattooController.ClearAllSlotsAsync(),
                    _makeupController.ClearAllSlotsAsync(),
                    _makeupColorController.ClearAllSlotsAsync()
                );

                return;
            }

            // fetch tattoo asset and slot IDs from the skin definition
            IEnumerable<(string assetId, string slotId)> tattoos = skinDefinition.Tattoos?
                .Where(tattoo => tattoo != null)
                .Select(tattoo => (tattoo.Name, tattoo.BodyPartId));

            // fetch makeup assets from the skin definition
            (string assetId, string slotId)[] makeup = skinDefinition.Makeup is null ? null : new []
            {
                (skinDefinition.Makeup.Stickers,  MakeupSlot.Stickers),
                (skinDefinition.Makeup.Lipstick,  MakeupSlot.Lipstick),
                (skinDefinition.Makeup.Freckles,  MakeupSlot.Freckles),
                (skinDefinition.Makeup.FaceGems,  MakeupSlot.FaceGems),
                (skinDefinition.Makeup.Eyeshadow, MakeupSlot.Eyeshadow),
                (skinDefinition.Makeup.Blush,     MakeupSlot.Blush)
            };

            // fetch makeup colors from the skin definition
            // TODO we should include the color IDs in the definition if we want to be consistent with the other controllers, right now the definition only contains the serialized HTML colors
            MakeupDefinition makeupDefinition = skinDefinition.Makeup;
            (Ref<MakeupColorAsset> color, string slotId)[] makeupColors = null;
            if (makeupDefinition != null)
            {
                makeupColors = await UniTask.WhenAll(new []
                {
                    CreateMakeupColorAssetAsync(MakeupSlot.Lipstick,  makeupDefinition.LipstickColor1,  makeupDefinition.LipstickColor2,  makeupDefinition.LipstickColor3),
                    CreateMakeupColorAssetAsync(MakeupSlot.Freckles,  makeupDefinition.FrecklesColor),
                    CreateMakeupColorAssetAsync(MakeupSlot.FaceGems,  makeupDefinition.FaceGemsColor1,  makeupDefinition.FaceGemsColor2,  makeupDefinition.FaceGemsColor3),
                    CreateMakeupColorAssetAsync(MakeupSlot.Eyeshadow, makeupDefinition.EyeshadowColor1, makeupDefinition.EyeshadowColor2, makeupDefinition.EyeshadowColor3),
                    CreateMakeupColorAssetAsync(MakeupSlot.Blush,     makeupDefinition.BlushColor1,     makeupDefinition.BlushColor2,     makeupDefinition.BlushColor3)
                });
            }

            await UniTask.WhenAll
            (
                _tattooController.LoadAndSetEquippedAssetsAsync(tattoos),
                _makeupController.LoadAndSetEquippedAssetsAsync(makeup),
                _makeupColorController.SetEquippedAssetsAsync(makeupColors)
            );
        }

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

        private void GetSerializedMakeupColor(string slotId, out string color1, out string color2, out string color3)
        {
            color1 = color2 = color3 = null;
            if (!_makeupColorController.TryGetEquippedAsset(slotId, out _, out Ref<MakeupColorAsset> assetRef))
            {
                return;
            }

            MakeupColorAsset makeupColorAsset = assetRef.Item;
            assetRef.Dispose();
            color1 = $"#{ColorUtility.ToHtmlStringRGBA(makeupColorAsset.Color1)}";
            color2 = $"#{ColorUtility.ToHtmlStringRGBA(makeupColorAsset.Color2)}";
            color3 = $"#{ColorUtility.ToHtmlStringRGBA(makeupColorAsset.Color3)}";
        }

        private void OnAnyControllerUpdated()
        {
            Updated?.Invoke();
        }

        private async UniTask<(Ref<MakeupColorAsset> color, string slotId)> CreateMakeupColorAssetAsync(string slotId, string color1 = null,
            string color2 = null, string color3 = null)
        {
            // right now, when the string is an empty string it means there is no defined color, so we have to load the default
            if (color1 == string.Empty)
            {
                (Ref<MakeupColorAsset> color, string slotId) colorAsset = await LoadDefaultMakeupColorAssetAsync(slotId);
                if (colorAsset.color.IsAlive)
                {
                    return colorAsset;
                }

                // if default could not be loaded then generate a default color asset (all colors white)
            }

            Color parsedColor1 = default;
            Color parsedColor2 = default;
            Color parsedColor3 = default;

            if (color1 != null)
            {
                ColorUtility.TryParseHtmlString(color1, out parsedColor1);
            }

            if (color2 != null)
            {
                ColorUtility.TryParseHtmlString(color2, out parsedColor2);
            }

            if (color3 != null)
            {
                ColorUtility.TryParseHtmlString(color3, out parsedColor3);
            }

            var makeupColor = new MakeupColorAsset(parsedColor1, parsedColor2, parsedColor3);
            return (CreateRef.FromAny(makeupColor), slotId);
        }

        private async UniTask<(Ref<MakeupColorAsset> color, string slotId)> LoadDefaultMakeupColorAssetAsync(string slotId)
        {
            foreach ((string assetId, string assetSlotId) in UnifiedDefaults.DefaultMakeupColors)
            {
                if (assetSlotId != slotId)
                {
                    continue;
                }

                Ref<MakeupColorAsset> color = await _makeupColorLoader.LoadAsync(assetId);
                return (color, slotId);
            }

            return default;
        }

        private static async UniTask<AvatarDefinition> TryToDeserializeDefinition(string definition)
        {
            AvatarDefinition unifiedDefinition = null;
            try
            {
                unifiedDefinition = await definition.DeserializeToAvatarDefinitionAsync();
                if (unifiedDefinition == null)
                {
                    Debug.LogError($"[{nameof(UnifiedGenieController)}] could not deserialize unified definition. The result is null");
                }

                return unifiedDefinition;
            }
            catch (Exception exception)
            {
                Debug.LogError($"[{nameof(UnifiedGenieController)}] could not deserialize unified definition:\n{exception}");
                return unifiedDefinition;
            }
        }
    }
}
