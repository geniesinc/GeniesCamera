using System;
using UnityEngine;
using UnityEngine.UI;

namespace Genies.MegaEditor
{
    public class ColorRegionButton : MonoBehaviour
    {
        [SerializeField] protected Button button;
        [SerializeField] protected Image outline;
        public Color selectedOutlineColor = Color.magenta;
        public Color unselectedOutlineColor = Color.white;
        public float selectedOutlineWidth = 3.0f;
        public float unselectedOutlineWidth = 1.0f;

        private bool _isSelected;

        /// <summary>
        /// Event triggered when this color region is selected (button is clicked) to pass its index.
        /// </summary>
        public event Action<int> OnColorRegionSelected;

        /// <summary>
        /// Index of the button using SiblingIndex.
        /// </summary>
        public int Index => transform.GetSiblingIndex();

        /// <summary>
        /// Gets or sets the selection state of this button. Setting it will update visuals accordingly.
        /// </summary>
        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                _isSelected = value;

                // update selected visuals
                UpdateSelectedVisuals();
            }
        }

        public Color Color
        {
            get => button.image.color;
            set => button.image.color = value;
        }

        private void Awake()
        {
            button ??= GetComponentInChildren<Button>();
        }

        private void OnEnable()
        {
            // Always have the first button selected by default.
            _isSelected = Index == 0;

            button.onClick.AddListener(() => OnColorRegionSelected?.Invoke(Index));

            UpdateSelectedVisuals();
        }

        private void OnDisable()
        {
            button.onClick.RemoveAllListeners();
        }

        private void UpdateSelectedVisuals()
        {
            outline.color = _isSelected ? selectedOutlineColor : unselectedOutlineColor;
            SetOutlineWidth(_isSelected ? selectedOutlineWidth : unselectedOutlineWidth);
        }

        private void SetOutlineWidth(float width)
        {
            outline.rectTransform.offsetMin = new Vector2(-width, -width);
            outline.rectTransform.offsetMax = new Vector2(width, width);
        }
    }
}