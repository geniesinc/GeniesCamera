using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Genies.Avatars;


namespace Genies.Looks.Customization.Commands
{
    /// <summary>
    /// Unequips the current equipped outfit asset
    /// </summary>
    public class UnequipAvatarOutfitCommand : UnifiedGenieModificationCommand
    {
        private readonly string _currentEquipped;

        //Keep track of the previously equipped outfit for undo/redo
        private readonly List<string> _previousOutfit;

        public UnequipAvatarOutfitCommand(string currentEquipped, UnifiedGenieController controller) : base(controller)
        {
            _currentEquipped = currentEquipped;
            _previousOutfit = new List<string>(controller.Outfit.EquippedAssetIds);
        }

        protected override async UniTask ExecuteModificationAsync(UnifiedGenieController controller)
        {
            await controller.Outfit.UnequipAssetAsync(_currentEquipped);
        }

        protected override async UniTask UndoModificationAsync(UnifiedGenieController controller)
        {
            await controller.Outfit.LoadAndSetEquippedAssetsAsync(_previousOutfit);
        }
    }
}
