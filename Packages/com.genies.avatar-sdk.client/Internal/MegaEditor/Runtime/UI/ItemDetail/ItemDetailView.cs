using System;
using Genies.UIFramework;
using Genies.UI.Widgets;
using Genies.Utilities.Internal;
using TMPro;
using UnityEngine;

namespace Genies.Customization.MegaEditor
{
    public class ItemDetailView : MonoBehaviour
    {
        [SerializeField]
        private PopupWidget _popupWidget;
        [SerializeField]
        private GeniesButton _backButton;
        [SerializeField]
        private GeniesButton _multiOptionButton;
        [SerializeField]
        private OutlineButton _createLookButton;
        [SerializeField]
        private ItemDetailViewIcon _thumb;
        [SerializeField]
        private GameObject _descriptionPanel;
        [SerializeField]
        private GameObject _datePanel;
        [SerializeField]
        private RectTransform _authorPanel;
        [SerializeField]
        private TextMeshProUGUI _author;
        [SerializeField]
        private RectTransform _ownerPanel;
        [SerializeField]
        private TextMeshProUGUI _owner;
        [SerializeField]
        private TextMeshProUGUI _title;
        [SerializeField]
        private TextMeshProUGUI _description;
        [SerializeField]
        private TextMeshProUGUI _descriptionTitle;
        [SerializeField]
        private TextMeshProUGUI _created;


        [SerializeField]
        private RectTransform _content;
        [SerializeField]
        private RectTransform _nftInfoPanel;

        public event Action BackButtonClicked;
        public event Action MultiOptionButtonClicked;
        public event Action CreateLookClicked;

        private ItemDetailData _data;

        private enum DetailsBehavior
        {
            CreatedWearable,
            StaticNft
        }
        private void Awake()
        {
            _multiOptionButton.onClick.AddListener(()=> MultiOptionButtonClicked?.Invoke());
            _backButton.onClick.AddListener(()=> BackButtonClicked?.Invoke());
            _createLookButton.OnClick.AddListener(()=> CreateLookClicked?.Invoke());
        }

        public void SetActive(bool isActive)
        {
            if (isActive)
            {
                //weird behavior to enable and dispatch the animation, so we call two times
                _popupWidget?.gameObject.SetActive(true);
                _popupWidget?.Show();
                _popupWidget?.Show();
                _thumb?.SetState(ItemDetailViewIcon.ItemState.NotInitialized);
            }
            else
            {
                _popupWidget?.Hide();
            }
        }

        public void SetupItem(string assetId, ItemDetailData data)
        {
            _data = data;

            SetComponentBehavior(data.isNft ? DetailsBehavior.StaticNft : DetailsBehavior.CreatedWearable);

            _thumb.SetDebuggingAssetLabel(assetId);
            _thumb.thumbnail.sprite = data.thumbnail;
            _thumb.SetState(ItemDetailViewIcon.ItemState.Initialized);

            if (string.IsNullOrEmpty(_data.authorName))
            {
                _authorPanel.gameObject.SetActive(false);
            }
            else
            {
                _authorPanel.gameObject.SetActive(true);
                _author.text = "@"+_data.authorName;
            }

            if (string.IsNullOrEmpty(_data.ownerName))
            {
                _ownerPanel.gameObject.SetActive(false);
            }
            else
            {
                _ownerPanel.gameObject.SetActive(true);
                _owner.text = "@"+_data.ownerName;
            }

            if (string.IsNullOrEmpty(_data.assetId) && string.IsNullOrEmpty(_data.description))
            {
                _title.text = string.Empty;
                _descriptionPanel.gameObject.SetActive(false);
                _descriptionTitle.gameObject.SetActive(false);
                _datePanel.gameObject.SetActive(true);
                DateTimeOffset time = FormattingUtils.ConvertDecimalEpochToDate(_data.created);
                _created.text = "created " + time.DateTime.ToString("MM/dd/yyyy");
            }
            else
            {
                _datePanel.gameObject.SetActive(false);
                _title.text = _data.assetId;

                //check if theres description
                if (string.IsNullOrEmpty(_data.description))
                {
                    _descriptionPanel.gameObject.SetActive(false);
                    _descriptionTitle.gameObject.SetActive(false);
                }
                else
                {
                    _descriptionPanel.gameObject.SetActive(true);
                    _descriptionTitle.gameObject.SetActive(true);
                    _description.text = _data.description;
                }
            }

            _thumb.SetNftBadgeActive(data.isNft && !data.isEditable);
        }

        public void Dispose()
        {
            //TODO
        }

        private void SetComponentBehavior(DetailsBehavior behavior)
        {
            // change the height size of the content and enable extra info
            switch (behavior)
            {
                case DetailsBehavior.CreatedWearable:
                    _content.sizeDelta = new Vector2(0, 800);
                    _nftInfoPanel.gameObject.SetActive(false);
                    break;
                case DetailsBehavior.StaticNft:
                    _content.sizeDelta = new Vector2(0, 1500);
                    _nftInfoPanel.gameObject.SetActive(true);
                    break;
            }
        }
    }

    [Serializable]
    public struct ItemDetailData
    {
        public string assetId;
        public Sprite thumbnail;
        public string authorName;
        public string ownerName;
        public string description;
        public decimal? created;
        public bool isNft;
        public bool isEditable;
    }
}
