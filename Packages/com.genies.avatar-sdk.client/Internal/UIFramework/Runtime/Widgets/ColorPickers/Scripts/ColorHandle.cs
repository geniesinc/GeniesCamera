using UnityEngine;
using UnityEngine.UI;

namespace Genies.UI.Widgets
{
    [ExecuteAlways]
    public class ColorHandle : MonoBehaviour
    {
        internal const float DefaultHandleSize = 30f;
        internal const float DefaultFillRatio = 0.75f;

        [SerializeField] private RectTransform handleRect;
        [SerializeField] private RectTransform fillRect;
        [SerializeField] private Image fillImage;

        [SerializeField]
        private float handleSize = DefaultHandleSize;

        [SerializeField][Range(0, 1)]
        private float fillRatio = DefaultFillRatio;

        private bool _initialized;

        public float HandleSize
        {
            set
            {
                handleSize = value;
                UpdateHandleSize();
            }
        }

        public float FillRatio
        {
            set
            {
                fillRatio = value;
                UpdateFillRatio();
            }
        }

        public Vector2 AnchoredPosition
        {
            set
            {
                handleRect ??= GetComponent<RectTransform>();
                handleRect.anchoredPosition = value;
            }
        }

        public Color FillColor
        {
            set
            {
                fillImage ??= transform.GetChild(0).GetComponent<Image>();
                fillImage.color = value;
            }
        }

        private void Awake()
        {
            Initialize();
        }

        internal void Initialize()
        {
            if (_initialized)
            {
                return;
            }

            _initialized = true;

            handleRect ??= GetComponent<RectTransform>();
            fillRect ??= transform.GetChild(0).GetComponent<RectTransform>();
            fillImage ??= transform.GetChild(0).GetComponent<Image>();

            // Anchor to the bottom left, so it simplifies calculation for handle position for (0,1) of the two values.
            handleRect.anchorMin = Vector2.zero;
            handleRect.anchorMax = Vector2.zero;

            UpdateHandleSize();
            UpdateFillRatio();
        }

        internal void Reset()
        {
            handleSize = DefaultHandleSize;
            fillRatio = DefaultFillRatio;

            Initialize();
        }

        private void OnValidate()
        {
            UpdateHandleSize();
            UpdateFillRatio();
        }

        private void UpdateHandleSize()
        {
            handleRect ??= GetComponent<RectTransform>();
            handleRect.sizeDelta = Vector2.one * handleSize;
        }

        private void UpdateFillRatio()
        {
            fillRect ??= transform.GetChild(0).GetComponent<RectTransform>();
            fillRect.sizeDelta = Vector2.one * handleSize * fillRatio;
        }
    }
}
