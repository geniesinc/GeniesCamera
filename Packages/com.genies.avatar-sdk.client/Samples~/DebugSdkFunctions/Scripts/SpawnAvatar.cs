using System;
using System.Collections;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace Genies.Sdk.Samples.DebugSdkFunctions
{
    internal class SpawnAvatar : IDisposable
    {
        private GameObject AttachedGameObject { get; set; }

        public SpawnAvatar(GameObject attachedGameObject)
        {
            if (attachedGameObject == null)
            {
                throw new ArgumentNullException(nameof(attachedGameObject), "Must provide a valid game object.");
            }

            AttachedGameObject = attachedGameObject;

            AvatarSdk.Events.UserLoggedIn += UpdateStateDisplay;
            AvatarSdk.Events.UserLoggedOut += UpdateStateDisplay;

            UpdateStateDisplay();
        }

        public void Dispose()
        {
            AvatarSdk.Events.UserLoggedIn -= UpdateStateDisplay;
            AvatarSdk.Events.UserLoggedOut -= UpdateStateDisplay;

            DestroyStateDisplayComponents();
        }

        public void UpdateStateDisplay() => UpdateStateDisplay(null, null);
        public void UpdateStateDisplay(List<ManagedAvatarComponent> spawnedAvatars, ManagedAvatarComponent lastSpawned)
        {
            if (AttachedGameObject == null) { return; }

            DestroyStateDisplayComponents();

            var stateComponent = AttachedGameObject.AddComponent<AvatarStateDisplay>();
            stateComponent.SpawnedAvatars = spawnedAvatars;
            stateComponent.LastSpawned = lastSpawned;
            stateComponent.IsLoggedIn = AvatarSdk.IsLoggedIn;

#if UNITY_EDITOR
            EditorUtility.SetDirty(AttachedGameObject);
#endif
        }

        private void DestroyStateDisplayComponents()
        {
            if (AttachedGameObject == null) { return; }

            foreach (var component in AttachedGameObject.GetComponents<IAvatarStateComponent>())
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
        // Read-Only State Display Component
        // ==================================================

        private interface IAvatarStateComponent { }

        private class AvatarStateDisplay : MonoBehaviour, IAvatarStateComponent
        {
            [Header("Avatar Spawn State (Read-Only)")]
            [SerializeField] private List<ManagedAvatarComponent> _spawnedAvatars;
            [SerializeField] private ManagedAvatarComponent _lastSpawned;
            [SerializeField] private bool _isLoggedIn;

            public List<ManagedAvatarComponent> SpawnedAvatars { get; set; }
            public ManagedAvatarComponent LastSpawned { get; set; }
            public bool IsLoggedIn { get; set; }

            private IEnumerator Start()
            {
                var waitForSeconds = new WaitForSeconds(0.5f);
                while (true)
                {
                    _spawnedAvatars = SpawnedAvatars;
                    _lastSpawned = LastSpawned;
                    _isLoggedIn = IsLoggedIn;
                    yield return waitForSeconds;
                }
            }
        }
    }
}

