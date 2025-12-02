using Genies.UI.Components.Widgets;
using System;
using System.Collections.Generic;
using System.Linq;
using Genies.CrashReporting;
using Genies.Utilities;
using UnityEngine;

namespace Genies.UI.Widgets
{
    public class Breadcrumbs : Widget
    {
        [SerializeField]
        private GameObject _itemPrefab;
        [SerializeField]
        private GameObject _container;
        [SerializeField]
        private PopupMenu _popupMenu;
        [Range(3, 6)]
        public int VisibleItems = 5;
        public float PopupMenuOffset = 20f;
        public Color LeftItemColor = new Color32(58, 57, 57, 255);
        public Color RightItemColor = new Color32(24, 22, 23, 255);

        private List<BreadcrumbItem> _breadcrumbVisibleItems = new List<BreadcrumbItem>();
        private static List<IBreadcrumb> _breadcrumbs = new List<IBreadcrumb>();
        private List<IBreadcrumb> _menuItems = new List<IBreadcrumb>();
        private readonly string _threeDots = "...";
        private Canvas _canvas;
        private RectTransform _canvasRect;
        private RectTransform _popupMenuRect;
        private Vector2 _popupMenuDefaultPosition;

        public override void OnWidgetInitialized()
        {
            Debug.Assert(_itemPrefab != null, "_itemPrefab is not set!");
            Debug.Assert(_container != null, "_container is not set!");
            Debug.Assert(_popupMenu != null, "_popupMenu is not set!");
            _popupMenuRect = _popupMenu.GetComponent<RectTransform>();
            _popupMenuDefaultPosition = _popupMenuRect.anchoredPosition;
            _popupMenu.MenuItemPressed += OnMenuItemPressed;
            _popupMenu.MenuDismissed += OnMenuDismissed;

            _canvas = GetComponentInParent<Canvas>();

            if (_canvas != null)
            {
                _canvasRect = _canvas.gameObject.GetComponent<RectTransform>();
            }
        }

        public void ClearBreadcrumbs(bool shouldRebuildUi = true)
        {
            _breadcrumbs.Clear();

            if (shouldRebuildUi)
            {
                RebuildUI();
            }
        }

        public void SetBreadcrumb(IBreadcrumb breadcrumb)
        {
            var existingBreadcrumb = _breadcrumbs.FirstOrDefault(x => x.BreadcrumbId == breadcrumb.BreadcrumbId);

            if (existingBreadcrumb == null)
            {
                _breadcrumbs.Add(breadcrumb);
            }
        }

        public void RebuildUI()
        {
            //Ensure we have enough visible items
            var visibleElementsNeeded = Mathf.Min(_breadcrumbs.Count, VisibleItems);
            while (_breadcrumbVisibleItems.Count != visibleElementsNeeded)
            {
                //Remove extra
                if (_breadcrumbVisibleItems.Count > visibleElementsNeeded)
                {
                    var item = _breadcrumbVisibleItems[_breadcrumbVisibleItems.Count - 1];
                    _breadcrumbVisibleItems.Remove(item);
                    item.AnimateOut(onComplete: () => Destroy(item.gameObject));
                }
                //Add more if needed.
                else
                {
                    var breadcrumbItemGO = Instantiate(_itemPrefab, _container.transform);
                    var breadcrumbItem   = breadcrumbItemGO.GetComponent<BreadcrumbItem>();
                    _breadcrumbVisibleItems.Add(breadcrumbItem);
                }
            }

            ReassignBreadcrumbVisibleItems();
        }

        private void ReassignBreadcrumbVisibleItems()
        {
            int menuIndex = VisibleItems / 2;
            int j = _breadcrumbs.Count - menuIndex;
            var count = Mathf.Min(VisibleItems, _breadcrumbs.Count);
            menuIndex = VisibleItems % 2 == 0 ? menuIndex - 1 : menuIndex;

            for (int i = 0; i < count; i++)
            {
                if (i == count - 1)
                {
                    _breadcrumbVisibleItems[i].MainColor = RightItemColor;
                }
                else
                {
                    _breadcrumbVisibleItems[i].MainColor = Color.Lerp(LeftItemColor, RightItemColor, i / (float)count);
                }

                if (i < menuIndex || _breadcrumbs.Count <= VisibleItems)
                {
                    CreateItemsBeforeMenu(i);
                }
                else if (i == menuIndex)
                {
                    if (_breadcrumbs.Count > VisibleItems)
                    {
                        CreatePopupMenuButton(i);
                    }
                    else
                    {
                        CreateItemsAfterMenu(j - 1, i);
                    }
                }
                else
                {
                    CreateItemsAfterMenu(j, i);
                    j++;
                }
            }
        }

