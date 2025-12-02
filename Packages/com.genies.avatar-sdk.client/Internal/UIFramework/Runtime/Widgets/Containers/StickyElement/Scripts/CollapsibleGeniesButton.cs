using Genies.UI.Animations;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Genies.UIFramework;
using UnityEngine.Serialization;

namespace Genies.UI.Widgets
{
    public class CollapsibleGeniesButton : GeniesButton
    {
        [SerializeField]
        private Image _icon;

        [SerializeField]
        private TextMeshProUGUI _title;

        [SerializeField]
        private float _collapsedSize = 40f;

        [SerializeField]
        private Vector2 _collapsedIconPosition = Vector2.zero;

        [SerializeField]
        private bool _isVerticalExpand = false;

        private Vector2 _initialSize;
        private Vector2 _iconInitialPosition;
        private RectTransform _rectTransform;
        private RectTransform _iconRectTransform;
        private CanvasGroup _titleCanvasGroup;

        [FormerlySerializedAs("isCollapsed")] public bool IsCollapsed;

        public float ButtonWidth => _rectTransform.sizeDelta.x;

        protected void Awake()
        {
            Debug.Assert(_icon != null,  "_icon is not set!");
            Debug.Assert(_title != null, "_title is not set!");
            _rectTransform = GetComponent<RectTransform>();
            _titleCanvasGroup = _title.GetComponent<CanvasGroup>();
            _iconRectTransform = _icon.GetComponent<RectTransform>();
            _initialSize = _rectTransform.sizeDelta;
            _iconInitialPosition = _iconRectTransform.anchoredPosition;
            IsCollapsed = false;
        }

        public void Collapse()
        {
            if (IsCollapsed)
            {
                return;
            }

            var newSize = _isVerticalExpand ? new Vector3(_initialSize.x, _collapsedSize, 0) : new Vector3(_collapsedSize, _initialSize.y, 0);
            // Use snappy springs for responsive UI feel
            _rectTransform.SpringSizeDelta(newSize, SpringPhysics.Presets.Snappy);
            _titleCanvasGroup.SpringFade(0f, SpringPhysics.Presets.Snappy);
            _iconRectTransform.SpringLocalPosition(_collapsedIconPosition, SpringPhysics.Presets.Snappy);

            IsCollapsed = true;
        }

        public void Resize(float value)
        {
            var newSize = _isVerticalExpand ? new Vector3(_initialSize.x, _collapsedSize, 0) : new Vector3(_collapsedSize, _initialSize.y, 0);
            _rectTransform.sizeDelta = Vector2.Lerp(_initialSize, newSize, 1f - value);
            var titleValue = value - 0.5f > 0 ? (value - 0.5f) * 2f : value - 0.5f;
            _titleCanvasGroup.alpha = Mathf.Lerp(0f, 1f, titleValue);
            _iconRectTransform.anchoredPosition = Vector3.Lerp(
                                                               _iconInitialPosition,
                                                               _collapsedIconPosition, 1f - value
                                                              );
        }

        public void Expand()
        {
            if (!IsCollapsed)
            {
                return;
            }

            // Use gentle springs for smooth expansion with subtle bounce
            _rectTransform.SpringSizeDelta(_initialSize, SpringPhysics.Presets.Gentle);
            _titleCanvasGroup.SpringFade(1f, SpringPhysics.Presets.Gentle);
            _iconRectTransform.SpringLocalPosition(_iconInitialPosition, SpringPhysics.Presets.Gentle);

            IsCollapsed = false;
        }

        public float GetCollapsedSize()
        {
            return _collapsedSize;
        }

        public float GetExpandedSize(bool isVertical)
        {
            if (isVertical)
            {
                return _initialSize.y;
            }

            return _initialSize.x;
        }
    }
}
