using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Genies.Refs;
using UnityEngine;
using Genies.Assets.Services;  

namespace Genies.Avatars
{
    /// <summary>
    /// A different implementation for controlling the tattoos on a <see cref="MegaSkinGenieMaterial"/> instance
    /// that uses transform presets instead of allowing the user to control the tattoo transformation.
    /// </summary>
    public sealed class TattooPresetController : AssetSlotsController<Texture2DAsset>
    {
        // dependencies
        private readonly MegaSkinGenieMaterial _skinMaterial;
        private readonly IAssetLoader<Texture2DAsset> _tattooLoader;
        private readonly TattooController _tattooController;
        
        // state
        private readonly Dictionary<string, int> _slotIndicesBySlotId;

        public TattooPresetController(MegaSkinGenieMaterial skinMaterial, IAssetLoader<Texture2DAsset> tattooLoader, IEnumerable<TattooTransformPreset> transformPresets)
        {
            _skinMaterial = skinMaterial;
            _tattooLoader = tattooLoader;
            _tattooController = new TattooController(skinMaterial, tattooLoader);
            
            _slotIndicesBySlotId = new Dictionary<string, int>();
            
            // set the preset transformations to each slot controller and map their slot index to the preset ID
            int index = 0;
            foreach (TattooTransformPreset transformPreset in transformPresets)
            {
                if (index >= _tattooController.SlotControllers.Count)
                {
                    Debug.LogError($"[{nameof(TattooPresetController)}] there are more tattoo transform presets than available tattoo slots on the material");
                    break;
                }
                
                // get the next available slot controller and set its transform from the preset
                TattooSlotController slotController = _tattooController.SlotControllers[index];
                slotController.PositionX = transformPreset.PositionX;
                slotController.PositionY = transformPreset.PositionY;
                slotController.Rotation = transformPreset.Rotation;
                slotController.Scale = transformPreset.Scale;
                
                // map the preset ID to the controller's slot index
                _slotIndicesBySlotId[transformPreset.Id] = index;
                ++index;
            }
        }

        protected override bool IsSlotValid(string slotId)
        {
            return _slotIndicesBySlotId.ContainsKey(slotId);
        }

        protected override UniTask<Ref<Texture2DAsset>> LoadAssetAsync(string assetId, string slotId)
        {
            return _tattooLoader.LoadAsync(assetId, _skinMaterial.Lod);
        }

        protected override UniTask OnAssetEquippedAsync(Texture2DAsset asset, string slotId)
        {
            if (!TryGetSlotController(slotId, out TattooSlotController slotController))
            {
                return UniTask.CompletedTask;
            }

            /**
             * The TattooSlotController can also be used separately, so it requires an asset reference to be passed
             * if you are equipping an already loaded asset. Our base class currently handles asset references for us
             * and hence it passes us the asset directly so we don't need to care. But for this specific case we still
             * need to pass a reference to the slot controller so we have to create a dummy reference that will not
             * destroy the texture on disposal. This means that the real asset reference release will be handled by our
             * base class and not by the slot controller
             */
            Ref<Texture2DAsset> dummyReference = CreateRef.FromAny(asset);
            slotController.EquipTattoo(dummyReference);
            
            return UniTask.CompletedTask;
        }

        protected override UniTask OnAssetUnequippedAsync(Texture2DAsset asset, string slotId)
        {
            if (!TryGetSlotController(slotId, out TattooSlotController slotController))
            {
                return UniTask.CompletedTask;
            }

            slotController.ClearTattoo();
            return UniTask.CompletedTask;
        }

        public override void Dispose()
        {
            base.Dispose();
            _tattooController.Dispose();
            _slotIndicesBySlotId.Clear();
        }
        
        private bool TryGetSlotController(string slotId, out TattooSlotController slotController)
        {
            slotController = null;
            
            if (!_slotIndicesBySlotId.TryGetValue(slotId, out int slotIndex))
            {
                return false;
            }

            if (slotIndex < 0 || slotIndex >= _tattooController.SlotControllers.Count)
            {
                return false;
            }

            slotController = _tattooController.SlotControllers[slotIndex];
            return slotController != null;
        }
    }
}