using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Genies.Avatars;


namespace Genies.Looks.Customization.Commands
{
    /// <summary>
    /// Command for equipping a face preset asset with <see cref="_targetId"/>
    /// </summary>
    public class EquipAvatarFacePresetCommand : UnifiedGenieModificationCommand
    {
        private readonly string _targetId;
        private readonly List<string> _previousEquippedShapes;

        public EquipAvatarFacePresetCommand(string targetId, UnifiedGenieController controller) : base(controller)
        {
            _targetId = targetId;
            _previousEquippedShapes = new List<string>(controller.BlendShapes.EquippedAssetIds);
        }

        protected override async UniTask ExecuteModificationAsync(UnifiedGenieController controller)
        {
            await controller.BlendShapes.LoadAndEquipPresetAsync(_targetId);
        }

        protected override async UniTask UndoModificationAsync(UnifiedGenieController controller)
        {
            await controller.BlendShapes.LoadAndSetEquippedAssetsAsync(_previousEquippedShapes);
        }
    }
}
