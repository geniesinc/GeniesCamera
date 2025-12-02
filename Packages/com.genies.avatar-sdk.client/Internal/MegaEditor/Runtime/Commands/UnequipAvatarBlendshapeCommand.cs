using Cysharp.Threading.Tasks;
using Genies.Avatars;


namespace Genies.Looks.Customization.Commands
{
    /// <summary>
    /// Command for unequipping current blendshape
    /// </summary>
    public class UnequipAvatarBlendshapeCommand : UnifiedGenieModificationCommand
    {
        private readonly string _previousId;

        public UnequipAvatarBlendshapeCommand(string slotId, UnifiedGenieController unifiedGenieController) : base(unifiedGenieController)
        {
            _previousId = unifiedGenieController.BlendShapes.GetEquippedBlendShapeForSlot(slotId);
        }

        protected override async UniTask ExecuteModificationAsync(UnifiedGenieController controller)
        {
            await controller.BlendShapes.UnequipAssetAsync(_previousId);
        }

        protected override async UniTask UndoModificationAsync(UnifiedGenieController controller)
        {
            if (string.IsNullOrEmpty(_previousId))
            {
                return;
            }

            await controller.BlendShapes.LoadAndEquipAssetAsync(_previousId);
        }
    }
}
