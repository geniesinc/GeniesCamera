using Cysharp.Threading.Tasks;
using Genies.Avatars.Sdk;
using Genies.Naf;
using UnityEngine;

namespace Genies.Sdk
{
    internal sealed partial class CoreSdk
    {
        public class Avatar
        {
            private CoreSdk Parent { get; }

            private Avatar() { }

            internal Avatar(CoreSdk parent)
            {
                Parent = parent;
            }
            /// <summary>
            /// Loads a default avatar with optional configuration.
            /// </summary>
            /// <param name="avatarName">Optional name for the avatar GameObject.</param>
            /// <param name="parent">Optional parent transform for the avatar.</param>
            /// <param name="playerAnimationController">Optional animation controller to apply to the avatar.</param>
            /// <returns>A ManagedAvatar instance, or null if loading failed.</returns>
            public async UniTask<ManagedAvatar> LoadDefaultAvatarAsync(
                string avatarName,
                Transform parent,
                RuntimeAnimatorController playerAnimationController)
            {
                if (await Parent.InitializeAsync() is false)
                {
                    return null;
                }

                var geniesAvatar = await GeniesAvatarsSdk.LoadAvatarControllerWithClassDefinition(new AvatarDefinition(), parent);
                return InstantiateAndConfigure(geniesAvatar, avatarName, playerAnimationController);
            }

            /// <summary>
            /// Loads the authenticated user's avatar with optional configuration. Falls back to default avatar if user is not logged in.
            /// </summary>
            /// <param name="avatarName">Optional name for the avatar GameObject.</param>
            /// <param name="parent">Optional parent transform for the avatar.</param>
            /// <param name="playerAnimationController">Optional animation controller to apply to the avatar.</param>
            /// <returns>A ManagedAvatar instance, or null if loading failed.</returns>
            public async UniTask<ManagedAvatar> LoadUserAvatarAsync(
                string avatarName,
                Transform parent,
                RuntimeAnimatorController playerAnimationController)
            {
                if (await Parent.InitializeAsync() is false)
                {
                    return null;
                }

                if (Parent.LoginApi.IsLoggedIn is false)
                {
                    Debug.LogWarning("User is not logged in. Loading default avatar instead.");
                    await LoadDefaultAvatarAsync(avatarName, parent, playerAnimationController);
                    return null;
                }

                var geniesAvatar = await GeniesAvatarsSdk.LoadUserAvatarController(parent);
                return InstantiateAndConfigure(geniesAvatar, avatarName, playerAnimationController);
            }

            /// <summary>
            /// Instantiates and configures a ManagedAvatar from a GeniesAvatar instance.
            /// </summary>
            /// <param name="geniesAvatar">The GeniesAvatar instance to wrap.</param>
            /// <param name="avatarName">Optional name for the avatar GameObject.</param>
            /// <param name="playerAnimationController">Optional animation controller to apply to the avatar.</param>
            /// <returns>A configured ManagedAvatar instance, or null if the input GeniesAvatar is null.</returns>
            private ManagedAvatar InstantiateAndConfigure(GeniesAvatar geniesAvatar, string avatarName, RuntimeAnimatorController playerAnimationController)
            {
                if (geniesAvatar is null) { return null; }

                var avatarInstance = new ManagedAvatar(geniesAvatar);

                if (string.IsNullOrWhiteSpace(avatarName) is false)
                {
                    avatarInstance.Root.name = avatarName;
                }

                if (playerAnimationController != null)
                {
                    avatarInstance.SetAnimatorController(playerAnimationController);
                }

                return avatarInstance;
            }
        }
    }
}
