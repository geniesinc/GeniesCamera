using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Genies.Avatars;


namespace Genies.Looks.Customization.Commands
{
    /// <summary>
    /// Unequip current avatar FacePreset
    /// </summary>
    public class UnequipAvatarFacePresetCommand: UnifiedGenieModificationCommand
    {
        private readonly string _targetId;
        private readonly List<string> _previousEquippedShapes;

        public UnequipAvatarFacePresetCommand(UnifiedGenieController controller) : base(controller)
        {
            _previousEquippedShapes = new List<string>(controller.BlendShapes.EquippedAssetIds);
        }

        protected override async UniTask ExecuteModificationAsync(UnifiedGenieController controller)
        {
            await controller.BlendShapes.UnequipAllAssetsAsync();
        }

        protected override async UniTask UndoModificationAsync(UnifiedGenieController controller)
        {
            await controller.BlendShapes.LoadAndSetEquippedAssetsAsync(_previousEquippedShapes);
        }
    }
}
