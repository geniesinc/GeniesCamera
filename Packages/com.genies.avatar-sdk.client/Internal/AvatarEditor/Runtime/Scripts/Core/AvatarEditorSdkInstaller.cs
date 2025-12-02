using Genies.ServiceManagement;
using VContainer;

namespace Genies.AvatarEditor.Core
{
    /// <summary>
    /// Installer for Avatar Editor SDK services.
    /// Registers the Avatar Editor SDK service instance with the service manager.
    /// </summary>
    [AutoResolve]
#if GENIES_SDK && !GENIES_INTERNAL
    internal class AvatarEditorSdkInstaller : IGeniesInstaller
#else
    public class AvatarEditorSdkInstaller : IGeniesInstaller
#endif
    {
        public int OperationOrder => DefaultInstallationGroups.DefaultServices + 10; // After dependent services

        public void Install(IContainerBuilder builder)
        {
            Register();
        }

        public void Register()
        {
            var newInstance = new AvatarEditorSdkService();
            newInstance.RegisterSelf().As<IAvatarEditorSdkService>();
        }
    }
}
