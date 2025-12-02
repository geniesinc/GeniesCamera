using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Genies.DataRepositoryFramework;
using UnityEngine;
using UnityEngine.Serialization;
using Avatar = Genies.Services.Model.Avatar;
using Newtonsoft.Json;

namespace Genies.Avatars.Services
{
    /// <summary>
    /// Local implementation of <see cref="IAvatarService"/> that stores avatar data on the local disk.
    /// This service is useful for offline scenarios or testing environments where remote API access is not available.
    /// </summary>
    public class LocalAvatarService : IAvatarService
    {
        private readonly LocalDiskDataRepository<LocalAvatar> _storage;
        private Avatar _loadedAvatar;

        /// <summary>
        /// Gets or sets the currently loaded avatar, automatically handling definition deserialization and error recovery.
        /// </summary>
        public Avatar LoadedAvatar
        {
            get => _loadedAvatar;
            private set => _loadedAvatar = value;
        }

        /// <summary>
        /// Local representation of an avatar stored on disk.
        /// </summary>
        [Serializable]
        private struct LocalAvatar
        {
            /// <summary>
            /// Unique identifier for the avatar.
            /// </summary>
            [FormerlySerializedAs("id")] public string Id;

            /// <summary>
            /// The unified avatar definition containing appearance data.
            /// </summary>
            [FormerlySerializedAs("definition")] public Naf.AvatarDefinition Definition;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LocalAvatarService"/> class.
        /// Sets up local disk storage for avatar data.
        /// </summary>
        public LocalAvatarService()
        {
            _storage = new LocalDiskDataRepository<LocalAvatar>("UnifiedAvatars", avatar => avatar.Id);
        }

        private async UniTask<string> GenerateValidGuid()
        {
            var guid = Guid.NewGuid().ToString();
            var currentIds = await _storage.GetIdsAsync();

            while (currentIds.Contains(guid))
            {
                guid = Guid.NewGuid().ToString();
            }

            return guid;
        }

        public async UniTask<bool> CreateAvatarAsync(string gender)
        {
            var definition = NafAvatarExtensions.DefaultDefinition();

            var newAvatar = new LocalAvatar() { Id = await GenerateValidGuid(), Definition = definition };

            var createdDataRecord = await _storage.CreateAsync(newAvatar);
            if (createdDataRecord.Definition == null)
            {
                Debug.LogError("Create Data Record Failed");
                return false;
            }

            // Set the loaded avatar
            LoadedAvatar = new Avatar(createdDataRecord.Id, null, null, definition: JsonConvert.SerializeObject(createdDataRecord.Definition));

            return true;
        }

        public async UniTask<bool> CreateAvatarAsync(Naf.AvatarDefinition avatarDefinition)
        {
            var newAvatar = new LocalAvatar() { Id = await GenerateValidGuid(), Definition = avatarDefinition };

            var createdDataRecord = await _storage.CreateAsync(newAvatar);
            
            // Set the loaded avatar
            LoadedAvatar = new Avatar(createdDataRecord.Id, null, null, definition: JsonConvert.SerializeObject(createdDataRecord.Definition));
            
            return true;
        }

        public async UniTask<Naf.AvatarDefinition> GetAvatarDefinitionAsync()
        {
            var ids = await _storage.GetIdsAsync();
            if (ids == null || ids.Count == 0)
            {
                return null;
            }

            var avatarRecord = await _storage.GetByIdAsync(ids[0]);
            
            // Set the loaded avatar
            LoadedAvatar = new Avatar(avatarRecord.Id, null, null, definition: JsonConvert.SerializeObject(avatarRecord.Definition));
            
            return avatarRecord.Definition;
        }

        public UniTask<Naf.AvatarDefinition> GetUserAvatarDefinitionAsync(string userId)
        {
            throw new NotImplementedException();
        }

        public UniTask<List<Avatar>> GetUserAvatarsAsync(string userId)
        {
                throw new NotImplementedException();
        }

        public async UniTask UpdateAvatarAsync(Naf.AvatarDefinition avatarDefinition)
        {
            var ids = await _storage.GetIdsAsync();
            if (ids == null || ids.Count == 0)
            {
                await CreateAvatarAsync(avatarDefinition);
                return;
            }

            var updatedAvatar = new LocalAvatar() { Id = ids[0], Definition = avatarDefinition };
            await _storage.UpdateAsync(updatedAvatar);
            
            // Set the loaded avatar
            LoadedAvatar = new Avatar(updatedAvatar.Id, null, null, definition: JsonConvert.SerializeObject(updatedAvatar.Definition));
        }

        public async UniTask<Naf.AvatarDefinition> GetOrCreateAvatarAsync(string bodyType)
        {
            var ids = await _storage.GetIdsAsync();
            if (ids == null || ids.Count == 0)
            {
                var definition = new Naf.AvatarDefinition();
                await CreateAvatarAsync(definition);
            }

            return await GetAvatarDefinitionAsync();
        }

        public async UniTask<string> UploadAvatarImageAsync(byte[] imageData, string avatarId)
        {
            // TODO: Implement avatar image upload functionality
            // This method should handle uploading avatar images to local storage or cloud service
            await UniTask.CompletedTask;
            return string.Empty;
        }
    }
}
