using System.Threading;
using Cysharp.Threading.Tasks;
using Genies.Customization.Framework;
using Genies.Naf;

namespace Genies.Looks.Customization.Commands
{
    public class SetNativeAvatarColorsCommand : ICommand
    {
        private readonly NativeUnifiedGenieController _controller;
        private readonly GenieColorEntry[]            _colors;
        private readonly GenieColorEntry[]            _previousColors;

        public SetNativeAvatarColorsCommand(GenieColorEntry[] colors, NativeUnifiedGenieController controller)
        {
            _controller = controller;
            _colors = colors;
            _previousColors = new GenieColorEntry[colors.Length];

            for (int i = 0; i < colors.Length; i++)
            {
                string colorId = colors[i].ColorId;
                _previousColors[i] = new GenieColorEntry
                {
                    ColorId = colorId,
                    Value   = controller.GetColor(colorId),
                };
            }
        }

        public UniTask ExecuteAsync(CancellationToken cancellationToken = default)
        {
            return _controller.SetColorsAsync(_colors);
        }

        public UniTask UndoAsync(CancellationToken cancellationToken = default)
        {
            return _controller.SetColorsAsync(_previousColors);
        }
    }
}
