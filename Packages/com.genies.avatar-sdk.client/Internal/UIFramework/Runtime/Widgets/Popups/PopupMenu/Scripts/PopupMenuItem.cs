using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Genies.UI.Components.Widgets
{
    public enum PopupMenuItemBGType : ushort
    {
        Top = 0,
        Middle = 1,
        Bottom = 2
    }

    [RequireComponent(typeof(Image))]
    [RequireComponent(typeof(Button))]
    [RequireComponent(typeof(RectTransform))]
    public class PopupMenuItem : MonoBehaviour
    {
        [SerializeField]
        private Texture2D _topBG;
        [SerializeField]
        private Texture2D _middleBG;
        [SerializeField]
        private Texture2D _bottomBG;
        [SerializeField]
        private Image _icon;
        [SerializeField]
        private TextMeshProUGUI _title;
        [SerializeField]
        private Image _devider;

        private PopupMenuItemBGType _bgType;
        private Image _background;
        private Sprite _top;
        private Sprite _middle;
        private Sprite _bottom;

        public string Title
        {
            get => _title.text;
            set => _title.text = value;
        }

        public Sprite Icon
        {
            get => _icon.sprite;
            set
            {
                if (_icon != null)
                {
                    _icon.sprite = value;
                    _icon.gameObject.SetActive(value != null);
                }
            }
        }

        public PopupMenuItemBGType BackgroundType
        {
            get => _bgType;
            set
            {
                _bgType = value;

                switch (_bgType)
                {
                    case PopupMenuItemBGType.Top:
                        _background.sprite = _top;
                        break;

                    case PopupMenuItemBGType.Middle:
                        _background.sprite = _middle;
                        break;

                    case PopupMenuItemBGType.Bottom:
                        _background.sprite = _bottom;
                        _devider?.gameObject.SetActive(false);
                        break;

                }
            }
        }

        protected void Awake()
        {
            Debug.Assert(_topBG != null, "_topBG is not set!");
            Debug.Assert(_middleBG != null, "_middleBG is not set!");
            Debug.Assert(_bottomBG != null, "_bottomBG is not set!");
            Debug.Assert(_title != null, "_title is not set!");

            Initialize();
        }

        public void Initialize()
        {
            _background ??= GetComponent<Image>();
            _top ??= Sprite.Create(_topBG, new Rect(0, 0, _topBG.width, _topBG.height), new Vector2(0.5f, 0.5f));
            _middle ??= Sprite.Create(_middleBG, new Rect(0, 0, _middleBG.width, _middleBG.height), new Vector2(0.5f, 0.5f));
            _bottom ??= Sprite.Create(_bottomBG, new Rect(0, 0, _bottomBG.width, _bottomBG.height), new Vector2(0.5f, 0.5f));
        }
    }
}
