using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Genies.Avatars;

using Genies.Models;
using UnityEngine;

namespace Genies.Looks.Customization.Commands
{
    /// <summary>
    /// Equips a color preset on a flair using its id <see cref="_assetId"/>
    /// </summary>
    public class EquipFlairColorAssetCommand : UnifiedGenieModificationCommand
    {
        private readonly string _assetId;
        private readonly string _flairAssetType;
        //Keep track of the previously equipped outfit for undo/redo
        private readonly Dictionary<FlairAssetType, IReadOnlyCollection<string>> _previousFlairAsset;

        private readonly Color[] _colors;
        public EquipFlairColorAssetCommand(string assetId, Color[] colors, string flairAssetType,  UnifiedGenieController controller) : base(controller)
        {
            _assetId = assetId;
            _flairAssetType = flairAssetType;
            _colors = colors;
        }

        protected override async UniTask ExecuteModificationAsync(UnifiedGenieController controller)
        {
            await controller.Flair.EquipColorPreset(_assetId, _colors, _flairAssetType.ToString());
        }

        protected override UniTask UndoModificationAsync(UnifiedGenieController controller)
        {
            throw new NotImplementedException();
        }
    }
}
