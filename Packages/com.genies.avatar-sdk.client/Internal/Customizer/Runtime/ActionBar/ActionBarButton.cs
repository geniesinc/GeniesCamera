using Genies.UIFramework;
using UnityEngine;
using UnityEngine.UI;


namespace Genies.Customization.Framework.Actions
{
    public class ActionBarButton : GeniesButton
    {
        [Header("Action Bar Button Configuration")]
        [SerializeField]
        private Image _icon;
        public Image Icon => _icon;

        [SerializeField]
        private Color _activeColor;

        [SerializeField]
        private Color _inactiveColor;


        public void ToggleActivity(bool isActive)
        {
            _icon.color = isActive ? _activeColor : _inactiveColor;
            SetButtonEnabled(isActive);
        }
    }
}
