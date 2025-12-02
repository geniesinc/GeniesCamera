using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;

namespace Genies.ABTesting
{
    public interface IABTestingService
    {
        bool CheckFeatureFlag(string flagId);
        public T GetFromConfig<T>(string configName, string key, T defaultValue = default);
        public T GetObjectFromConfig<T>(string configName, string key);
        public void GetAllKeysFromConfig(string configName, List<string> keys);
        public UniTask<bool> AddUserForSegment(string segmentName, string userId);
        public UniTask<bool> SetUser(string userId);

        public bool IsInitialized { get; }

        public Dictionary<string, string> GetMappingFromConfig(string configName)
        {

            var tempKeys = new List<string>();

            //Get all keys
            GetAllKeysFromConfig(configName, tempKeys);

            //Map each key to it's value
            var dynamicConfig = tempKeys.ToDictionary(keySelector: key => key, elementSelector: key => GetFromConfig<string>(configName, key));

            return dynamicConfig;
        }

    }

    /// <summary>
    /// Initialization will be different based on the service.
    /// </summary>
    /// <typeparam name="TModel"> The configuration model for the service </typeparam>
    public interface IABTestingService<TModel> : IABTestingService
    {
        UniTask<bool> Initialize(TModel configurationModel);
    }
}
