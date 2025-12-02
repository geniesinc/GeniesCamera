using Cysharp.Threading.Tasks;
using Genies.ABTesting;
using Genies.CrashReporting;
using Genies.ServiceManagement;
using Genies.Services.Auth;

namespace Genies.Services.DynamicConfigs
{
    public class DynamicConfigServiceFromStatsig: IDynamicConfigService
    {
        public bool IsInitialized => _StatsigService != null ? _StatsigService.IsInitialized : true;

        private IABTestingService _StatsigService => this.GetService<IABTestingService>();

        public UniTask<T> GetDynamicConfig<T>(string configName, string jsonKey = default)
        {
            try
            {
                T data = _StatsigService == null ? default : _StatsigService.GetObjectFromConfig<T>(configName, jsonKey);
                return UniTask.FromResult(data);
            }
            catch (Exception e)
            {
                CrashReporter.LogError($"[DynamicConfigServiceFromStatsig] Exception: {e}");
                return UniTask.FromResult(default(T));
            }
        }


    }
}
