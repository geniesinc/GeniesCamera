using System.Runtime.CompilerServices;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Serialization;

[assembly: InternalsVisibleTo("Genies.Multiplayer.Sdk")]

namespace Genies.Avatars.Sdk
{
    /// <summary>
    /// A Unity Monobehaviour that can be dropped onto any gameobject to create the User Avatar at Start.
    /// Will load a default avatar if you are not logged in.
    /// </summary>
#if GENIES_SDK && !GENIES_INTERNAL
    internal sealed class CreateUserAvatar : MonoBehaviour
#else
    public sealed class CreateUserAvatar : MonoBehaviour
#endif
    {
        [FormerlySerializedAs("animatorController")] [SerializeField]
        private RuntimeAnimatorController _animatorController;
        [FormerlySerializedAs("autoSpawn")] [SerializeField]
        private bool _autoSpawn = true;

        public bool AutoSpawn
        {
            get => _autoSpawn;
            set => _autoSpawn = value;
        }

        public bool DidSpawn { get; private set; }

        private async void Start()
        {
            if (AutoSpawn)
            {
                await SpawnAvatarAsync();
            }
        }

        public async UniTask<GeniesAvatar> SpawnAvatarAsync()
        {
            var avatar = await GeniesAvatarsSdk.LoadUserAvatarAsync("User Avatar", this.transform, _animatorController, false);
            DidSpawn = true;
            return avatar;
        }
    }
}
