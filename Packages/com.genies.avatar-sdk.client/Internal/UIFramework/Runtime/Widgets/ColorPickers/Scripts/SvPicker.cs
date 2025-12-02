using UnityEngine;

namespace Genies.UI.Widgets
{
    /// <summary>
    /// 2D spectrum picker for saturation and brightness.
    /// </summary>
    public class SvPicker : MonoBehaviour
    {
        /// <summary>
        /// Gets or sets whether the sv picker is interactable.
        /// </summary>
        public bool Interactable
        {
            get => pointerHandler2D != null && pointerHandler2D.enabled;
            set
            {
                if (pointerHandler2D != null)
                {
                    pointerHandler2D.enabled = value;
                }
            }
        }

        [SerializeField] private PointerHandler2D pointerHandler2D;
        [SerializeField] private Texture2D sourceTexture;

        public ColorHandle ColorHandle => colorHandle;
        [SerializeField] private ColorHandle colorHandle;

        [SerializeField] private RectTransform panelRect;

        public Vector2 SaturationRange => saturationRange;
        [SerializeField] private Vector2 saturationRange = new Vector2(0, 1);

        public Vector2 BrightnessRange => brightnessRange;
        [SerializeField] private Vector2 brightnessRange = new Vector2(0, 1);

        [SerializeField][Range(0, 1)]
        private float hue;

        [SerializeField][Range(0, 1)]
        private float saturation;

        [SerializeField][Range(0, 1)]
        private float brightness;

        /// <summary>
        /// Saturation value within the range set by <see cref="SaturationRange"/>.
        /// </summary>
        public float Saturation
        {
            get => saturation;
            set
            {
                saturation = Mathf.Clamp(value, saturationRange.x, saturationRange.y);
                UpdateHandlePosition();
                UpdateHandleFillColor();
            }
        }

        /// <summary>
        /// Brightness value within range set by <see cref="BrightnessRange"/>.
        /// </summary>
        public float Brightness
        {
            get
            {
                return Mathf.Clamp(brightness, brightnessRange.x, brightnessRange.y);
            }
            set
            {
                brightness = Mathf.Clamp(value, brightnessRange.x, brightnessRange.y);
                UpdateHandlePosition();
                UpdateHandleFillColor();
            }
        }

        /// <summary>
        /// Sets the Hue of the background image, also updates the fill color of the handle.
        /// </summary>
        public float Hue
        {
            set
            {
                hue = Mathf.Clamp01(value);

                UpdateSourceTexture();
                UpdateHandleFillColor();
            }
        }

        public delegate void SvChangedEventHandler(float saturation, float brightness);
        public event SvChangedEventHandler OnValueChanged;

        private bool _initialized;

        public int Width => (int)panelRect.sizeDelta.x;
        public int Height => (int)panelRect.sizeDelta.y;

        private void Awake()
        {
            panelRect ??= GetComponent<RectTransform>();
            colorHandle ??= GetComponentInChildren<ColorHandle>();
        }

        /// <summary>
        /// Initializes the color handle.
        /// </summary>
        internal void Initialize()
        {
            colorHandle.Initialize();

            UpdateHandlePosition();
            UpdateHandleFillColor();
        }

        private void OnEnable()
        {
            pointerHandler2D.onValueChanged.AddListener(UpdateSaturationBrightness);
        }

        private void OnDisable()
        {
            pointerHandler2D.onValueChanged.RemoveAllListeners();
        }

        public void UpdateSourceTexture()
        {
            var width = Width;
            var height = Height;

            if (sourceTexture == null)
            {
                sourceTexture = GenerateSvTexture();
            }

            for (var x = 0; x < width; x++)
            {
                for (var y = 0; y < height; y++)
                {
                    var s = Mathf.Lerp(saturationRange.x, saturationRange.y, (float)x / width);
                    var v = Mathf.Lerp(brightnessRange.x, brightnessRange.y, (float)y / height);

                    sourceTexture.SetPixel(x, y, Color.HSVToRGB(hue, s, v));
                }
            }

            sourceTexture.Apply();
        }

        public Texture2D GenerateSvTexture()
        {
            var width = Width;
            var height = Height;

            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);

            for (var x = 0; x < width; x++)
            {
                for (var y = 0; y < height; y++)
                {
                    var s = Mathf.Lerp(saturationRange.x, saturationRange.y, (float)x / width);
                    var v = Mathf.Lerp(brightnessRange.x, brightnessRange.y, (float)y / height);

                    texture.SetPixel(x, y, Color.HSVToRGB(hue, s, v));
                }
            }

            texture.Apply();
            return texture;
        }

        private void UpdateSaturationBrightness(Vector2 pointerValue)
        {
            Saturation = Mathf.Lerp(saturationRange.x, saturationRange.y, pointerValue.x);
            Brightness = Mathf.Lerp(brightnessRange.x, brightnessRange.y, pointerValue.y);

            OnValueChanged?.Invoke(Saturation, Brightness);
        }

        public void UpdateHandlePosition()
        {
            panelRect ??= GetComponent<RectTransform>();
            Vector2 panelSizeDelta = panelRect.sizeDelta;

            // Update anchoredPosition
            colorHandle.AnchoredPosition = new Vector2(
                 Mathf.Clamp01(Mathf.InverseLerp(saturationRange.x, saturationRange.y, saturation)) * panelSizeDelta.x,
                 Mathf.Clamp01(Mathf.InverseLerp(brightnessRange.x, brightnessRange.y, brightness)) * panelSizeDelta.y
            );

            UpdateHandleFillColor();
        }

        public void UpdateHandleFillColor()
        {
            colorHandle.FillColor = Color.HSVToRGB(hue, saturation, brightness);
        }
    }
}
