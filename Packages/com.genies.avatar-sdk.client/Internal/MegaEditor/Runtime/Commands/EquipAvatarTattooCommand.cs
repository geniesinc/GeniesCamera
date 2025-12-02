using Cysharp.Threading.Tasks;
using Genies.Avatars;


namespace Genies.Looks.Customization.Commands
{
    /// <summary>
    /// Command for equipping a tattoo asset with <see cref="_targetId"/> to <see cref="_areaId"/> (slot)
    /// </summary>
    public class EquipAvatarTattooCommand : UnifiedGenieModificationCommand
    {
        private readonly string _targetId;
        private readonly string _areaId;
        private readonly string _previousId;

        public EquipAvatarTattooCommand(string targetId, string areaId, UnifiedGenieController controller) : base(controller)
        {
            _targetId = targetId;
            _areaId = areaId;

            controller.Tattoos.TryGetEquippedAssetId(areaId, out _previousId);
        }

        protected override async UniTask ExecuteModificationAsync(UnifiedGenieController controller)
        {
            await controller.Tattoos.LoadAndEquipAssetAsync(_targetId, _areaId);
        }

        protected override async UniTask UndoModificationAsync(UnifiedGenieController controller)
        {
            if (string.IsNullOrEmpty(_previousId))
            {
                await controller.Tattoos.ClearSlotAsync(_areaId);
                return;
            }

            await controller.Tattoos.LoadAndEquipAssetAsync(_previousId, _areaId);
        }
    }
}
