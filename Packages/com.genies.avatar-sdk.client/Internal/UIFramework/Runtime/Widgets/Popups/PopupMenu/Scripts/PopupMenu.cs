using Genies.UI.Widgets;
using System;
using System.Collections.Generic;
using System.Linq;
using Genies.CrashReporting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Genies.UI.Components.Widgets
{
    [Serializable]
    public class MenuItemData
    {
        public string Title;
        public Sprite Icon;
        public Color TextColor = Color.white;
    }
    public class PopupMenu : PopupWidget
    {
        [SerializeField]
        protected List<MenuItemData> _menuItems;
        [SerializeField]
        private GameObject _menuItemPrefab;
        [SerializeField]
        private GameObject _menuRoot;
        [SerializeField]
        private float _verticalOffset = 35f;
        [SerializeField]
        private float _emptyPixelsOffset = 6f;
        [SerializeField]
        private float _totalHeightOffset = 20f;
        private RectTransform _menuRect;
        private bool _isDismissed;
        private int _currentIndex;

        public event Action<int> MenuItemPressed;
        public event Action MenuDismissed;
        public bool InitializeOnLaunch = true;
        public int MenuItemCount => _menuItems.Count;

        protected readonly Dictionary<string, MenuItemData> _menuItemsDictio = new Dictionary<string, MenuItemData>();

        protected void Awake()
        {
            if (InitializeOnLaunch)
            {
                InitializeMenu();
            }
        }

        public void InitializeMenu()
        {
            int index = 0;
            float menuItemWidth = 0;
            float menuHeight = _verticalOffset * _menuItems.Count + _totalHeightOffset;
            foreach (var item in _menuItems)
            {
                var go = Instantiate(_menuItemPrefab, ContentGroup.transform);
                var button = go.GetComponent<Button>();
                var menuItem = go.GetComponent<PopupMenuItem>();
                menuItem.Initialize();
                menuItem.Title = item.Title;
                menuItem.Icon = item.Icon;
                SetBackground(index, menuItem);
                var rectTransform = go.GetComponent<RectTransform>();
                menuItemWidth = rectTransform.sizeDelta.x;
                rectTransform.anchoredPosition = new Vector2(0, -(_verticalOffset * index) + menuHeight /2f -
                                                                  rectTransform.sizeDelta.y /2f + _emptyPixelsOffset);
                button.onClick.AddListener(OnMenuItemClick);
                index++;

                _menuItemsDictio.TryAdd(item.Title, item);
            }

            _menuRect = _menuRoot.GetComponent<RectTransform>();
            _menuRect.sizeDelta = new Vector2(menuItemWidth, menuHeight);
            var rootRect = GetComponent<RectTransform>();
            rootRect.sizeDelta = _menuRect.sizeDelta;
        }

        public void AddMenuItem(string title, Color titleColor, Sprite sprite = null, int? index = null)
        {
            var menuItemData = new MenuItemData() { Title = title, TextColor = titleColor, Icon = sprite };
            if (index == null)
            {
                _menuItems.Add(menuItemData);
            }
            else
            {
                _menuItems.Insert((int) index, menuItemData);
            }

            if(_menuItemsDictio.TryGetValue(title, out _))
            {
                _menuItemsDictio[title] =  menuItemData;
            }
            else
            {
                _menuItemsDictio.TryAdd(title, menuItemData);
            }
        }

        public void Cleanup(bool clearOnlyObjects)
        {
            foreach (Transform transform in ContentGroup.transform)
            {
                Destroy(transform.gameObject);
            }

            if (!clearOnlyObjects)
            {
                _menuItems.Clear();
                _menuItemsDictio.Clear();
            }
        }

        private void SetBackground(int index, PopupMenuItem menuItem)
        {
            if (index == 0)
            {
                menuItem.BackgroundType = PopupMenuItemBGType.Top;
            }
            else if (index == _menuItems.Count - 1)
            {
                menuItem.BackgroundType = PopupMenuItemBGType.Bottom;
            }
            else
            {
                menuItem.BackgroundType = PopupMenuItemBGType.Middle;
            }
        }

        private void OnMenuItemClick()
        {
            var selectedButton = EventSystem.current.currentSelectedGameObject;

            if(selectedButton != null)
            {
                var menuItem = selectedButton.GetComponent<PopupMenuItem>();
                if(menuItem != null)
                {
                    var menuItemData = _menuItems.FirstOrDefault(x => x.Title == menuItem.Title);
                    _currentIndex = _menuItems.IndexOf(menuItemData);
                    Hide();
                }
            }
        }

        protected override void HandleTapDismiss()
        {
            if (AllowTapToDismiss && _animTime >= 1f)
            {
                if (Application.isEditor)
                {
                    HandleKeyboardDismiss();
                }
                else
                {
                    HandleTouchDismiss();
                }
            }
        }

        private void HandleTouchDismiss()
        {
            if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
            {
                var touch = Input.GetTouch(0);
                var localTouchPos = PanelRectTransform.InverseTransformPoint(touch.position);
                if (!PanelRectTransform.rect.Contains(localTouchPos))
                {
                    Hide();
                    _isDismissed = true;
                }
            }
        }

        private void HandleKeyboardDismiss()
        {
            if (Input.GetMouseButtonDown(0))
            {
                var localPos = PanelRectTransform.InverseTransformPoint(Input.mousePosition);
                if (!PanelRectTransform.rect.Contains(localPos))
                {
                    Hide();
                    _isDismissed = true;
                }
            }
        }

        protected override void OnHidden()
        {
            try
            {
                if (_isDismissed)
                {
                    MenuDismissed?.Invoke();
                    _isDismissed = false;
                }
                else
                {
                    MenuItemPressed?.Invoke(_currentIndex);
                }
            }
            catch(Exception ex)
            {
                CrashReporter.LogHandledException(ex);
            }
            finally
            {
                gameObject.SetActive(false);
            }
        }
    }
}
