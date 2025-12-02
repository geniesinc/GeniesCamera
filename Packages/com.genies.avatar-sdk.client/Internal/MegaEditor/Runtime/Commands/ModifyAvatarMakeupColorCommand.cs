using Cysharp.Threading.Tasks;
using Genies.Avatars;


namespace Genies.Looks.Customization.Commands
{
    /// <summary>
    /// Command for equipping a makeup color asset with <see cref="_targetId"/> to <see cref="_slotId"/> (slot)
    /// </summary>
    public class ModifyAvatarMakeupColorCommand : UnifiedGenieModificationCommand
    {
        private readonly string _targetId;
        private readonly string _slotId;
        private readonly string _previousId;

        public ModifyAvatarMakeupColorCommand(string targetId, string slotId, UnifiedGenieController controller) : base(controller)
        {
            _targetId = targetId;
            _slotId = slotId;

            controller.MakeupColors.TryGetEquippedAssetId(slotId, out _previousId);
        }

        protected override async UniTask ExecuteModificationAsync(UnifiedGenieController controller)
        {
            await controller.MakeupColors.LoadAndEquipAssetAsync(_targetId, _slotId);
        }

        protected override async UniTask UndoModificationAsync(UnifiedGenieController controller)
        {
            if (string.IsNullOrEmpty(_previousId))
            {
                await controller.MakeupColors.ClearSlotAsync(_slotId);
                return;
            }

            await controller.MakeupColors.LoadAndEquipAssetAsync(_previousId, _slotId);
        }
    }
}
