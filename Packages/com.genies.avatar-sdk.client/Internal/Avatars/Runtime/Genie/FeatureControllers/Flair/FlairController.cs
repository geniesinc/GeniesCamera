using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Genies.Refs;
using Genies.CrashReporting;
using UnityEngine;

namespace Genies.Avatars
{
    public class FlairController : AssetSlotsController<FlairAsset>, IFlairController
    {
        private readonly IEditableGenie _genie;
        private readonly ISlottedAssetLoader<FlairAsset> _loader;

        private static readonly Color[] _defaultBlackPreset = {Color.black, Color.black, Color.black, Color.black};

        // State
        private Dictionary<string, FlairColorPreset> _equippedColorPresets;
        private Dictionary<string, FlairMaterial> _flairMaterials;

        public Dictionary<string, FlairColorPreset> EquippedColorPresets => _equippedColorPresets;
        public Dictionary<string, FlairAsset> EquippedPresets { get; } = new Dictionary<string, FlairAsset>();

        // Static config
        public static string[] DefaultSlots = new string[]{UnifiedMaterialSlot.Eyebrows, UnifiedMaterialSlot.Eyelashes};

        // String consts
        private const string _albedoTransparency = "_AlbedoTransparency";
        private const string _normal = "_Normal";
        private const string _metallicSmoothness = "_MetallicSmoothness";
        private const string _rgbaMask = "_RGBAMask";
        private const string _colorBase = "_ColorBase";
        private const string _colorR = "_Color_R";
        private const string _colorG = "_Color_G";
        private const string _colorB = "_Color_B";

        public FlairController(IEditableGenie genie, ISlottedAssetLoader<FlairAsset> loader)
        {
            _genie = genie;

            _loader = loader;

            _flairMaterials = new Dictionary<string, FlairMaterial>();
            _equippedColorPresets = new Dictionary<string, FlairColorPreset>();

            foreach (string slot in DefaultSlots)
            {
                var mat = new FlairMaterial(slot);
                _flairMaterials[slot] = mat;
                _genie.AddMaterial(mat);

                _equippedColorPresets[slot] = new FlairColorPreset();
            }
        }
        protected override bool IsSlotValid(string slotId)
        {
            bool isValid = DefaultSlots.Contains(slotId);
            if (!isValid)
            {
                CrashReporter.LogError($"Invalid slot ID: {slotId}");
            }

            return isValid;
        }

        protected override UniTask<Ref<FlairAsset>> LoadAssetAsync(string assetId, string slotId)
        {
            return _loader.LoadAsync(assetId, slotId);
        }

        protected override UniTask OnAssetEquippedAsync(FlairAsset asset, string slotId)
        {
            return EquipFlair(asset, slotId);
        }

        protected override UniTask OnAssetUnequippedAsync(FlairAsset asset, string slotId)
        {
            return UniTask.CompletedTask;
        }

        public UniTask LoadAndEquipAssetOrDefaultAsync(string assetId, string slotId)
        {
            if (string.IsNullOrEmpty(assetId))
            {
                string defaultId = GetDefaultForSlot(slotId);
                return LoadAndEquipAssetAsync(defaultId, slotId);
            }
            else
            {
                return LoadAndEquipAssetAsync(assetId, slotId);
            }
        }

        private string GetDefaultForSlot(string slotId)
        {
            switch (slotId)
            {
                case UnifiedMaterialSlot.Eyebrows:
                    return UnifiedDefaults.DefaultEyebrowTexturePreset;
                case UnifiedMaterialSlot.Eyelashes:
                    return UnifiedDefaults.DefaultEyelashTexturePreset;
                default:
                    return null;
            }
        }

        private string[] GetDefaultColorForSlot(string slotId)
        {
            switch (slotId)
            {
                case UnifiedMaterialSlot.Eyebrows:
                    return UnifiedDefaults.DefaultEyebrowColors;
                case UnifiedMaterialSlot.Eyelashes:
                    return UnifiedDefaults.DefaultEyelashColors;
                default:
                    return null;
            }
        }

        public override void Dispose()
        {
            foreach (var kvp in _flairMaterials)
            {
                _genie.RemoveMaterial(kvp.Value);
                kvp.Value.Dispose();
            }

            base.Dispose();
        }

