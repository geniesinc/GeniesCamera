using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Genies.Customization.Framework.ItemPicker;
using Genies.UI.Animations;
using Genies.UIFramework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Genies.Customization.MegaEditor
{
    public class FilteredGallery : MonoBehaviour
    {
        [SerializeField]
        private FilterBar _filterBar;

        [SerializeField]
        private ExpandableGalleryItemPicker _galleryItemPicker;

        [Header("Actions Bar")]
        [SerializeField]
        private Image _closeButtonImage;
        [SerializeField]
        private GeniesButton _closeButton;

        [SerializeField]
        private Image _acceptButtonImage;
        [SerializeField]
        private Button _acceptButton;
        public Button AcceptButton => _acceptButton;

        [SerializeField]
        private TextMeshProUGUI _text;

        public event Action<int,string> FilterSelected;
        public event Action CloseGalleryRequested;
        public event Action AcceptButtonSelected;
        public event Action OnItemSelected;

        public void OnEnable()
        {
            _galleryItemPicker.OnItemSelected += ItemSelected;
        }

        public void OnDisable()
        {
            _galleryItemPicker.OnItemSelected -= ItemSelected;
        }

        public void SetFilterOptions(IReadOnlyList<FilterBarOption> filterList, int selectedIndex = 0)
        {
            var options = new List<FilterBarOption>();

            for (var i = 0; i < filterList.Count; i++)
            {
                var filter = filterList[i];
                var index = i;

                var filterBar = new FilterBarOption
                {
                    displayName = filter.displayName,
                    filterId = filter.filterId,
                    countDisplay = filter.countDisplay,
                    onClicked = () => SelectFilter(index, filter.filterId),
                };

                options.Add(filterBar);
            }

            _filterBar.SetOptions(options, selectedIndex);
        }

        /// <summary>
        ///  Enable/Disable the filter bar component
        /// </summary>
        /// <param name="isActive"> true to enable, false to disable the entire component</param>
        public void SetFilterBarActive(bool isActive)
        {
            _filterBar.gameObject.SetActive(isActive);
        }

        private void OnHidden()
        {
            CloseGalleryRequested?.Invoke();
        }

        private void SelectFilter(int index, string targetNode)
        {
            FilterSelected?.Invoke(index, targetNode);
            _filterBar.SetSelected(index);
        }

        public void Dispose()
        {
            _galleryItemPicker.Hidden -= OnHidden;
            _filterBar.Dispose();
        }

        public void Show(IItemPickerDataSource dataSource)
        {
            if (_closeButton != null)
            {
                _closeButton.onClick.RemoveAllListeners();
                _closeButton.onClick.AddListener(()=> CloseGalleryRequested?.Invoke());
            }

            if (_acceptButton != null)
            {
                _acceptButton.onClick.RemoveAllListeners();
                _acceptButton.onClick.AddListener(()=> AcceptButtonSelected?.Invoke());
            }

            _galleryItemPicker.Hidden += OnHidden;


            _galleryItemPicker.Show(dataSource);
            AnimateActionBar(1);
        }

        public void Hide()
        {
            _galleryItemPicker.Hide();
            AnimateActionBar(0);
        }

        public async UniTask RefreshData()
        {
            await _galleryItemPicker.RefreshData();
        }

        private void AnimateActionBar(float targetAlpha)
        {
            if (_closeButton != null)
            {
                var closeButtonTargetColor = _closeButtonImage.color;
                closeButtonTargetColor.a = targetAlpha;
                _closeButtonImage.SpringColor(closeButtonTargetColor, SpringPhysics.Presets.Smooth).Play();
            }

            if (_acceptButton != null)
            {
                var acceptButtonTargetColor = _acceptButtonImage.color;
                acceptButtonTargetColor.a = targetAlpha;
                _acceptButtonImage.SpringColor(acceptButtonTargetColor, SpringPhysics.Presets.Smooth).Play();
            }

            if (_text != null)
            {
                var textTargetColor = _text.color;
                textTargetColor.a = targetAlpha;
                _text.SpringColor(textTargetColor, SpringPhysics.Presets.Smooth).Play();
            }
        }

        private void ItemSelected()
        {
            OnItemSelected?.Invoke();
        }
    }
}
