using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Genies.UIFramework
{
    public class ConfirmationSlidingPopupWidget : SlidingPopupWidget
    {
        [SerializeField]
        private Button _confirmationButton;

        [SerializeField]
        private TextMeshProUGUI _header;

        [SerializeField]
        private Image _image;

        [SerializeField]
        private Sprite _sprite;

        [SerializeField]
        private TextMeshProUGUI _description;

        private Action _confirmationAction;

        public void Initialize(Action confirmationAction = null)
        {
            _confirmationAction = confirmationAction;
            _image.sprite = _sprite;
        }

        public void Show(string header, string label = "", Action confirmationAction = null)
        {
            this.gameObject.SetActive(true);


            if (confirmationAction != null)
            {
                _confirmationAction = confirmationAction;
            }

            _header.text = header;

            if (string.IsNullOrWhiteSpace(label))
            {
                _description.gameObject.SetActive(false);
            }
            else
            {
                _description.gameObject.SetActive(true);
                _description.text = label;
            }

            Show();
        }

        protected override void AddListeners()
        {
            base.AddListeners();

            if (_cancelButton)
            {
                _confirmationButton.onClick.AddListener(ConfirmationAction);
            }
        }

        protected override void RemoveListeners()
        {
            base.RemoveListeners();

            _confirmationButton.onClick.RemoveListener(ConfirmationAction);
        }

        private void ConfirmationAction()
        {
            _confirmationAction?.Invoke();
            Hide();
        }
    }
}
