using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Genies.Avatars;


namespace Genies.Looks.Customization.Commands
{
    /// <summary>
    /// Equips the avatar outfit asset using its id <see cref="_assetId"/>
    /// </summary>
    public class EquipAvatarOutfitAssetCommand : UnifiedGenieModificationCommand
    {
        private readonly string _assetId;

        //Keep track of the previously equipped outfit for undo/redo
        private readonly List<string> _previousOutfit;

        public EquipAvatarOutfitAssetCommand(string assetId, UnifiedGenieController controller) : base(controller)
        {
            _assetId = assetId;
            _previousOutfit = new List<string>(controller.Outfit.EquippedAssetIds);
        }

        protected override async UniTask ExecuteModificationAsync(UnifiedGenieController controller)
        {
            await controller.Outfit.LoadAndEquipAssetAsync(_assetId);
        }

        protected override async UniTask UndoModificationAsync(UnifiedGenieController controller)
        {
            await controller.Outfit.LoadAndSetEquippedAssetsAsync(_previousOutfit);
        }
    }
}
