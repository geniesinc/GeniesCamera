using Cysharp.Threading.Tasks;
using Genies.Avatars;


namespace Genies.Looks.Customization.Commands
{
    /// <summary>
    /// Command for equipping a skin color asset with <see cref="_targetId"/>
    /// </summary>
    public class ModifyAvatarSkinColorCommand : UnifiedGenieModificationCommand
    {
        private readonly string _targetId;
        private readonly ColorAsset _previousColor;

        public ModifyAvatarSkinColorCommand(string targetId, UnifiedGenieController controller) : base(controller)
        {
            _targetId = targetId;
            _previousColor = controller.Skin.CurrentColor;
        }

        protected override async UniTask ExecuteModificationAsync(UnifiedGenieController controller)
        {
            await controller.Skin.LoadAndSetSkinColorAsync(_targetId);
        }

        protected override UniTask UndoModificationAsync(UnifiedGenieController controller)
        {
            controller.Skin.SetSkinColor(_previousColor);
            return UniTask.CompletedTask;
        }
    }
}
