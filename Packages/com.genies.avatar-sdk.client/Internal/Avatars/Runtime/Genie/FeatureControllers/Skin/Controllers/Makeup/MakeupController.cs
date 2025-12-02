using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Genies.Refs;
using UnityEngine;
using Genies.Assets.Services;

namespace Genies.Avatars
{
    /// <summary>
    /// Used to control the makeup slots from a <see cref="MegaSkinGenieMaterial"/> instance.
    /// Any changes made will set the skin material dirty.
    /// </summary>
    public sealed class MakeupController : AssetSlotsController<Texture2DAsset>
    {
        private static readonly HashSet<string> Slots = new(MakeupSlot.All);

        // dependencies
        private readonly MegaSkinGenieMaterial _skinMaterial;
        private readonly IAssetLoader<Texture2DAsset> _makeupLoader;

        public MakeupController(MegaSkinGenieMaterial skinMaterial, IAssetLoader<Texture2DAsset> makeupLoader)
        {
            _skinMaterial = skinMaterial;
            _makeupLoader = makeupLoader;
        }

        protected override bool IsSlotValid(string slotId)
        {
            return Slots.Contains(slotId);
        }

        protected override UniTask<Ref<Texture2DAsset>> LoadAssetAsync(string assetId, string slotId)
        {
            return _makeupLoader.LoadAsync(assetId, _skinMaterial.Lod);
        }

        protected override UniTask OnAssetEquippedAsync(Texture2DAsset asset, string slotId)
        {
            if (!MakeupSlotPropertyIds.TryGetPropertyIds(slotId, out MakeupSlotPropertyIds propertyIds))
            {
                Debug.LogError($"[{nameof(MakeupController)}] couldn't get the makeup material property IDs for the slot {slotId}");
                return UniTask.CompletedTask;
            }

            if (!_skinMaterial.NonBakedMaterial)
            {
                return UniTask.CompletedTask;
            }

            _skinMaterial.NonBakedMaterial.SetTexture(propertyIds.TextureId, asset.Texture);
            _skinMaterial.NotifyUpdate();
            return UniTask.CompletedTask;
        }

        protected override UniTask OnAssetUnequippedAsync(Texture2DAsset asset, string slotId)
        {
            if (!MakeupSlotPropertyIds.TryGetPropertyIds(slotId, out MakeupSlotPropertyIds propertyIds))
            {
                Debug.LogError($"[{nameof(MakeupController)}] couldn't get the makeup material property IDs for the slot {slotId}");
                return UniTask.CompletedTask;
            }

            if (!_skinMaterial.NonBakedMaterial)
            {
                return UniTask.CompletedTask;
            }

            _skinMaterial.NonBakedMaterial.SetTexture(propertyIds.TextureId, null);
            _skinMaterial.NotifyUpdate();
            return UniTask.CompletedTask;
        }
    }
}
