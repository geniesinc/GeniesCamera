using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Genies.Sdk.Samples.AvatarStarter
{
    /// <summary>
    /// A Unity Monobehavior that can be dropped onto any gameobject to create the User Avatar at Start.
    /// Will load a default avatar if you are not logged in.
    /// </summary>
    public sealed class CreateGeniesAvatar : MonoBehaviour
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

        public async UniTask<ManagedAvatar> SpawnAvatarAsync()
        {
            var avatar = await AvatarSdk.LoadUserAvatarAsync("User Avatar", this.transform, animatorController);
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
