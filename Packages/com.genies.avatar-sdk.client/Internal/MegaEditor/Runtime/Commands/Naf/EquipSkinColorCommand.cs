using System.Threading;
using Cysharp.Threading.Tasks;
using Genies.Customization.Framework;
using Genies.Naf;
using UnityEngine;

namespace Genies.Looks.Customization.Commands
{
    public class EquipSkinColorCommand : ICommand
    {
        private readonly NativeUnifiedGenieController _controller;
        private readonly Color _color;
        private readonly Color _previousColor;

        public EquipSkinColorCommand(Color color, NativeUnifiedGenieController controller)
        {
            _color = color;
            _controller = controller;
            var previousColor =  controller.GetColor(GenieColor.Skin);
            if (previousColor != null)
            {
                _previousColor = (Color) previousColor;
            }
        }

        public async UniTask ExecuteAsync(CancellationToken cancellationToken = default)
        {
            await _controller.SetColorAsync(GenieColor.Skin, _color);
        }

        public async UniTask UndoAsync(CancellationToken cancellationToken = default)
        {
            await _controller.SetColorAsync(GenieColor.Skin, _previousColor);
        }
    }
}
