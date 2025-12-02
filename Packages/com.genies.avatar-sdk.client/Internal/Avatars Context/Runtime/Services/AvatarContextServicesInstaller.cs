using Genies.Addressables;
using Genies.Assets.Services;
using Genies.ServiceManagement;
using Genies.Ugc;
using VContainer;

namespace Genies.Avatars.Context
{
    [AutoResolve]
    public class AvatarContextServicesInstaller : IGeniesInstaller,
        IRequiresInstaller<AddressableServicesInstaller>
    {
        public void Install(IContainerBuilder builder)
        {
            builder.Register<ProjectedTexturesProvider>(Lifetime.Singleton);
            builder.Register<IAssetLoader<UgcTemplateAsset>, UgcTemplateLoader>(Lifetime.Singleton);
            builder.Register<IAssetLoader<UgcElementAsset>, UgcElementLoader>(Lifetime.Singleton);
            builder.Register<IUgcTemplateDataService, UgcTemplateDataService>(Lifetime.Singleton);

            //Projected textures
            builder.Register<IProjectedTextureService, ProjectedTextureRemoteLoaderService>(Lifetime.Singleton);
        }
    }
}
