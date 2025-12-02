using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Genies.Refs;
using UnityEngine;
using Genies.Assets.Services;

namespace Genies.Avatars
{
    /// <summary>
    /// Used to control the makeup colors from a <see cref="MegaSkinGenieMaterial"/> instance.
    /// Any changes made will set the skin material dirty.
    /// </summary>
    public sealed class MakeupColorController : AssetSlotsController<MakeupColorAsset>
    {
        private static readonly HashSet<string> Slots = new HashSet<string>(MakeupSlot.All);

        // dependencies
        private readonly MegaSkinGenieMaterial _skinMaterial;
        private readonly IAssetLoader<MakeupColorAsset> _makeupColorLoader;

        // state
        private readonly Dictionary<string, MakeupColorAsset> _originalColorsBySlot;

        public MakeupColorController(MegaSkinGenieMaterial skinMaterial, IAssetLoader<MakeupColorAsset> makeupColorLoader)
        {
            _skinMaterial = skinMaterial;
            _makeupColorLoader = makeupColorLoader;

            _originalColorsBySlot = GetCurrentColors(skinMaterial.NonBakedMaterial);
        }

        protected override bool IsSlotValid(string slotId)
        {
            return Slots.Contains(slotId);
        }

        protected override UniTask<Ref<MakeupColorAsset>> LoadAssetAsync(string assetId, string slotId)
        {
            return _makeupColorLoader.LoadAsync(assetId, _skinMaterial.Lod);
        }

        protected override UniTask OnAssetEquippedAsync(MakeupColorAsset asset, string slotId)
        {
            if (!MakeupSlotPropertyIds.TryGetPropertyIds(slotId, out MakeupSlotPropertyIds propertyIds))
            {
                Debug.LogError($"[{nameof(MakeupController)}] couldn't get the makeup material property IDs for the slot {slotId}");
                return UniTask.CompletedTask;
            }

            if (asset is null || propertyIds.ColorIds is null || !_skinMaterial.NonBakedMaterial)
            {
                return UniTask.CompletedTask;
            }

            SetColor(propertyIds, 0, asset.Color1);
            SetColor(propertyIds, 1, asset.Color2);
            SetColor(propertyIds, 2, asset.Color3);

            // always apply the color changes so they are kept in the material but don't set it dirty if there is no makeup equipped
            if (HasSlotMakeupEquipped(propertyIds))
            {
                _skinMaterial.NotifyUpdate();
            }

            return UniTask.CompletedTask;
        }

        protected override UniTask OnAssetUnequippedAsync(MakeupColorAsset asset, string slotId)
        {
            if (!MakeupSlotPropertyIds.TryGetPropertyIds(slotId, out MakeupSlotPropertyIds propertyIds))
            {
                Debug.LogError($"[{nameof(MakeupController)}] couldn't get the makeup material property IDs for the slot {slotId}");
                return UniTask.CompletedTask;
            }

            if (propertyIds.ColorIds is null || !_originalColorsBySlot.TryGetValue(slotId, out MakeupColorAsset color) || !_skinMaterial.NonBakedMaterial)
            {
                return UniTask.CompletedTask;
            }

            SetColor(propertyIds, 0, color.Color1);
            SetColor(propertyIds, 1, color.Color2);
            SetColor(propertyIds, 2, color.Color3);

            // always apply the color changes so they are kept in the material but don't set it dirty if there is no makeup equipped
            if (HasSlotMakeupEquipped(propertyIds))
            {
                _skinMaterial.NotifyUpdate();
            }

            return UniTask.CompletedTask;
        }

        public override void Dispose()
        {
            base.Dispose();
            _originalColorsBySlot.Clear();
        }

        private void SetColor(MakeupSlotPropertyIds propertyIds, int index, Color color)
        {
            if (index < 0 || index >= propertyIds.ColorIds.Length)
            {
                return;
            }

            int colorId = propertyIds.ColorIds[index];
            _skinMaterial.NonBakedMaterial.SetColor(colorId, color);
        }

        // returns true if the given slot has any makeup equipped
        private bool HasSlotMakeupEquipped(MakeupSlotPropertyIds slotPropertyIds)
        {
            return _skinMaterial.NonBakedMaterial.GetTexture(slotPropertyIds.TextureId);
        }

        private static Dictionary<string, MakeupColorAsset> GetCurrentColors(Material material)
        {
            var colorsBySlot = new Dictionary<string, MakeupColorAsset>();
            var defaultColorAsset = new MakeupColorAsset("default-makeup-color-white", Color.white, Color.white, Color.white);

            foreach (string slotId in MakeupSlot.All)
            {
                if (!MakeupSlotPropertyIds.TryGetPropertyIds(slotId, out MakeupSlotPropertyIds propertyIds) || propertyIds.ColorIds is null)
                {
                    colorsBySlot[slotId] = defaultColorAsset;
                    continue;
                }

                Color color1 = propertyIds.ColorIds.Length > 0 ? material.GetColor(propertyIds.ColorIds[0]) : default;
                Color color2 = propertyIds.ColorIds.Length > 1 ? material.GetColor(propertyIds.ColorIds[1]) : default;
                Color color3 = propertyIds.ColorIds.Length > 2 ? material.GetColor(propertyIds.ColorIds[2]) : default;
                MakeupColorAsset colorAsset = new MakeupColorAsset(color1, color2, color3);

                colorsBySlot[slotId] = colorAsset;
            }

            return colorsBySlot;
        }
    }
}
