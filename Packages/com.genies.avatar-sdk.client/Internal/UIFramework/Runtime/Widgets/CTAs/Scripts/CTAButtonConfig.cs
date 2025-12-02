using System;
using UnityEngine;

namespace Genies.UI.Widgets
{
    /// <summary>
    /// Config class for a given CTA button.
    /// </summary>
    [Serializable]
    public class CTAButtonConfig
    {
        [SerializeField]
        private GameObject _gameObject;

        [SerializeField]
        private CollapsibleGeniesButton _ctaButton;

        [SerializeField]
        private bool _isSelectable = false;

        [SerializeField]
        private GameObject _selectedView;

        //Set to false if this element shouldn't be allowed to collapse or expand
        [SerializeField]
        private bool _isCollapsible;

        //Set to false if this element shouldn't have a enable/disable behavior if an option is selected
        [SerializeField]
        private bool _isInactiveIfNoOptionSelected = false;

        public GameObject GameObject => _gameObject;
        public CollapsibleGeniesButton CTAButton => _ctaButton;
        public bool IsCollapsible => _isCollapsible;

        public void SetSelected(bool isSelected)
        {
            if (!_isSelectable)
            {
                return;
            }

            _selectedView.SetActive(isSelected);
        }

        public void SetActive(bool isActive)
        {
            if (!_isInactiveIfNoOptionSelected)
            {
                return;
            }

            _selectedView.SetActive(isActive);
            CTAButton.SetButtonEnabled(isActive, isActive);
        }
    }
}
