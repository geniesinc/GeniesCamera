using Genies.Customization.Framework.Actions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Genies.Looks.MultiOptionPopup.Scripts
{
    [RequireComponent(typeof(Button))]
    public class MultiOptionButton : MonoBehaviour
    {
        private ActionDrawerOption _option;
        [SerializeField] protected TextMeshProUGUI text;
        [SerializeField] protected Image icon;

        public Button button;
        // private GeniesButton _button; // TODO: create subclass on genies button.

        private Color _disabledColor = new Color(.3f, .3f, .3f, 1f);
        private Color _enabledColor = new Color(10.0f / 255.0f, 132.0f / 255.0f, 1, 1);

        public void Initialize(ActionDrawerOption option)
        {
            // Initialize button, text and cache option.
            _option = option;
            text.text = option.displayName;
            // invoke Action on click
            button.onClick.AddListener(option.onClick.Invoke);

            if (icon != null)
            {
                icon.sprite = option.icon;
            }
        }

        public void SetColors(Color enabledColor, Color disabledColor)
        {
            _disabledColor = disabledColor;
            _enabledColor = enabledColor;
        }

        public void UpdateButton()
        {
            var value = _option.getOptionEnabled.Invoke();
            text.color = value ? text.color = _enabledColor : text.color = _disabledColor;
            button.interactable = value;
            if (icon != null)
            {
                icon.color = value ? text.color = _enabledColor : text.color = _disabledColor;
            }
        }
    }
}
