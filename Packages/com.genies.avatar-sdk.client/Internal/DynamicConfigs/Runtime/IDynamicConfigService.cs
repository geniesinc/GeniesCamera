using Cysharp.Threading.Tasks;

namespace Genies.Services.DynamicConfigs
{
    public interface IDynamicConfigService
    {
        /// <summary>
        /// It will return a dynamic config object based on the type
        /// </summary>
        /// <param name="configName"></param>
        /// <param name="jsonKey"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        UniTask<T> GetDynamicConfig<T>(string configName, string jsonKey = default);


        public bool IsInitialized { get; }
    }
}
