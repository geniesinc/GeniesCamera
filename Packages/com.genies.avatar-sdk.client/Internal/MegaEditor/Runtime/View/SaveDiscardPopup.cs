using System;
using Genies.UI.Widgets;
using Genies.UIFramework;
using UnityEngine;

namespace Genies.MegaEditor
{
    public class SaveDiscardPopup : PopupWidget
    {
        public Action OnSaveClicked;
        public Action OnDiscardClicked;
        public Action OnCloseClicked;

        [SerializeField] private MobileButton saveButton;
        [SerializeField] private MobileButton discardButton;
        [SerializeField] private GeniesButton closeButton;

        public override void OnEnable()
        {
            base.OnEnable();
            saveButton.onClick.AddListener(Save);
            discardButton.onClick.AddListener(Discard);
            closeButton.onClick.AddListener(Close);
        }

        public void OnDisable()
        {
            saveButton.onClick.RemoveListener(Save);
            discardButton.onClick.RemoveListener(Discard);
            closeButton.onClick.RemoveListener(Close);
        }

        private async void Save()
        {
            await HideAsync();
            OnSaveClicked.Invoke();
        }

        private async void Discard()
        {
            await HideAsync();
            OnDiscardClicked.Invoke();
        }

        private async void Close()
        {
            await HideAsync();
            OnCloseClicked.Invoke();
        }
    }
}



