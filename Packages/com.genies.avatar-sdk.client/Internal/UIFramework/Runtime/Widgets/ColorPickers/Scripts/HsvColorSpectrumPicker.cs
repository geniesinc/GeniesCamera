using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Genies.UI.Widgets
{
    /// <summary>
    /// Integrated HSV color picker UI widget with a 2D spectrum area for saturation and brightness and a hue slider.
    /// </summary>
    public class HsvColorSpectrumPicker : MonoBehaviour, IColorPicker
    {
        public bool Interactable
        {
            get => svPicker.Interactable && huePicker.Interactable;
            set
            {
                var interactable = value;
                svPicker.Interactable = interactable;
                huePicker.Interactable = interactable;
            }
        }
        [SerializeField] private RectTransform rectTransfrom;
        [SerializeField] private HuePicker huePicker;
        [SerializeField] private SvPicker svPicker;

        [SerializeField] private RectTransform hueGradient;

        [SerializeField] private Image roundCornerMaskImage;

        [SerializeField] private float handleSize = ColorHandle.DefaultHandleSize;

        [SerializeField]
        [Range(0f, 1f)]
        private float handleFillRatio = ColorHandle.DefaultFillRatio;

        [SerializeField] private Color defaultColor;

        [Space(5)]
        [Header("Editor Test")]
        [SerializeField]
        private bool isEditorTest;
        [SerializeField] private Color testColor;

        public UnityEvent<Color> colorSelected = new UnityEvent<Color>();

        private float _hue;
        private bool _initialized;

        #region IColorPicker implementation

        /// <inheritdoc/>
        public Color Color
        {
            get => Color.HSVToRGB(_hue, svPicker.Saturation, svPicker.Brightness);
            set
            {
                SetColorWithoutNotify(value);
                NotifyColorChange();
            }
        }

        /// <inheritdoc/>
        public event Action<Color> ColorUpdated;

        /// <inheritdoc/>
        public void SetColorWithoutNotify(Color color)
        {
            Color.RGBToHSV(color, out var hue, out var saturation, out var brightness);

            _hue = Mathf.Clamp01(hue);

            svPicker.Hue = hue;
            svPicker.Saturation = Mathf.Clamp01(saturation);
            svPicker.Brightness = Mathf.Clamp01(brightness);

            huePicker.Slider.SetValueWithoutNotify(_hue);
            UpdateHandlesColor();
        }
        #endregion

        private void Initialize()
        {
            if (_initialized)
            {
                return;
            }

            _initialized = true;

            huePicker.Initialize();
            svPicker.Initialize();

            SetRoundedCorners();

            // set default color
            Color = defaultColor;

            UpdateHandlesColor();
            UpdateHandleSizes();
            UpdateHandleFillRatio();
        }

        private void Awake()
        {
            Initialize();
        }

        private void OnValidate()
        {
            // Update color and handle sizes in the editor
            if (isEditorTest)
            {
                Initialize();
                Color = testColor;

                UpdateHandleSizes();
                UpdateHandleFillRatio();
            }
        }

        private void OnEnable()
        {
            huePicker.Slider.onValueChanged.AddListener(OnHueSliderValueChanged);
            svPicker.OnValueChanged += OnSvChanged;
        }

        private void OnDisable()
        {
            huePicker.Slider.onValueChanged.RemoveListener(OnHueSliderValueChanged);
            svPicker.OnValueChanged -= OnSvChanged;
        }

        private void OnHueSliderValueChanged(float value)
        {
            // hue is in range [0, 1] which maps to the slider value [0, 1].
            _hue = Mathf.Clamp01(value);

            // Sets the sv picker to change its Spectrum color and Handle color;
            svPicker.Hue = _hue;

            // Sets the color of the two handles
            UpdateHandlesColor();

            // Trigger events
            NotifyColorChange();
        }

        private void OnSvChanged(float saturation, float brightness)
        {
            UpdateHandlesColor();
            NotifyColorChange();
        }

        private void UpdateHandlesColor()
        {
            huePicker.ColorHandle.FillColor = Color;
            svPicker.Hue = _hue;
        }

        private void UpdateHandleSizes()
        {
            huePicker.ColorHandle.HandleSize = handleSize;
            svPicker.ColorHandle.HandleSize = handleSize;
        }

        private void UpdateHandleFillRatio()
        {
            huePicker.ColorHandle.FillRatio = handleFillRatio;
            svPicker.ColorHandle.FillRatio = handleFillRatio;
        }

        private void NotifyColorChange()
        {
            Color color = Color;
            ColorUpdated?.Invoke(color);
            colorSelected.Invoke(color);
        }

        // Make the rounded corner cover the height of the hue gradient slider area.
        private void SetRoundedCorners()
        {
            roundCornerMaskImage ??= GetComponentInChildren<Image>();
            roundCornerMaskImage.pixelsPerUnitMultiplier = hueGradient.sizeDelta.y;
        }

        public RectTransform GetRectTransform()
        {
            return rectTransfrom;
        }
    }
}
