using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Genies.Avatars;

using Genies.Models;

namespace Genies.Looks.Customization.Commands
{
    /// <summary>
    /// Equips the a flair asset using its id <see cref="_assetId"/>
    /// </summary>
    public class EquipFlairAvatarAssetCommand : UnifiedGenieModificationCommand
    {
        private readonly string _assetId;
        private readonly string _flairAssetType;
        //Keep track of the previously equipped outfit for undo/redo
        private readonly Dictionary<FlairAssetType, IReadOnlyCollection<string>> _previousFlairAsset;

        public EquipFlairAvatarAssetCommand(string assetId, string flairAssetType,  UnifiedGenieController controller) : base(controller)
        {
            _assetId = assetId;
            _flairAssetType = flairAssetType;
        }

        protected override async UniTask ExecuteModificationAsync(UnifiedGenieController controller)
        {
            await controller.Flair.LoadAndEquipAssetAsync(_assetId, _flairAssetType.ToString());
        }

        protected override UniTask UndoModificationAsync(UnifiedGenieController controller)
        {
            throw new NotImplementedException();
        }
    }
}
