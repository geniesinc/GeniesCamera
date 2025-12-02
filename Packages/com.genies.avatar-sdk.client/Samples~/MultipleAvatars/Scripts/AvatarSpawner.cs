using System;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;

namespace Genies.Sdk.Samples.MultipleAvatars
{
    /// <summary>
    /// Spawns two avatars from different profile IDs once the user is logged in.
    /// Uses AvatarSdk.LoadFromLocalGameObjectAsync to load each avatar.
    /// </summary>
    public class AvatarSpawner : MonoBehaviour
    {
        [Header("Avatar Spawn Configuration")]
        [Tooltip("Transform positions where the avatars will be spawned")]
        [SerializeField] private Transform _avatar1Transform;
        [SerializeField] private Transform _avatar2Transform;

        [Header("Status")]
        [SerializeField] private bool _hasAttemptedSpawn = false;

        private List<ManagedAvatar> _spawnedAvatars = new List<ManagedAvatar>();

        private string _profileId1 = "ExampleProfile1";
        private string _profileId2 = "ExampleProfile2";

        [SerializeField] private GameObject _loadingSpinner, _selectAvatarText;

        private void Awake()
        {
            // Subscribe to login events
            AvatarSdk.Events.UserLoggedIn += OnUserLoggedIn;
        }

        private void OnDestroy()
        {
            // Unsubscribe from events
            AvatarSdk.Events.UserLoggedIn -= OnUserLoggedIn;

            // Clean up spawned avatars
            CleanupAvatars();
        }

        private void OnUserLoggedIn()
        {
           TrySpawnAsync().Forget();
        }

        private async UniTask TrySpawnAsync()
        {
            if (!_hasAttemptedSpawn)
            {
                await SpawnAvatars();
            }
        }

        /// <summary>
        /// Spawns avatars from different profile IDs at specified transforms
        /// </summary>
        private async UniTask SpawnAvatars()
        {
            _loadingSpinner.SetActive(true);

            _hasAttemptedSpawn = true;

            // Validate transforms are assigned
            if (_avatar1Transform == null || _avatar2Transform == null)
            {
                Debug.LogError("One or more avatar transforms are not assigned! Please assign all transforms in the inspector.");
                return;
            }

            // Clean up any existing avatars first
            CleanupAvatars();

            // Spawn Avatar 1
            var (avatar1, transform1) = await SpawnSingleAvatar(_profileId1, _avatar1Transform);
            if (avatar1 != null)
            {
                _spawnedAvatars.Add(avatar1);
                PositionAvatar(avatar1, transform1);
            }
            else
            {
                Debug.LogError($"Failed to spawn Avatar 1: {_profileId1}");
            }


            // Spawn Avatar 2
            var (avatar2, transform2) = await SpawnSingleAvatar(_profileId2, _avatar2Transform);
            if (avatar2 != null)
            {
                _spawnedAvatars.Add(avatar2);
                PositionAvatar(avatar2, transform2);
            }
            else
            {
                Debug.LogError($"Failed to spawn Avatar 2: {_profileId2}");
            }

            _loadingSpinner.SetActive(false);
            _selectAvatarText.SetActive(true);
        }

        /// <summary>
        /// Spawns a single avatar from a profile ID
        /// </summary>
        private async UniTask<(ManagedAvatar avatar, Transform transform)> SpawnSingleAvatar(
            string profileId, Transform targetTransform)
        {
            try
            {
                // Load avatar from local GameObject using the profile ID
                var managedAvatar = await AvatarSdk.LoadFromLocalGameObjectAsync(profileId);

                if (managedAvatar != null)
                {
                    return (managedAvatar, targetTransform);
                }

                Debug.LogWarning($"Failed to load avatar from profile: {profileId} - Avatar was null");
                return (null, targetTransform);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Exception loading avatar from profile {profileId}: {ex.Message}");
                return (null, targetTransform);
            }
        }

        /// <summary>
        /// Positions and names the spawned avatar at the target transform
        /// </summary>
        private void PositionAvatar(ManagedAvatar avatar, Transform targetTransform)
        {
            if (avatar != null && avatar.Component != null)
            {
                // Set position and rotation
                avatar.Component.transform.SetParent(targetTransform);
                avatar.Component.transform.localPosition = new Vector3(0, -1, 0);
                avatar.Component.transform.localRotation = Quaternion.identity;

                // Add ClickableAvatar component to make it clickable
                var clickableAvatar = avatar.Component.gameObject.GetComponent<ClickableAvatar>();
                if (clickableAvatar == null)
                {
                    var capsule = avatar.Component.gameObject.AddComponent<CapsuleCollider>();
                    capsule.height = 2;
                    capsule.center = new Vector3(0, 0.5f, 0);
                    avatar.Component.gameObject.AddComponent<ClickableAvatar>();
                }

                targetTransform.GetComponent<MeshRenderer>().enabled = false;
                targetTransform.GetComponent<CapsuleCollider>().enabled = false;
            }
        }

        /// <summary>
        /// Cleans up all spawned avatars
        /// </summary>
        private void CleanupAvatars()
        {
            foreach (var avatar in _spawnedAvatars)
            {
                try
                {
                    avatar?.Dispose();
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Error disposing avatar: {ex.Message}");
                }
            }

            _spawnedAvatars.Clear();
        }
    }
}