        public bool TryGetEquippedColorPresetId(string slot, out string presetId, out string[] colors)
        {
            presetId = null;
            colors = null;

            bool succeeded = _equippedColorPresets.TryGetValue(slot, out var preset);

            if (succeeded)
            {
                presetId = preset.Guid;
                colors = TrySerializeColorList(preset.Colors);
            }

            return succeeded;
        }

        private UniTask EquipFlair(FlairAsset asset, string slot)
        {
            if(asset is null)
            {
                return UniTask.CompletedTask;
            }

            TrySetFlairMaterialProps(_flairMaterials[slot], asset);

            //register the usage per type
            if (EquippedPresets.TryGetValue(slot, out _))
            {
                EquippedPresets[slot] = asset;
            }
            else
            {
                EquippedPresets.Add(slot, asset);
            }
            
            return UniTask.CompletedTask;
        }

        private bool TrySetFlairMaterialProps(FlairMaterial material, FlairAsset asset)
        {
            try
            {
                material.Material.SetTexture(_albedoTransparency, asset.AlbedoTransparency);
                material.Material.SetTexture(_normal, asset.Normal);
                material.Material.SetTexture(_metallicSmoothness, asset.MetallicSmoothness);
                material.Material.SetTexture(_rgbaMask, asset.RgbaMask);

                material.NotifyUpdate();


                return true;
            }
            catch (Exception e)
            {
                CrashReporter.LogError($"Failed to set material properties for asset {asset.Id}: {e.Message}");
                return false;
            }
        }

        public UniTask LoadAndEquipColorOrDefaultAsync(string presetId, string[] colors, string slotId)
        {
            if (string.IsNullOrEmpty(presetId) && colors?.Length == 0)
            {
                string[] defaultColors = GetDefaultColorForSlot(slotId);
                switch (slotId)
                {
                    case UnifiedMaterialSlot.Eyebrows:
                        return EquipColorPreset(UnifiedDefaults.DefaultEyebrowColorPreset, TryParseColorList(defaultColors),  slotId);
                    case UnifiedMaterialSlot.Eyelashes:
                        return EquipColorPreset(UnifiedDefaults.DefaultEyelashColorPreset, TryParseColorList(defaultColors),  slotId);
                }

            }

            return EquipColorPreset(presetId, TryParseColorList(colors), slotId);
        }
        public UniTask EquipColorPreset(string presetId, Color[] colorPreset, string slot)
        {
            TrySetColorPresetProps(presetId, _flairMaterials[slot], colorPreset);
            return UniTask.CompletedTask;
        }

        public UniTask UnequipColorPreset(string slot)
        {
            TrySetColorPresetProps(null, _flairMaterials[slot], _defaultBlackPreset);
            return UniTask.CompletedTask;
        }

        private bool TrySetColorPresetProps(string presetId, FlairMaterial material, Color[] colorPreset)
        {
            if (colorPreset.Length != 4)
            {
                CrashReporter.LogError($"Color preset must have exactly 4 colors (Got {colorPreset.Length})");
                return false;
            }

            material.Material.SetColor(_colorBase, colorPreset[0]);
            material.Material.SetColor(_colorR, colorPreset[1]);
            material.Material.SetColor(_colorG, colorPreset[2]);
            material.Material.SetColor(_colorB, colorPreset[3]);

            _equippedColorPresets[material.SlotId].Guid = presetId;
            _equippedColorPresets[material.SlotId].Colors = colorPreset;

            material.NotifyUpdate();

            return true;
        }

        private Color[] TryParseColorList(string[] colorHexList)
        {
            if (colorHexList == null)
            {
                return _defaultBlackPreset;
            }

            var colors = new Color[colorHexList.Length];

            for (var i = 0; i < colorHexList.Length; i++)
            {
                ColorUtility.TryParseHtmlString(colorHexList[i], out Color rawColor);
                colors[i] = rawColor;
            }

            return colors;
        }

        private string[] TrySerializeColorList(Color[] colorList)
        {
            if (colorList == null)
            {
                return TrySerializeColorList(_defaultBlackPreset);
            }

            var colors = new string[colorList.Length];

            for (var i = 0; i < colorList.Length; i++)
            {
                colors[i] = $"#{ColorUtility.ToHtmlStringRGBA(colorList[i])}";
            }

            return colors;
        }
    }
}
