using Cysharp.Threading.Tasks;
using Genies.CrashReporting;
using Newtonsoft.Json;
using System;
using Genies.ServiceManagement;
using UnityEngine;

namespace Genies.Avatars.Services
{
    public static class AvatarCreatorUtil
    {
        private const string DefaultAvatarDefPath = "DefaultAvatarDefinition";
        private static IAvatarService AvatarService => ServiceManager.Get<IAvatarService>();

        public static async UniTask CreateDefaultAvatar()
        {
            var json = Resources.Load<TextAsset>(DefaultAvatarDefPath).text;
            await CreateAvatar(json);
        }

        public static async UniTask CreateAvatar(string avatarDefinition)
        {
            Naf.AvatarDefinition avatarDef = null;

            try
            {
                avatarDef = JsonConvert.DeserializeObject<Naf.AvatarDefinition>(avatarDefinition);
            }
            catch (AggregateException ae)
            {
                CrashReporter.Log("Cannot load, invalid avatar definition.", LogSeverity.Error);
                CrashReporter.LogHandledException(ae);
                avatarDef = new Naf.AvatarDefinition();
            }
            finally
            {
                var avatarDefInfo = await AvatarService.GetAvatarDefinitionAsync();

                if (avatarDefInfo != null)
                {
                    await AvatarService.UpdateAvatarAsync(avatarDef);
                }
                else
                {
                    await AvatarService.CreateAvatarAsync(avatarDef);
                }
            }
        }

        public static async UniTask<string> GetPersistentAvatarDefinition()
        {
            Naf.AvatarDefinition unifiedDefinition = await AvatarService.GetAvatarDefinitionAsync();
            unifiedDefinition ??= NafAvatarExtensions.DefaultDefinition();
            //AvatarDefinitionFilter.FilterNonPersistentAttributes(unifiedDefinition);
            var persistentDefinition = JsonConvert.SerializeObject(unifiedDefinition);
            return persistentDefinition;
        }

    }
}