        private void CreateItemsAfterMenu(int j, int i)
        {
            _breadcrumbVisibleItems[i].Breadcrumb = _breadcrumbs[j];
            _breadcrumbVisibleItems[i].Title = _breadcrumbs[j].Title;
            _breadcrumbVisibleItems[i].ItemType =
                i == Mathf.Min(VisibleItems, _breadcrumbs.Count) - 1 ?
                ItemType.Last : ItemType.Middle;

            _breadcrumbVisibleItems[i].onClick.RemoveAllListeners();
            _breadcrumbVisibleItems[i].onClick.AddListener(() => OnBreadcrumbClick(_breadcrumbs[j]));
            _breadcrumbVisibleItems[i].Initialize();
        }

        private void CreatePopupMenuButton(int i)
        {
            _breadcrumbVisibleItems[i].Title = _threeDots;
            _breadcrumbVisibleItems[i].ItemType = ItemType.Middle;
            _breadcrumbVisibleItems[i].onClick.RemoveAllListeners();
            _breadcrumbVisibleItems[i].onClick.AddListener(OnThreeDotsClick);
            _breadcrumbVisibleItems[i].Initialize();
        }

        private void CreateItemsBeforeMenu(int i)
        {
            _breadcrumbVisibleItems[i].Breadcrumb = _breadcrumbs[i];
            _breadcrumbVisibleItems[i].ItemType = i == 0 ? ItemType.First : ItemType.Middle;
            _breadcrumbVisibleItems[i].ItemType = i == 0 && _breadcrumbs.Count == 1 ? ItemType.Single : _breadcrumbVisibleItems[i].ItemType;
            _breadcrumbVisibleItems[i].ItemType = i == _breadcrumbs.Count - 1 && _breadcrumbs.Count > 1 ? ItemType.Last : _breadcrumbVisibleItems[i].ItemType;
            _breadcrumbVisibleItems[i].Title = _breadcrumbs[i].Title;
            _breadcrumbVisibleItems[i].onClick.RemoveAllListeners();
            _breadcrumbVisibleItems[i].onClick.AddListener(() => OnBreadcrumbClick(_breadcrumbs[i]));
            _breadcrumbVisibleItems[i].Initialize();
        }

        private void OnThreeDotsClick()
        {
            int menuIndex = VisibleItems / 2;
            menuIndex = VisibleItems % 2 == 0 ? menuIndex - 1 : menuIndex;
            var count = _breadcrumbs.Count - 1 - menuIndex;
            count = VisibleItems == 3 ? count : count - 1;
            _menuItems = _breadcrumbs.GetRange(menuIndex, count);

            _popupMenu.Cleanup(false);

            foreach (var item in _menuItems)
            {
                _popupMenu.AddMenuItem(item.Title, Color.white);
            }

            _popupMenu.InitializeMenu();
            _popupMenuRect.anchoredPosition = _popupMenuDefaultPosition;
            var threeDotsButton = _breadcrumbVisibleItems[menuIndex];
            var position = transform.parent.InverseTransformPoint(threeDotsButton.transform.position);
            _popupMenu.transform.SetParent(transform.parent);
            _popupMenu.transform.localPosition = new Vector3(position.x, position.y + _popupMenuRect.sizeDelta.y / 2f + PopupMenuOffset, 0f);
            _popupMenu.gameObject.ClampOnScreen(_canvasRect);
            _popupMenu.Show();
        }

        private void OnMenuItemPressed(int index)
        {
            OnBreadcrumbClick(_menuItems[index]);
        }

        private void OnMenuDismissed()
        {
            _popupMenu.transform.SetParent(transform);
        }

        private void OnBreadcrumbClick(IBreadcrumb breadcrumb)
        {
            try
            {
                var index = Mathf.Min(_breadcrumbs.Count - 1, VisibleItems - 1);
                if (_breadcrumbVisibleItems[index].Breadcrumb == breadcrumb)
                {
                    return;
                }

                breadcrumb?.BreadcumbAction();
            }
            catch (Exception ex)
            {
                CrashReporter.LogHandledException(ex);
            }
        }
    }
}
