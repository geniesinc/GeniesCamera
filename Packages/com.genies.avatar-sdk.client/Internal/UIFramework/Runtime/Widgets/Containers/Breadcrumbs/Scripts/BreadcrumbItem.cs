using System;
using System.Collections;
using Cysharp.Threading.Tasks;
using Genies.UI.Animations;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Genies.UIFramework;

namespace Genies.UI.Widgets
{
    public enum ItemType
    {
        Middle = 0,
        First = 1,
        Last = 2,
        Single = 3,
    }
    public class BreadcrumbItem : GeniesButton
    {
        [Header("Breadcrumb Elements")]
        [SerializeField]
        private Image _first;
        [SerializeField]
        private Image _last;
        [SerializeField]
        private Image _middle;
        [SerializeField]
        private Image _leftSeparator;
        [SerializeField]
        private Image _rightSeparator;
        [SerializeField]
        private TextMeshProUGUI _textMesh;

        [Header("Breadcrumb Config")]
        public Color ClickColor = Color.white;
        public Color MainColor = new Color32(58, 57, 57, 255);
        [Range(0, 100)]
        public int Offset = 20; //Percents
        public int MinCenterWidth = 60;

        private IBreadcrumb _breadcrumb;
        private ItemType _itemType = ItemType.Middle;
        private RectTransform _textRect;
        private RectTransform _middleImageRect;
        private RectTransform _buttonContainerRect;
        private RectTransform _rootRect;
        private RectTransform _firstRect;
        private RectTransform _lastRect;
        private RectTransform _mainComponentRect;

        private bool _canAnimate;

        public string Title
        {
            get => _textMesh.text;
            set
            {
                _textMesh.text = value;
                StartCoroutine(SetSize());
            }
        }

        public IBreadcrumb Breadcrumb
        {
            get => _breadcrumb;
            set
            {
                _breadcrumb = value;
                Title = _breadcrumb.Title;
            }
        }

        public ItemType ItemType
        {
            get => _itemType;
            set
            {
                _itemType = value;
                ConfigureBackground();
            }
        }

        protected virtual void Awake()
        {
            Debug.Assert(_first != null, "_first is not set!");
            Debug.Assert(_last != null, "_last is not set!");
            Debug.Assert(_middle != null, "_middle is not set!");
            Debug.Assert(_leftSeparator != null, "_separator is not set!");
            Debug.Assert(_rightSeparator != null, "_rightSeparator is not set!");

            _canAnimate = true;
        }

        public virtual void Initialize()
        {
            ConfigureBackground();
            StartCoroutine(SetSize());
        }

        private void ConfigureBackground()
        {
            switch (_itemType)
            {
                case ItemType.Middle:
                    _rightSeparator.gameObject.SetActive(true);
                    _leftSeparator.gameObject.SetActive(false);
                    _first.gameObject.SetActive(false);
                    _last.gameObject.SetActive(false);
                    break;
                case ItemType.First:
                    _rightSeparator.gameObject.SetActive(true);
                    _leftSeparator.gameObject.SetActive(false);
                    _first.gameObject.SetActive(true);
                    _last.gameObject.SetActive(false);
                    break;
                case ItemType.Last:
                    _first.gameObject.SetActive(false);
                    _last.gameObject.SetActive(true);
                    _rightSeparator.gameObject.SetActive(false);
                    _leftSeparator.gameObject.SetActive(false);
                    break;
                case ItemType.Single:
                    _first.gameObject.SetActive(true);
                    _last.gameObject.SetActive(true);
                    _rightSeparator.gameObject.SetActive(false);
                    _leftSeparator.gameObject.SetActive(false);
                    break;
            }
        }

        private IEnumerator SetSize()
        {
            if (_canAnimate)
            {
                CanvasGroup.alpha = 0f;
            }

            yield return new WaitForEndOfFrame();

            _textRect ??= _textMesh.GetComponent<RectTransform>();
            _middleImageRect ??= _middle.GetComponent<RectTransform>();
            _firstRect ??= _first.GetComponent<RectTransform>();
            _lastRect ??= _last.GetComponent<RectTransform>();
            _buttonContainerRect ??= _middleImageRect.transform.parent.GetComponent<RectTransform>();
            _rootRect ??= gameObject.GetComponent<RectTransform>();
            var leftRightOffset = (Mathf.Max(_textRect.sizeDelta.x, MinCenterWidth) / 100f) * Offset * 2f;
            _middleImageRect.sizeDelta = new Vector2(_textRect.sizeDelta.x + leftRightOffset, _middleImageRect.sizeDelta.y);
            _buttonContainerRect.sizeDelta = new Vector2(_textRect.sizeDelta.x + leftRightOffset, _buttonContainerRect.sizeDelta.y);
            var rootWidth = _textRect.sizeDelta.x + leftRightOffset;
            rootWidth = _firstRect.gameObject.activeSelf ? rootWidth + _firstRect.sizeDelta.x : rootWidth;
            rootWidth = _lastRect.gameObject.activeSelf ? rootWidth + _lastRect.sizeDelta.x : rootWidth;
            _rootRect.sizeDelta = new Vector2(rootWidth, _rootRect.sizeDelta.y);

            _first.color = MainColor;
            _middle.color = MainColor;
            _last.color = MainColor;

            yield return new WaitForEndOfFrame();
            _mainComponentRect ??= gameObject.transform.parent.parent.GetComponent<RectTransform>();

            LayoutRebuilder.ForceRebuildLayoutImmediate(_mainComponentRect);

            yield return new WaitForEndOfFrame();

            AnimateIn();
        }

        protected override float DoClickAnimation(float t)
        {
            t += Time.deltaTime / OnClickAnimTime;
            t = Mathf.Clamp01(t);

            _first.color = Color.LerpUnclamped(MainColor, ClickColor, OnClickScaleCurve.Evaluate(t));
            _middle.color = Color.LerpUnclamped(MainColor, ClickColor, OnClickScaleCurve.Evaluate(t));
            _last.color = Color.LerpUnclamped(MainColor, ClickColor, OnClickScaleCurve.Evaluate(t));

            return t;
        }

        private void AnimateIn()
        {
            if (!_canAnimate)
            {
                return;
            }

            _textRect.anchoredPosition = new Vector2(-_textRect.sizeDelta.x, 0f);

            // Springs for smooth, natural animation
            _textRect.SpringAnchorPos(new Vector2(0f, 0f), SpringPhysics.Presets.Gentle);
            CanvasGroup.SpringFade(1f, SpringPhysics.Presets.Smooth);

            _canAnimate = false;
        }

        public async void AnimateOut(Action onComplete)
        {
            await UniTask.WhenAll(
                _textRect.SpringAnchorPos(new Vector2(-_textRect.sizeDelta.x, 0f), SpringPhysics.Presets.Gentle).AsyncWaitForCompletion(),
                CanvasGroup.SpringFade(0f, SpringPhysics.Presets.Smooth).AsyncWaitForCompletion()
            );

            onComplete?.Invoke();
        }
    }
}
