using Genies.ServiceManagement;
using Genies.UIFramework.Widgets;
using Genies.UI.Widgets;
using UnityEngine;
using TMPro;
using UnityEngine.Events;

namespace Genies.MegaEditor
{
    public class CustomizeColorView : MonoBehaviour
    {
        [Tooltip("Title of this screen")] [SerializeField]
        private TextMeshProUGUI title;

        [Tooltip("The list of color regions on top of the spectrum")] [SerializeField]
        private ColorRegionsView colorRegionsView;

        public ColorRegionsView ColorRegionsView => colorRegionsView;

        [Tooltip("The spectrum picker")] [SerializeField]
        private HsvColorSpectrumPicker hsvColorSpectrumPicker;

        private PictureInPictureController _PictureInPictureController => this.GetService<PictureInPictureController>();

        /// <summary>
        /// Event when the hsvColorSpectrumPicker's color is updated.
        /// </summary>
        public UnityEvent<Color> OnColorSelected => hsvColorSpectrumPicker.colorSelected;

        /// <summary>
        /// Initializes the color regions with a color array and sets the picker to match the first color.
        /// Also registers the events to update colors when region selection and spectrum color update.
        /// Sets the game object to be active.
        /// </summary>
        /// <param name="colors">the color array to initialize the color regions</param>
        public void Initialize(params Color[] colors)
        {
            colorRegionsView ??= GetComponentInChildren<ColorRegionsView>();
            hsvColorSpectrumPicker ??= GetComponentInChildren<HsvColorSpectrumPicker>();

            colorRegionsView.OnRegionSelectionUpdated += OnRegionSelectedUpdated;
            hsvColorSpectrumPicker.ColorUpdated += OnColorPickerUpdated;
            _PictureInPictureController.OnSizeModeChanged += OnPiPSizeModeChanged;

            colorRegionsView.InitializeColorRegionsView(colors);

            // Sets the picker's color to be the first color
            hsvColorSpectrumPicker.Color = colors[0];

            // Sets the title
            title.text = "Customize";

            gameObject.SetActive(true);
        }

        /// <summary>
        /// Unregisters events and disables the game object.
        /// </summary>
        public void Dispose()
        {
            this.gameObject.SetActive(false);

            OnColorSelected.RemoveAllListeners();
            colorRegionsView.OnRegionSelectionUpdated -= OnRegionSelectedUpdated;
            colorRegionsView.UnregisterColorRegionsSelected();

            hsvColorSpectrumPicker.ColorUpdated -= OnColorPickerUpdated;
            _PictureInPictureController.OnSizeModeChanged -= OnPiPSizeModeChanged;

            Destroy(this.gameObject);
        }

        private void OnPiPSizeModeChanged(PictureInPictureSizeMode sizeMode)
        {
            hsvColorSpectrumPicker.Interactable = sizeMode != PictureInPictureSizeMode.Big;
        }

        // Update the select color region color
        private void OnColorPickerUpdated(Color color)
        {
            colorRegionsView.SelectedColorRegionButton.Color = color;
        }

        // Update the spectrum picker to show the color of the selected region button
        private void OnRegionSelectedUpdated(int i)
        {
            hsvColorSpectrumPicker.Color = colorRegionsView.ColorRegionButtons[i].Color;
        }

        /// <summary>
        /// Currently we don't have alpha channel change so we force any color to have 1 on alpha
        /// for some reason the current color from AvatarController.Skin has 0 on alpha.
        /// </summary>
        /// <param name="color">the color to validate for this view</param>
        /// <returns>the validated color with alpha to be 1</returns>
        public static Color ValidateColorAlpha(Color color)
        {
            return new Color(color.r, color.g, color.b, 1f);
        }

        /// <summary>
        /// Checks if the input color params array is valid
        /// </summary>
        /// <param name="colors">color params array</param>
        /// <returns>true if valid, false if not</returns>
        public static bool ColorInputsValid(params Color[] colors)
        {
            // Check if the colors array is null
            if (colors == null)
            {
                Debug.LogError("Colors array is null");
                return false;
            }

            // Check if the colors array has the correct length
            // Adjust this to your needs
            if (colors.Length == 0)
            {
                Debug.LogError("Colors array is empty");
                return false;
            }

            // Check if each color is valid
            foreach (Color color in colors)
            {
                // Check if color has valid RGBA values
                if (color.r < 0 || color.r > 1 ||
                    color.g < 0 || color.g > 1 ||
                    color.b < 0 || color.b > 1 ||
                    color.a < 0 || color.a > 1)
                {
                    Debug.LogError("Invalid color value");
                    return false;
                }
            }

            return true;
        }
    }
}
