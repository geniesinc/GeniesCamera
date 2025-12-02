using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Genies.Refs;
using UnityEngine;

namespace Genies.Avatars
{
    public sealed class MaterialController : AssetSlotsController<MaterialAsset>
    {
        // dependencies
        private readonly IEditableGenie _genie;
        private readonly ISlottedAssetLoader<MaterialAsset> _materialLoader;

        // state
        private readonly Dictionary<string, MaterialSlotController> _slotControllers;

        public MaterialController(IEditableGenie genie, ISlottedAssetLoader<MaterialAsset> materialLoader, IEnumerable<MaterialSlotController> slotControllers)
        {
            _genie = genie;
            _materialLoader = materialLoader;

            // map the slot controllers
            _slotControllers = new Dictionary<string, MaterialSlotController>();
            foreach (MaterialSlotController slotController in slotControllers)
            {
                if (_slotControllers.ContainsKey(slotController.SlotId))
                {
                    Debug.LogError($"[{nameof(MaterialController)}] the following slot has more than one slot controller {slotController.SlotId}");
                }

                _slotControllers[slotController.SlotId] = slotController;
            }

            foreach (MaterialSlotController slotController in _slotControllers.Values)
            {
                _genie.AddMaterial(slotController);
            }
        }

        protected override bool IsSlotValid(string slotId)
        {
            return _slotControllers.ContainsKey(slotId);
        }

        protected override UniTask<Ref<MaterialAsset>> LoadAssetAsync(string assetId, string slotId)
        {
            return _materialLoader.LoadAsync(assetId, slotId, _genie.Lod);
        }

        protected override UniTask OnAssetEquippedAsync(MaterialAsset asset, string slotId)
        {
            if (asset != null && asset.Material && _slotControllers.TryGetValue(slotId, out MaterialSlotController slotController))
            {
                slotController.EquipMaterial(asset.Material);
            }

            return UniTask.CompletedTask;
        }

        protected override UniTask OnAssetUnequippedAsync(MaterialAsset asset, string slotId)
        {
            if (_slotControllers.TryGetValue(slotId, out MaterialSlotController slotController))
            {
                slotController.ClearSlot();
            }

            return UniTask.CompletedTask;
        }

        public override void Dispose()
        {
            base.Dispose();

            foreach (MaterialSlotController slotController in _slotControllers.Values)
            {
                _genie.RemoveMaterial(slotController);
                slotController.Dispose();
            }

            _slotControllers.Clear();
            _slotControllers.TrimExcess();
        }
    }
}
