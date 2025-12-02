using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Genies.UI.Widgets
{
    /// <summary>
    /// HSV color picker implementation.
    /// </summary>
    public sealed class HsvColorPicker : MonoBehaviour, IColorPicker
    {
        [SerializeField]
        private Slider hueSlider;

        [SerializeField]
        private Slider saturationSlider;

        [SerializeField]
        private Slider brightnessSlider;

        [SerializeField]
        private Image[] handleImages;

        [SerializeField]
        private Image saturationBar;

        [SerializeField]
        private Image brightnessBar;

        [Space(5)]
        public UnityEvent<Color> colorSelected = new UnityEvent<Color>();

        public event Action<Color> ColorUpdated;

        public Color Color
        {
            get => Color.HSVToRGB(_hue, _saturation, _brightness);
            set
            {
                SetColorWithoutNotify(value);
                NotifyColorChange(value);
            }
        }

        // white by default
        private float _hue = 0.0f;
        private float _saturation = 0.0f;
        private float _brightness = 1.0f;
        private bool _initialized;

        private void Initialize()
        {
            if (_initialized)
            {
                return;
            }

            _initialized = true;

            // initialize all sliders range to be [0, 1]
            hueSlider.minValue = saturationSlider.minValue = brightnessSlider.minValue = 0.0f;
            hueSlider.maxValue = saturationSlider.maxValue = brightnessSlider.maxValue = 1.0f;

            // initialize the component to the current color
            SetColorWithoutNotify(Color);
        }

        private void Start()
        {
            Initialize();
        }

        private void OnEnable()
        {
            hueSlider.onValueChanged.AddListener(OnHueSliderValueChanged);
            saturationSlider.onValueChanged.AddListener(OnSaturationSliderValueChanged);
            brightnessSlider.onValueChanged.AddListener(OnBrightnessSliderValueChanged);
        }

        private void OnDisable()
        {
            hueSlider.onValueChanged.RemoveListener(OnHueSliderValueChanged);
            saturationSlider.onValueChanged.RemoveListener(OnSaturationSliderValueChanged);
            brightnessSlider.onValueChanged.RemoveListener(OnBrightnessSliderValueChanged);
        }

        public void SetColorWithoutNotify(Color color)
        {
            Initialize();

            Color.RGBToHSV(color, out float hue, out float saturation, out _brightness);

            // don't modify the hue slider when we are black or white, this is for a better user experience
            bool ignoreHue = saturation <= 0.0f || _brightness <= 0.0f || (_hue <= 0.0f && hue >= 1.0f) ||
                (_hue >= 1.0f && hue <= 0.0f);
            if (!ignoreHue)
            {
                _hue = hue;
            }

            // do the same for saturation
            if (_brightness > 0.0f)
            {
                _saturation = saturation;
            }

            hueSlider.SetValueWithoutNotify(_hue);
            saturationSlider.SetValueWithoutNotify(_saturation);
            brightnessSlider.SetValueWithoutNotify(_brightness);
            UpdateImages(color);
        }

        private void OnHueSliderValueChanged(float value)
        {
            _hue = Mathf.Clamp01(value);
            Color color = Color;
            UpdateImages(color);
            NotifyColorChange(color);
        }

        private void OnSaturationSliderValueChanged(float value)
        {
            _saturation = Mathf.Clamp01(value);
            Color color = Color;
            UpdateImages(color);
            NotifyColorChange(color);
        }

        private void OnBrightnessSliderValueChanged(float value)
        {
            _brightness = Mathf.Clamp01(value);
            Color color = Color;
            UpdateImages(color);
            NotifyColorChange(color);
        }

        private void UpdateImages(Color color)
        {
            // update handles to be the selected color
            foreach (Image image in handleImages)
            {
                image.color = color;
            }

            // update saturation and brightness bars with pure color (this is the best option for now)
            // it would be nice to make a proper update of the gradient transition but that is harder than it seems
            color = Color.HSVToRGB(_hue, 1.0f, 1.0f);
            saturationBar.color = color;
            brightnessBar.color = color;
        }

        private void NotifyColorChange(Color color)
        {
            ColorUpdated?.Invoke(color);
            colorSelected.Invoke(color);
        }
    }
}
