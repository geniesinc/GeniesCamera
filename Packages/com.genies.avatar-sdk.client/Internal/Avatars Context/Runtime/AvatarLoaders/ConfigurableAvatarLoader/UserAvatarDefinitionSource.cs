using Cysharp.Threading.Tasks;
using Genies.Avatars.Services;
using Genies.Naf;
using Genies.ServiceManagement;
using UnityEngine;

namespace Genies.Avatars.Context
{
    public sealed class UserAvatarDefinitionSource : IAvatarDefinitionSource
    {
        public async UniTask<string> GetDefinitionAsync()
        {
            var avatarService = ServiceManager.GetService<IAvatarService>(null);
            if (avatarService is null)
            {
                Debug.LogError("Cannot get the latest avatar definition from the user because the avatar service is not initialized.");
                return null;
            }

            Genies.Naf.AvatarDefinition definition = await avatarService.GetAvatarDefinitionAsync();
            return definition.SerializeDefinition();
        }
    }
}
