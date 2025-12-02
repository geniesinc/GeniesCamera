using System.Collections.Generic;
using UnityEngine;

namespace Genies.UIFramework
{
    /// <summary>
    /// Represents a style associated with a Popup in PopupSystem. Contains background colour, button colours and button text colours
    /// </summary>
    public class PopupStyle
    {
        public static readonly Color DefaultBackgroundColor = Color.white;

        public Color BackgroundColor;
        public List<PopupButtonStyle> ButtonStyles;

        public PopupStyle(
            Color backgroundColor,
            List<PopupButtonStyle> button)
        {
            BackgroundColor = backgroundColor;
            ButtonStyles = button;
        }

        public static PopupStyle Default => new PopupStyle
        (
            DefaultBackgroundColor,
            new List<PopupButtonStyle> { PopupButtonStyle.Default, PopupButtonStyle.Default}
        );
    }
}
