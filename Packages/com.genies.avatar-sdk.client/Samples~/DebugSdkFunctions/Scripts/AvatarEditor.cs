using System;
using System.Collections;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Genies.Sdk.Samples.DebugSdkFunctions
{
    internal class AvatarEditor : IDisposable
    {
        private const string StatusOpen = "Editor is OPEN";
        private const string StatusClosed = "Editor is CLOSED";

        private GameObject AttachedGameObject { get; set; }

        public AvatarEditor(GameObject attachedGameObject)
        {
            if (attachedGameObject == null)
            {
                throw new ArgumentNullException(nameof(attachedGameObject), "Must provide a valid game object.");
            }

            AttachedGameObject = attachedGameObject;

            AvatarSdk.Events.AvatarEditorOpened += OnAvatarEditorOpened;
            AvatarSdk.Events.AvatarEditorClosed += OnAvatarEditorClosed;

            UpdateStateDisplay();
        }

        public void Dispose()
        {
            AvatarSdk.Events.AvatarEditorOpened -= OnAvatarEditorOpened;
            AvatarSdk.Events.AvatarEditorClosed -= OnAvatarEditorClosed;

            DestroyStateDisplayComponents();
        }

        public void UpdateStateDisplay(ManagedAvatarComponent currentAvatar = null, Camera camera = null)
        {
            if (AttachedGameObject == null) { return; }

            DestroyStateDisplayComponents();

            if (AvatarSdk.IsAvatarEditorOpen)
            {
                AttachedGameObject.AddComponent<EditorOpenedDisplay>();
            }
            else
            {
                var closedComponent = AttachedGameObject.AddComponent<EditorClosedDisplay>();
                closedComponent.CurrentAvatar = currentAvatar;
                closedComponent.Camera = camera;
            }

#if UNITY_EDITOR
            EditorUtility.SetDirty(AttachedGameObject);
#endif
        }

        private void OnAvatarEditorOpened()
        {
            UpdateStateDisplay();
        }

        private void OnAvatarEditorClosed()
        {
            UpdateStateDisplay();
        }

        private void DestroyStateDisplayComponents()
        {
            if (AttachedGameObject == null) { return; }

            foreach (var component in AttachedGameObject.GetComponents<IEditorStateComponent>())
            {
                if (component is MonoBehaviour destroyable)
                {
                    GameObject.Destroy(destroyable);
                }
            }

#if UNITY_EDITOR
            EditorUtility.SetDirty(AttachedGameObject);
#endif
        }

        // ==================================================
        // Read-Only State Display Components
        // ==================================================

        private interface IEditorStateComponent { }

        private class EditorOpenedDisplay : MonoBehaviour, IEditorStateComponent
        {
            [Header("Avatar Editor State (Read-Only)")]
            [SerializeField] private string _status;

            private IEnumerator Start()
            {
                _ = _status;

                var waitForSeconds = new WaitForSeconds(0.5f);
                while (true)
                {
                    _status = StatusOpen;
                    yield return waitForSeconds;
                }
            }
        }

        private class EditorClosedDisplay : MonoBehaviour, IEditorStateComponent
        {
            [Header("Avatar Editor State (Read-Only)")]
            [SerializeField] private string _status;
            [SerializeField] private ManagedAvatarComponent _currentAvatar;
            [SerializeField] private Camera _camera;

            public ManagedAvatarComponent CurrentAvatar { get; set; }
            public Camera Camera { get; set; }

            private IEnumerator Start()
            {
                _ = _status;

                var waitForSeconds = new WaitForSeconds(0.5f);
                while (true)
                {
                    _status = StatusClosed;
                    _currentAvatar = CurrentAvatar;
                    _camera = Camera;
                    yield return waitForSeconds;
                }
            }
        }
    }
}

