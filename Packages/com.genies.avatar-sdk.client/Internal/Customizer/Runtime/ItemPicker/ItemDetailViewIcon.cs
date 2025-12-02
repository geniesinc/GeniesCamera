using Genies.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Genies.Customization.MegaEditor
{
    public class ItemDetailViewIcon : MonoBehaviour
    {
        public enum ItemState
        {
            NotInitialized,
            Initialized,
        }

        public Image thumbnail;

        [SerializeField]
        private GameObject _thumbContainer;
        [SerializeField]
        private GameObject _processingBadge;
        [SerializeField]
        private GameObject _placeHolder;
        [SerializeField]
        private DebuggingAssetLabel _assetLabel;
        [SerializeField]
        [Header("[CanBeNull] it shows an icon representation if the asset is a NFT")]
        protected GameObject _nftBadge;

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

        public void SetState(ItemState state)
        {
            switch (state)
            {
                case ItemState.NotInitialized:
                    ClearDebuggingAssetLabel();
                    _processingBadge.SetActive(true);
                    _placeHolder.SetActive(true);
                    _thumbContainer.SetActive(false);
                    SetNftBadgeActive(false);
                    break;
                case ItemState.Initialized:
                    _processingBadge.SetActive(false);
                    _placeHolder.SetActive(false);
                    _thumbContainer.SetActive(true);
                    break;
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
