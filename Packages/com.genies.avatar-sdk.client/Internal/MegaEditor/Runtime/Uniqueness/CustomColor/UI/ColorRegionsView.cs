using System;
using System.Collections.Generic;
using Genies.Utilities.Internal;
using UnityEngine;

namespace Genies.MegaEditor
{

    public class ColorRegionsView : MonoBehaviour
    {
        [SerializeField] private ColorRegionButton colorRegionButtonPrefab;

        [SerializeField] private int regionCount;

        [SerializeField] private int selectedRegionIndex;

        /// <summary>
        /// Gets or sets the index of the selected region.
        /// Setting it to a specific one will also deselect others.
        /// </summary>
        public int SelectedRegionIndex
        {
            get => selectedRegionIndex;
            set
            {
                selectedRegionIndex = value;
                UpdateSelection(selectedRegionIndex);
            }
        }

        [SerializeField]
        [Tooltip("List of color region buttons, " +
                 "will be automatically instantiated and balanced to match the region count")]
        private List<ColorRegionButton> colorRegionButtons = new List<ColorRegionButton>();

        /// <summary> Gets the list of color region buttons. </summary>
        public List<ColorRegionButton> ColorRegionButtons => colorRegionButtons;

        /// <summary> Gets the selected color region button. </summary>
        public ColorRegionButton SelectedColorRegionButton =>
            colorRegionButtons.Count != 0 ? colorRegionButtons[selectedRegionIndex] : null;

        /// <summary> Events when the region selection is updated. </summary>
        public event Action<int> OnRegionSelectionUpdated;

        /// <summary>
        /// Initializes the color regions to have the same number as the length of the given color array.
        /// Initializes each color region according to the corresponding color in the array.
        /// </summary>
        /// <param name="colors">the given </param>
        public void InitializeColorRegionsView(params Color[] colors)
        {
            //if (!CustomizeColorView.ColorInputsValid(colors))
            //return;

            regionCount = colors.Length;

            // Instantiate UI prefabs under the attached transform.
            UIUtils.BalanceListOfChildPrefabs(colorRegionButtonPrefab, regionCount, transform, colorRegionButtons);

            // Initialize every color region with the given color.
            for (var i = 0; i < regionCount; i++)
            {
                colorRegionButtons[i].OnColorRegionSelected += UpdateSelection;
                colorRegionButtons[i].Color = colors[i];
            }
        }

        public Color[] GetCurrentColors()
        {
            var currentColors = new List<Color>();
            // Initialize every color region with the given color.
            for (var i = 0; i < regionCount; i++)
            {
                currentColors.Add(colorRegionButtons[i].Color);
            }

            return currentColors.ToArray();
        }

        /// <summary>
        /// Unregisters the selected events for each color region button.
        /// </summary>
        public void UnregisterColorRegionsSelected()
        {
            foreach (ColorRegionButton colorRegionButton in colorRegionButtons)
            {
                colorRegionButton.OnColorRegionSelected -= UpdateSelection;
            }
        }

        private void UpdateSelection(int selectedId)
        {
            if (selectedId < 0 || selectedId >= regionCount || selectedRegionIndex == selectedId)
            {
                return;
            }

            // Deselect others and select only the selected Id.
            foreach (ColorRegionButton colorRegionButton in colorRegionButtons)
            {
                colorRegionButton.IsSelected = colorRegionButton.Index == selectedId;
            }

            // Update selection.
            selectedRegionIndex = selectedId;
            OnRegionSelectionUpdated?.Invoke(selectedRegionIndex);
        }
    }
}