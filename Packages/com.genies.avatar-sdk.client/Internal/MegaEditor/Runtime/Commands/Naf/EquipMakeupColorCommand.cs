using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Genies.Avatars.Behaviors;
using Genies.Customization.Framework;
using Genies.MakeupPresets;
using Genies.Naf;
using UnityEngine;

namespace Genies.Looks.Customization.Commands
{
    /// <summary>
    /// Applies a set of makeup colors to the avatar (e.g., LipstickAll/1/2/3).
    /// Stores previous values for a proper Undo.
    /// </summary>
    public class EquipMakeupColorCommand : ICommand
    {
        private readonly NativeUnifiedGenieController _controller;
        private readonly GenieColorEntry[] _newColors;

        // Captured on first execute
        private GenieColorEntry[] _previousColors;

        public EquipMakeupColorCommand(MakeupPresetCategory subcategory, Color[] colors, NativeUnifiedGenieController controller)
        {
            var newColors = MapMakeupPresetToEntries(subcategory, colors);
            _newColors = newColors;
            _controller = controller;
        }

        public async UniTask ExecuteAsync(CancellationToken cancellationToken = default)
        {
            if (_newColors == null || _newColors.Length == 0)
            {
                return;
            }

            // Capture previous values once, just before first apply
            if (_previousColors == null)
            {
                _previousColors = new GenieColorEntry[_newColors.Length];
                for (int i = 0; i < _newColors.Length; i++)
                {
                    var id = _newColors[i].ColorId;
                    var prev = _controller.GetColor(id); // Color?
                    _previousColors[i] = new GenieColorEntry(id, prev);
                }
            }

            // Apply new colors
            for (int i = 0; i < _newColors.Length; i++)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return;
                }

                var id = _newColors[i].ColorId;
                if (_newColors[i].Value.HasValue)
                {
                    await _controller.SetColorAsync(id, _newColors[i].Value.Value);
                }
                // If Value is null, we skip; you can add a "clear" call here if your controller supports it.
            }
        }

        public async UniTask UndoAsync(CancellationToken cancellationToken = default)
        {
            if (_previousColors == null || _previousColors.Length == 0)
            {
                return;
            }

            for (int i = 0; i < _previousColors.Length; i++)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return;
                }

                var id = _previousColors[i].ColorId;
                var value = _previousColors[i].Value;

                if (value.HasValue)
                {
                    await _controller.SetColorAsync(id, value.Value);
                }
                else
                {
                    // No previous value — optionally clear. If you have a clear API, call it here.
                    // e.g. await _controller.ClearColorAsync(id);
                }
            }
        }

        private static GenieColorEntry[] MapMakeupPresetToEntries(MakeupPresetCategory subcategory, Color[] colors)
        {
            // Ensure at least 4 values: [0]=All, [1]=1, [2]=2, [3]=3
            var src = colors ?? Array.Empty<Color>();
            var baseCol = src.Length > 0 ? src[0] : Color.black;
            var c0 = src.Length > 0 ? src[0] : baseCol;
            var c1 = src.Length > 1 ? src[1] : baseCol;
            var c2 = src.Length > 2 ? src[2] : baseCol;
            var c3 = src.Length > 3 ? src[3] : baseCol;

            switch (subcategory)
            {
                case MakeupPresetCategory.Lipstick:
                    return new[]
                    {
                        new GenieColorEntry(GenieColor.LipstickAll, c0),
                        new GenieColorEntry(GenieColor.Lipstick1, c1),
                        new GenieColorEntry(GenieColor.Lipstick2, c2),
                        new GenieColorEntry(GenieColor.Lipstick3, c3),
                    };

                case MakeupPresetCategory.Blush:
                    return new[]
                    {
                        new GenieColorEntry(GenieColor.BlushAll, c0), new GenieColorEntry(GenieColor.Blush1, c1),
                        new GenieColorEntry(GenieColor.Blush2, c2), new GenieColorEntry(GenieColor.Blush3, c3),
                    };

                case MakeupPresetCategory.Eyeshadow:
                    return new[]
                    {
                        new GenieColorEntry(GenieColor.EyeshadowAll, c0),
                        new GenieColorEntry(GenieColor.Eyeshadow1, c1),
                        new GenieColorEntry(GenieColor.Eyeshadow2, c2),
                        new GenieColorEntry(GenieColor.Eyeshadow3, c3),
                    };

                case MakeupPresetCategory.Freckles:
                    // Freckles typically use a single channel; map others to same color for safety.
                    return new[] { new GenieColorEntry(GenieColor.Freckles, c0) };

                case MakeupPresetCategory.FaceGems:
                    return new[]
                    {
                        new GenieColorEntry(GenieColor.FaceGemsAll, c0),
                        new GenieColorEntry(GenieColor.FaceGems1, c1),
                        new GenieColorEntry(GenieColor.FaceGems2, c2),
                        new GenieColorEntry(GenieColor.FaceGems3, c3),
                    };

                // Stickers generally aren’t color-only in most pipelines; no-op to avoid side effects.
                case MakeupPresetCategory.Stickers:
                default:
                    return Array.Empty<GenieColorEntry>();
            }
        }
    }
}
