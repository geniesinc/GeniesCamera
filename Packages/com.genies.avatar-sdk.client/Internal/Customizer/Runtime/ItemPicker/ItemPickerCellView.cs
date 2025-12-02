using System;
using Genies.UI;
using Genies.UIFramework;
using UnityEngine;
using UnityEngine.Serialization;

namespace Genies.Customization.Framework.ItemPicker
{
    public enum ItemCellState
    {
        NotInitialized,
        Initialized,
    }

    public abstract class ItemPickerCellView : MonoBehaviour
    {
        [SerializeField]
        protected GeniesButton _button;
        [FormerlySerializedAs("_downloadSpinner")]
        [SerializeField]
        protected GameObject _processingBadge;
        [SerializeField]
        protected GameObject _processingBackground;
        [SerializeField]
        protected GameObject _selectedView;
        [SerializeField]
        protected GameObject _initializedView;
        [SerializeField]
        protected GameObject _placeHolder;
        [SerializeField]
        protected DebuggingAssetLabel _assetLabel;

        [SerializeField]
        [Header("[CanBeNull] it shows an icon representation if the asset is a NFT")]
        protected GameObject _nftBadge;
        [Space(10)]

        //Index of the cell
        public int Index = -1;
        protected ItemPickerCellData _data;

        public void Initialize(ItemPickerCellData cellData)
        {
            _data = cellData;
            _button.onClick.RemoveAllListeners();

            if (cellData != null)
            {
                _button.onClick.AddListener(() => cellData.OnClicked?.Invoke());
            }

            ToggleProcessingBadge(false);
            OnInitialize();
        }


        protected abstract void OnInitialize();

        public void SetDebuggingAssetLabel(string label)
        {
            if (_assetLabel)
            {
                _assetLabel.SetLabel(label);
            }
        }

        public void ClearDebuggingAssetLabel()
        {
            if (_assetLabel)
            {
                _assetLabel.SetLabel(null);
            }
        }

        public void Dispose()
        {
            _button.onClick.RemoveAllListeners();
            _selectedView.SetActive(false);
            OnDispose();
        }

        protected abstract void OnDispose();

        public void ToggleSelected(bool isSelected)
        {
            _button.SetButtonSelected(isSelected);
            _selectedView.SetActive(isSelected);
            OnSelectionChanged(isSelected);
        }

        protected abstract void OnSelectionChanged(bool isSelected);

        public void ToggleProcessingBadge(bool isProcessing)
        {
            if (_processingBadge != null)
            {
                _processingBadge.SetActive(isProcessing);
            }

            if (_processingBackground != null)
            {
                _processingBackground.SetActive(isProcessing);
            }
        }

        public void SetState(ItemCellState state)
        {
            switch (state)
            {
                case ItemCellState.NotInitialized:
                    _button.enabled = false;
                    _selectedView.SetActive(false);
                    _placeHolder.SetActive(true);
                    _initializedView.SetActive(false);
                    SetNftBadgeActive(false);
                    break;
                case ItemCellState.Initialized:
                    _button.enabled = true;
                    _placeHolder.SetActive(false);
                    _initializedView.SetActive(true);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(state), state, null);
            }
        }

        /// <summary>
        /// Access the nft serialized property only if we have and set as active or not
        /// </summary>
        /// <param name="isActive">enable/disable the gameObject</param>
        public void SetNftBadgeActive(bool isActive)
        {
            if (_nftBadge)
            {
                _nftBadge.SetActive(isActive);
            }
        }
    }
}
