using Genies.Login;
using Genies.ServiceManagement;
using VContainer;

namespace Genies.Avatars.Services
{
    [AutoResolve]
    public class AvatarServiceInstaller : IGeniesInstaller,
        IRequiresInstaller<IGeniesLoginInstaller>
    {
        public void Install(IContainerBuilder builder)
        {
            var avatarService = new AvatarService();
            avatarService.RegisterSelf().As<IAvatarService>();
        }
    }
}
