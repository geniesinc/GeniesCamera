using Cysharp.Threading.Tasks;
using Genies.Avatars;


namespace Genies.Looks.Customization.Commands
{
    /// <summary>
    /// Command for equipping a blendshape asset with <see cref="_targetId"/>
    /// </summary>
    public class EquipAvatarBlendShapeCommand : UnifiedGenieModificationCommand
    {
        private readonly string _targetId;
        private readonly string _previousId;

        public EquipAvatarBlendShapeCommand(string targetId, string slotId, UnifiedGenieController unifiedGenieController) : base(unifiedGenieController)
        {
            _targetId = targetId;
            _previousId = unifiedGenieController.BlendShapes.GetEquippedBlendShapeForSlot(slotId);
        }

        protected override async UniTask ExecuteModificationAsync(UnifiedGenieController controller)
        {
            await controller.BlendShapes.LoadAndEquipAssetAsync(_targetId);
        }

        protected override async UniTask UndoModificationAsync(UnifiedGenieController controller)
        {
            if (string.IsNullOrEmpty(_previousId))
            {
                await controller.BlendShapes.UnequipAssetAsync(_targetId);
                return;
            }

            await controller.BlendShapes.LoadAndEquipAssetAsync(_previousId);
        }
    }
}
