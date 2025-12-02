using System;
using Genies.UIFramework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Genies.Customization.Framework.Navigation
{
    [Serializable]
    public class NavBarNodeButtonData
    {
        public string displayName;
        public Sprite icon;
        public NavBarNodeButton overridePrefab;
        public Action clickCommand;
        public bool showNotification;
        public bool isSelected;
    }

    /// <summary>
    /// A button that sits specifically in the customizer navigation bar
    /// </summary>
    public class NavBarNodeButton : GeniesButton
    {
        [SerializeField]
        private TextMeshProUGUI _text;

        [SerializeField]
        private Image _icon;

        [SerializeField]
        private GameObject _notificationObj;

        private Action _command;

        public void Initialize(NavBarNodeButtonData nodeButtonData)
        {
            _text.text = nodeButtonData.displayName;
            _icon.sprite = nodeButtonData.icon;
            if (_icon.sprite == null)
            {
                _icon.enabled = false;
                _text.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -15.5f);
            }
            else
            {
                _icon.enabled = true;
                _text.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -28f);
            }
            _command = nodeButtonData.clickCommand;
            _notificationObj.SetActive(nodeButtonData.showNotification);

            onClick.AddListener(InvokeCommand);
        }

        public void Dispose()
        {
            onClick.RemoveListener(InvokeCommand);
        }

        private void InvokeCommand()
        {
            _command?.Invoke();
        }
    }
}
