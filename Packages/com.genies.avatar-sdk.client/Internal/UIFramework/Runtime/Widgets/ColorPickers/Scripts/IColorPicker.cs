using System;
using UnityEngine;

namespace Genies.UI.Widgets
{
    /// <summary>
    /// Represents a color picker widget. It's an abstraction that allows us to create different types of color pickers
    /// like RGB or HSV, so the code that uses them only cares about when a color is picked and manually setting it.
    /// </summary>
    public interface IColorPicker
    {
        /// <summary>
        /// Current color. If set, it will fire the <see cref="ColorUpdated"/> event.
        /// If you need to set the color without notification use <see cref="SetColorWithoutNotify"/>.
        /// </summary>
        Color Color { get; set; }

        /// <summary>
        /// Fired whenever the color is updated.
        /// </summary>
        event Action<Color> ColorUpdated;

        /// <summary>
        /// Same as setting the color through the <see cref="Color"/> attribute but it will not fire thie <see cref="ColorUpdated"/> event.
        /// </summary>
        void SetColorWithoutNotify(Color color);
    }
}
