using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Genies.Avatars;
using Genies.CloudSave;
using Genies.CrashReporting;
using Genies.Login.Native;
using Genies.S3Service;
using Genies.Refs;
using Genies.Utilities;
using UnityEngine;

namespace Genies.Ugc.CustomPattern
{
    /// <summary>
    /// Uses <see cref="IGeniesS3Service"/> and <see cref="ICloudFeatureSaveService{T}"/> to upload patterns to our backend and sync them
    /// across devices.
    /// </summary>
    public class CustomPatternRemoteLoaderService : ICustomPatternService
    {
        // dependencies
        private readonly IGeniesS3Service _s3Service;
        private readonly ICloudFeatureSaveService<Pattern> _patternCloudSave;
        private readonly ImageLoader _imageLoader;

        public CustomPatternRemoteLoaderService(IGeniesS3Service s3Service, ICloudFeatureSaveService<Pattern> patternCloudSave, ImageLoader imageLoader)
        {
            _s3Service = s3Service;
            _patternCloudSave = patternCloudSave;
            _imageLoader = imageLoader;
        }

        public async UniTask InitializeAsync()
        {
            //By default will be cached in memory.
            await GetAllCustomPatternIdsAsync();
        }

        public async UniTask<bool> DoesCustomPatternExistAsync(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return false;
            }

            if (_patternCloudSave is not null)
            {
                var ids = await GetAllCustomPatternIdsAsync();
                if (ids.Contains(name))
                {
                    return true;
                }
            }

            return AvatarEmbeddedData.TryGetData<Pattern>(name, out _);
        }

        public async UniTask<int> GetCustomPatternsCountAsync()
        {
            return await _patternCloudSave.GetCountAsync();
        }

        public async UniTask<List<string>> GetAllCustomPatternIdsAsync()
        {
            return await _patternCloudSave.GetIdsAsync();
        }

        public async UniTask<Ref<Texture2D>> LoadCustomPatternTextureAsync(string customPatternId)
        {
            if (await DoesCustomPatternExistAsync(customPatternId) == false)
            {
                return default;
            }

            try
            {
                Pattern pattern   = await LoadCustomPatternAsync(customPatternId);
                var textureRemoteUrl = pattern?.TextureRemoteUrl;

                if (textureRemoteUrl == null)
                {
                    return default;
                }

                var result = await _s3Service.DownloadObject(textureRemoteUrl, $"{customPatternId}.jpg");

                if (!result.wasDownloaded)
                {
                    return default;
                }

                string filePath = result.downloadedFilePath;
                Ref<Texture2D> texture = await _imageLoader.LoadImageAsTextureAsync(filePath);
                return texture;
            }
            catch (Exception e)
            {
                CrashReporter.LogHandledException(e);
                return default;
            }
        }

        public async UniTask<Pattern> LoadCustomPatternAsync(string customPatternId)
        {
            try
            {
                if (await DoesCustomPatternExistAsync(customPatternId) == false)
                {
                    return AvatarEmbeddedData.TryGetData(customPatternId, out Pattern pattern) ? pattern : null;
                }

                Pattern record = null;
                if (_patternCloudSave is not null)
                {
                    record = await _patternCloudSave.GetByIdAsync(customPatternId);
                }

                if (record is null)
                {
                    return AvatarEmbeddedData.TryGetData(customPatternId, out Pattern pattern) ? pattern : null;
                }
                else
                {
                    AvatarEmbeddedData.SetData(customPatternId, record);
                }

                return record;
            }
            catch (Exception e)
            {
                CrashReporter.LogHandledException(e);
                return null;
            }
        }

        public async UniTask<string> CreateOrUpdateCustomPatternAsync(Texture2D newPattern, Pattern pattern = null, string customPatternId = null)
        {
            bool isUpdate = customPatternId != null && await DoesCustomPatternExistAsync(customPatternId);

            var userId = await GeniesLoginSdk.GetUserIdAsync();
            if (string.IsNullOrEmpty(customPatternId))
            {
                customPatternId = CustomPatternNameFactory.CreateNewName(userId);
            }

            try
            {
                var bytes           = newPattern.EncodeToJPG();
                var path            = $"Patterns/{customPatternId}.jpg";
                var distributionUrl = await _s3Service.UploadObject(path, bytes);

                if (string.IsNullOrEmpty(distributionUrl))
                {
                    return string.Empty;
                }

                pattern ??= new Pattern();
                pattern.TextureId = customPatternId;
                pattern.TextureRemoteUrl = distributionUrl;

                if (isUpdate)
                {
                    await _patternCloudSave.UpdateAsync(pattern);
                }
                else
                {
                    await _patternCloudSave.CreateAsync(pattern);
                }
            }
            catch (Exception ex)
            {
                CrashReporter.LogHandledException(ex);
                customPatternId = null;
            }

            return customPatternId;
        }

        public async UniTask<bool> DeletePatternAsync(string customPatternId)
        {
            try
            {
                //Delete texture
                var pattern = await LoadCustomPatternAsync(customPatternId);
                _s3Service.DeleteObject(pattern.TextureId);

                //Delete pattern
                await _patternCloudSave.DeleteAsync(customPatternId);

                return true;
            }
            catch (Exception e)
            {
                CrashReporter.LogHandledException(e);
            }

            return false;
        }

        public async UniTask DeleteAllPatternsAsync()
        {
            var ids   = await GetAllCustomPatternIdsAsync();
            var tasks = ids.Select(DeletePatternAsync).ToList();

            await UniTask.WhenAll(tasks);
        }

        public async UniTask<string> DoesCustomPatternFromOtherUser(string patternId)
        {
            string authorUserId = null;
            //check if the userid in the name belongs to my user if
            var myUserId = await GeniesLoginSdk.GetUserIdAsync();
            var isCreatedByMe = patternId.Contains(myUserId);

            if (isCreatedByMe)
            {
                return null;
            }


            authorUserId = CustomPatternNameFactory.GetUserIdFromCustomPatternId(patternId);
            return authorUserId;
        }
        public async UniTask<Ref<Texture2D>> LoadCustomPatternTextureAsync(string userId, string customPatternId)
        {
            var myUserId = await GeniesLoginSdk.GetUserIdAsync();
            //method responsible to download only patterns from different users
            if (myUserId.Equals(userId))
            {
                CrashReporter.LogError($"[{nameof(CustomPatternRemoteLoaderService)}] invalid call, use {nameof(LoadCustomPatternTextureAsync)} instead");
                return default;
            }

            //TODO implement a better way to get the domain
            var domainPath = $"{GeniesUrls.CloudfrontUrl}/users/";
            var path = $"Patterns/{customPatternId}.jpg";
            var remoteURL = $"{domainPath}{userId}/{path}";
            var result = await _s3Service.DownloadObject(remoteURL, $"{customPatternId}.jpg");

            if (!result.wasDownloaded)
            {
                return default;
            }

            string filePath = result.downloadedFilePath;
            Ref<Texture2D> texture = await _imageLoader.LoadImageAsTextureAsync(filePath);
            return texture;
        }
    }
}
