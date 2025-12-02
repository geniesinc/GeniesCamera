using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Genies.UIFramework
{
    /// <summary>
    /// Configuration class for defining popup appearance, content, and behavior.
    /// Contains all necessary data to create and display a popup with specified layout, text, images, and buttons.
    /// </summary>
    [Serializable]
    public class PopupConfig
    {
        /// <summary>
        /// The layout type of the popup (SingleButton, DualButton, InputField, Custom).
        /// </summary>
        public PopupLayout PopupLayout;

        /// <summary>
        /// The header text displayed at the top of the popup.
        /// </summary>
        public string Header;

        /// <summary>
        /// The main content text displayed in the body of the popup.
        /// </summary>
        public string Content;

        /// <summary>
        /// The placeholder text displayed in input fields when using InputField layout.
        /// </summary>
        public string PlaceholderInputFieldText;

        /// <summary>
        /// The sprite image displayed at the top of the popup (optional).
        /// </summary>
        public Sprite TopImage;

        /// <summary>
        /// List of labels for the popup buttons in order from left to right.
        /// </summary>
        public List<string> ButtonLabels;

        /// <summary>
        /// Whether the popup should include a close button (X button) in the header.
        /// </summary>
        public bool HasCloseButton;

        /// <summary>
        /// The visual style configuration for the popup appearance.
        /// </summary>
        public PopupStyle Style;

        /// <summary>
        /// The fallback font asset to use if the default font is not available.
        /// </summary>
        public TMP_FontAsset FallBackFont;

        /// <summary>
        /// Initializes a new instance of the PopupConfig class with the specified configuration parameters.
        /// </summary>
        /// <param name="popupLayout">The layout type for the popup.</param>
        /// <param name="header">The header text to display.</param>
        /// <param name="content">The main content text to display.</param>
        /// <param name="placeholderInputFieldText">Placeholder text for input fields.</param>
        /// <param name="topImage">Optional image to display at the top.</param>
        /// <param name="buttonLabels">Labels for the popup buttons.</param>
        /// <param name="hasCloseButton">Whether to include a close button.</param>
        /// <param name="style">Optional visual style configuration.</param>
        /// <param name="fallBackFont">Optional fallback font asset.</param>
        public PopupConfig(
            PopupLayout popupLayout,
            string header,
            string content,
            string placeholderInputFieldText,
            Sprite topImage,
            List<string> buttonLabels,
            bool hasCloseButton,
            PopupStyle style = null,
            TMP_FontAsset fallBackFont = null
        )
        {
            PopupLayout = popupLayout;
            Header = header;
            Content = content;
            PlaceholderInputFieldText = placeholderInputFieldText;
            TopImage = topImage;
            ButtonLabels = buttonLabels;
            HasCloseButton = hasCloseButton;
            Style = style;
            FallBackFont = fallBackFont;
        }

        /// <summary>
        /// Sets the header text for the popup configuration.
        /// </summary>
        /// <param name="text">The header text to set.</param>
        /// <returns>This PopupConfig instance for method chaining.</returns>
        public PopupConfig SetHeaderText(string text)
        {
            Header = text;
            return this;
        }

        /// <summary>
        /// Sets the content text for the popup configuration.
        /// </summary>
        /// <param name="text">The content text to set.</param>
        /// <returns>This PopupConfig instance for method chaining.</returns>
        public PopupConfig SetContentText(string text)
        {
            Content = text;
            return this;
        }

    }
}

