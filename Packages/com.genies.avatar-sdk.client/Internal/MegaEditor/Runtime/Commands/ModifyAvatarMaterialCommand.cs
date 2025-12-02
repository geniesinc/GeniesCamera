using Cysharp.Threading.Tasks;
using Genies.Avatars;


namespace Genies.Looks.Customization.Commands
{
    /// <summary>
    /// Command for equipping a material color asset with <see cref="_targetId"/> to <see cref="_slotId"/> (slot)
    /// </summary>
    public class ModifyAvatarMaterialCommand : UnifiedGenieModificationCommand
    {
        private readonly string _targetId;
        private readonly string _slotId;
        private readonly string _previousId;

        public ModifyAvatarMaterialCommand(string targetId, string slotId, UnifiedGenieController controller) : base(controller)
        {
            _targetId = targetId;
            _slotId = slotId;
            controller.Materials.TryGetEquippedAssetId(slotId, out _previousId);
        }

        protected override async UniTask ExecuteModificationAsync(UnifiedGenieController controller)
        {
            await controller.Materials.LoadAndEquipAssetAsync(_targetId, _slotId);
        }

        protected override async UniTask UndoModificationAsync(UnifiedGenieController controller)
        {
            if (string.IsNullOrEmpty(_previousId))
            {
                await controller.Materials.ClearSlotAsync(_slotId);
                return;
            }

            await controller.Materials.LoadAndEquipAssetAsync(_previousId, _slotId);
        }
    }
}
