using Cysharp.Threading.Tasks;
using Genies.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Genies.Avatars;
using Genies.CrashReporting;
using Genies.ServiceManagement;
using Genies.Refs;
using UnityEngine;

namespace Genies.Ugc.CustomPattern
{
    public class CustomPatternLocalLoaderService : ICustomPatternService
    {
        private readonly string _path = $"{Application.persistentDataPath}//CustomPatterns";
        private string PatternDataPath => $"{_path}//PatternData";

        private const string CustomPatternKey = "custom-pattern";

        public async UniTask<string> CreateOrUpdateCustomPatternAsync(Texture2D newPattern, Pattern pattern = null, string customPatternId = null)
        {
            if (string.IsNullOrEmpty(customPatternId))
            {
                customPatternId = $"{CustomPatternKey}-{DateTime.UtcNow.ToString("yyyy'-'MM'-'dd'T'HH'-'mm'-'ss.fffffffK")}-{Guid.NewGuid().ToString()}";
            }

            try
            {
                if (!Directory.Exists(_path))
                {
                    Directory.CreateDirectory(_path);
                }
                if (!Directory.Exists(PatternDataPath))
                {
                    Directory.CreateDirectory(PatternDataPath);
                }

                var bytes = newPattern.EncodeToJPG();
                await File.WriteAllBytesAsync($"{_path}//{customPatternId}.jpg", bytes);

                pattern ??= new Pattern();
                pattern.TextureId = customPatternId;
                var patternData = System.Text.Encoding.UTF8.GetBytes(JsonUtility.ToJson(pattern));
                await File.WriteAllBytesAsync($"{PatternDataPath}//{customPatternId}.pattern", patternData);

            }
            catch (Exception ex)
            {
                CrashReporter.LogHandledException(ex);
                customPatternId = null;
            }

            return customPatternId;
        }

        public UniTask<bool> DeletePatternAsync(string customPatternId)
        {
            try
            {
                string filepath = $"{_path}//{customPatternId}.jpg";

                if (File.Exists(filepath))
                {
                    File.Delete(filepath);
                    return UniTask.FromResult(true);
                }
            }
            catch (Exception ex)
            {
                CrashReporter.LogHandledException(ex);
            }

            return UniTask.FromResult(false);
        }

        public async UniTask DeleteAllPatternsAsync()
        {

            var ids   = await GetAllCustomPatternIdsAsync();
            var tasks = ids.Select(DeletePatternAsync).ToList();

            await UniTask.WhenAll(tasks);
        }

        public UniTask InitializeAsync()
        {
            // Not necessary
            return UniTask.CompletedTask;
        }

        public UniTask<List<string>> GetAllCustomPatternIdsAsync()
        {
            if (!Directory.Exists(_path))
            {
                return UniTask.FromResult(new List<string>());
            }

            try
            {
                var files = Directory.EnumerateFiles(_path).ToList();

                for (int i = 0; i < files.Count; i++)
                {
                    files[i] = Path.GetFileNameWithoutExtension(files[i]);
                }

                return UniTask.FromResult(files);
            }
            catch (Exception ex)
            {
                CrashReporter.LogHandledException(ex);
            }

            return UniTask.FromResult(new List<string>());

        }

        public UniTask<string> DoesCustomPatternFromOtherUser(string patternId)
        {
            throw new NotImplementedException();
        }

        public UniTask<int> GetCustomPatternsCountAsync()
        {
            try
            {
                var files = Directory.EnumerateFiles(_path).ToList();
                return UniTask.FromResult(files.Count);
            }
            catch (Exception ex)
            {
                CrashReporter.LogHandledException(ex);
            }

            return UniTask.FromResult(0);
        }

        public async UniTask<bool> DoesCustomPatternExistAsync(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return false;
            }

            var ids = await GetAllCustomPatternIdsAsync();
            return ids.Contains(name) || AvatarEmbeddedData.TryGetData<Pattern>(name, out _);
        }

        public async UniTask<Ref<Texture2D>> LoadCustomPatternTextureAsync(string customPatternId)
        {
            if (!Directory.Exists(_path))
            {
                return default;
            }

            try
            {
                string filePath = $"{_path}//{customPatternId}.jpg";
                Ref<Texture2D> texture = await this.GetService<ImageLoader>().LoadImageAsTextureAsync(filePath);
                return texture;
            }
            catch (Exception ex)
            {
                CrashReporter.LogHandledException(ex);
                return default;
            }
        }

        public UniTask<Ref<Texture2D>> LoadCustomPatternTextureAsync(string userId, string customPatternId)
        {
            throw new NotImplementedException();
        }

        public UniTask<Pattern> LoadCustomPatternAsync(string customPatternId)
        {
            Pattern pattern;

            if (!Directory.Exists(_path) || !Directory.Exists(PatternDataPath))
            {
                return UniTask.FromResult(AvatarEmbeddedData.TryGetData(customPatternId, out pattern) ? pattern : null);
            }

            try
            {
                string filePath = $"{PatternDataPath}//{customPatternId}.pattern";
                if (File.Exists(filePath))
                {
                    var data = System.Text.Encoding.UTF8.GetString(File.ReadAllBytes(filePath));
                    pattern = JsonUtility.FromJson<Pattern>(data);

                    if (pattern is null)
                    {
                        return UniTask.FromResult(AvatarEmbeddedData.TryGetData(customPatternId, out pattern)
                            ? pattern
                            : null);
                    }
                    else
                    {
                        AvatarEmbeddedData.SetData(customPatternId, pattern);
                        return UniTask.FromResult(pattern);
                    }
                }
            }
            catch (Exception ex)
            {
                CrashReporter.LogHandledException(ex);
            }

            return UniTask.FromResult(AvatarEmbeddedData.TryGetData(customPatternId, out pattern) ? pattern : null);
        }

    }
}
