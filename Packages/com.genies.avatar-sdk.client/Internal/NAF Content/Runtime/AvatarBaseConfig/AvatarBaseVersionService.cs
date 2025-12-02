using Cysharp.Threading.Tasks;
using Genies.ServiceManagement;

namespace Genies.Inventory
{
    public static class AvatarBaseVersionService
    {
        private const string Config = "AvatarBase/config/avatar_base_version.json";
        private static string ConfigLocation => $"{NafContentLocations.NafContentUrl}/{Config}";
        private static IContentConfigService _contentConfigService;

        public static async UniTask<string> GetAvatarBaseVersion()
        {
            var service = GetContentConfigService();
            var config = await service.FetchConfig(ConfigLocation);

            return config?.avatarBase?.version;
        }
        
        private static IContentConfigService GetContentConfigService()
        {
            if (_contentConfigService == null)
            {

                _contentConfigService = ServiceManager.Get<IContentConfigService>();
                if (_contentConfigService == null)
                {
                    _contentConfigService = new ContentConfigService();
                    ServiceManager.RegisterService(_contentConfigService).As<IContentConfigService>();
                }

            }
            return _contentConfigService;
        }
    }
}