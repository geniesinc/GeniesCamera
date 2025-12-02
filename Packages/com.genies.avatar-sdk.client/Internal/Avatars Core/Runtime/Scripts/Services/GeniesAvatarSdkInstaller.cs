using Genies.Addressables;
using Genies.Avatars.Services;
using Genies.Naf.Addressables;
using Genies.ServiceManagement;
using UnityEngine;
using VContainer;

namespace Genies.Avatars.Sdk
{
    [AutoResolve]
#if GENIES_SDK && !GENIES_INTERNAL
    internal class GeniesAvatarSdkInstaller : IGeniesInstaller,
#else
    public class GeniesAvatarSdkInstaller : IGeniesInstaller,
#endif
        IRequiresInstaller<AddressableServicesInstaller>,
        IRequiresInstaller<AvatarServiceInstaller>,
        IRequiresInstaller<NafResourceProviderInstaller>
    {
        public int OperationOrder => DefaultInstallationGroups.DefaultServices + 3; // Must come after dependent IGeniesInstallers.

        public void Install(IContainerBuilder builder)
        {
            var newInstance = new GeniesAvatarSdkService();
            newInstance.RegisterSelf().As<IGeniesAvatarSdkService>();
        }
    }
}
