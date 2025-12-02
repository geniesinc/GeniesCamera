using UnityEngine;

namespace Genies.UIFramework
{
    /// <summary>
    /// Defines the visual styling for popup buttons including background and text colors.
    /// Provides predefined styles for common button types like default, secondary, and negative actions.
    /// </summary>
    public class PopupButtonStyle
    {
        /// <summary>
        /// The text color for the button.
        /// </summary>
        public Color TextColor;
        
        /// <summary>
        /// The background color for the button.
        /// </summary>
        public Color BackgroundColor;

        /// <summary>
        /// The default white text color for buttons.
        /// </summary>
        public static readonly Color DefaultButtonTextColor = Color.white;
        
        /// <summary>
        /// The default blue background color for primary buttons.
        /// </summary>
        public static readonly Color DefaultButtonColor = new Color32(84, 90, 234, 255);

        /// <summary>
        /// Black text color used for secondary buttons.
        /// </summary>
        public static readonly Color BlackTextColor = Color.black;

        /// <summary>
        /// Red background color used for negative/destructive action buttons.
        /// </summary>
        public static readonly Color NegativeButtonColor = new Color32(255, 48, 48, 255);
        
        /// <summary>
        /// Light gray background color used for secondary buttons.
        /// </summary>
        public static readonly Color SecondaryButtonColor = new Color32(225, 225, 225, 255);

        /// <summary>
        /// Initializes a new instance of the PopupButtonStyle class with the specified colors.
        /// </summary>
        /// <param name="backgroundColor">The background color for the button.</param>
        /// <param name="textColor">The text color for the button.</param>
        public PopupButtonStyle(Color backgroundColor, Color textColor)
        {
            BackgroundColor = backgroundColor;
            TextColor = textColor;
        }

        /// <summary>
        /// Gets the default button style with blue background and white text.
        /// </summary>
        public static PopupButtonStyle Default => new PopupButtonStyle
        (
            DefaultButtonColor,
            DefaultButtonTextColor
        );

        /// <summary>
        /// Gets the secondary button style with light gray background and black text.
        /// </summary>
        public static PopupButtonStyle Secondary => new PopupButtonStyle
        (
            SecondaryButtonColor,
            BlackTextColor
        );

        /// <summary>
        /// Gets the negative action button style with red background and white text.
        /// </summary>
        public static PopupButtonStyle Negative => new PopupButtonStyle
        (
            NegativeButtonColor,
            DefaultButtonTextColor
        );
    }
}
