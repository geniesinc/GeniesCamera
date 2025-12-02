using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Genies.Sdk;

namespace Genies.Sdk.Samples.MultipleAvatars
{
    /// <summary>
    /// Manages UI GameObjects visibility based on Avatar Editor state.
    /// Disables specified GameObjects when Avatar Editor opens and re-enables them when it closes.
    /// </summary>
    public class AvatarEditorUIManager : MonoBehaviour
    {
        [Header("GameObjects to Hide During Avatar Editing")]
        [SerializeField] private GameObject _gameObjectToHide1;
        [SerializeField] private GameObject _gameObjectToHide2;
        [SerializeField] private GameObject _gameObjectToHide3;

        private void Awake()
        {
            // Subscribe to Avatar Editor events
            AvatarSdk.Events.AvatarEditorOpened += OnAvatarEditorOpened;
            AvatarSdk.Events.AvatarEditorClosed += OnAvatarEditorClosed;
        }

        private async void Start()
        {
            await UniTask.WaitUntil(() => AvatarSdk.IsLoggedIn);

            // Check if Avatar Editor is already open on start
            if (AvatarSdk.IsAvatarEditorOpen)
            {
                OnAvatarEditorOpened();
            }
        }

        private void OnDestroy()
        {
            // Unsubscribe from events to prevent memory leaks
            AvatarSdk.Events.AvatarEditorOpened -= OnAvatarEditorOpened;
            AvatarSdk.Events.AvatarEditorClosed -= OnAvatarEditorClosed;
        }

        /// <summary>
        /// Called when the Avatar Editor is opened. Disables the specified GameObjects.
        /// </summary>
        private void OnAvatarEditorOpened()
        {
            // Disable the GameObjects
            if (_gameObjectToHide1 != null)
            {
                _gameObjectToHide1.SetActive(false);
            }

            if (_gameObjectToHide2 != null)
            {
                _gameObjectToHide2.SetActive(false);
            }


            if (_gameObjectToHide3 != null)
            {
                _gameObjectToHide3.SetActive(false);
            }
        }

        /// <summary>
        /// Called when the Avatar Editor is closed. Re-enables the specified GameObjects.
        /// </summary>
        private void OnAvatarEditorClosed()
        {
            // Restore the GameObjects to their initial states
            if (_gameObjectToHide1 != null)
            {
                _gameObjectToHide1.SetActive(true);
            }

            if (_gameObjectToHide2 != null)
            {
                _gameObjectToHide2.SetActive(true);
            }

            if (_gameObjectToHide3 != null)
            {
                _gameObjectToHide3.SetActive(true);
            }
        }
    }
}
