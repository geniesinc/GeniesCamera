using Cysharp.Threading.Tasks;
using Genies.Avatars;


namespace Genies.Looks.Customization.Commands
{
    public class UnequipAvatarMakeupCommand : UnifiedGenieModificationCommand
    {
        private readonly string _slotId;
        private readonly string _previousId;

        public UnequipAvatarMakeupCommand(string slotId, UnifiedGenieController controller) : base(controller)
        {
            _slotId = slotId;

            controller.Makeup.TryGetEquippedAssetId(slotId, out _previousId);
        }

        protected override async UniTask ExecuteModificationAsync(UnifiedGenieController controller)
        {
            await controller.Makeup.ClearSlotAsync(_slotId);
        }

        protected override async UniTask UndoModificationAsync(UnifiedGenieController controller)
        {
            if (string.IsNullOrEmpty(_previousId))
            {
                await controller.Makeup.ClearSlotAsync(_slotId);
                return;
            }

            await controller.Makeup.LoadAndEquipAssetAsync(_previousId, _slotId);
        }
    }
}
