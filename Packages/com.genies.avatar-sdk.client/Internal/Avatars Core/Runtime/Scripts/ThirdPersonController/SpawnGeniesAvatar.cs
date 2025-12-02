using System.Runtime.CompilerServices;
using Cysharp.Threading.Tasks;
using Genies.Avatars.Sdk;
using UnityEngine;

[assembly: InternalsVisibleTo("Genies.Multiplayer.Sdk")]

namespace Genies.Experience.Gameplay
{
    /// <summary>
    /// A Unity Monobehavior that can be dropped onto any gameobject to create the User Avatar at Start.
    /// Will load a default avatar if you are not logged in.
    /// </summary>
#if GENIES_SDK && !GENIES_INTERNAL
    internal sealed class SpawnGeniesAvatar : MonoBehaviour
#else
    public sealed class SpawnGeniesAvatar : MonoBehaviour
#endif
    {
        // inspector
        [SerializeField]
        private RuntimeAnimatorController animatorController;
        public bool autoSpawn = true;

        public bool DidSpawn { get; private set; }

        private GeniesAvatarController _avatarController;

        private async void Start()
        {
            _avatarController = this.GetComponent<GeniesAvatarController>();
            if (autoSpawn)
            {
                await SpawnAvatarAsync();
            }
        }

        public async UniTask<GeniesAvatar> SpawnAvatarAsync()
        {
            var avatar = await GeniesAvatarsSdk.LoadUserAvatarAsync("User Avatar", this.transform, animatorController, false);
            DidSpawn = true;
            var animatorEventBridge = avatar.Root.gameObject.AddComponent<GeniesAnimatorEventBridge>();

            if (_avatarController != null)
            {
                _avatarController.SetAnimatorEventBridge(animatorEventBridge);
            }
            return avatar;
        }
    }
}
