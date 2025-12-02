using Genies.Components.ShaderlessTools;
using Genies.ServiceManagement;
using VContainer;

namespace Genies.Assets.Services
{
    [AutoResolve]
    public class AssetServiceInstaller : IGeniesInstaller
    {
        public void Install(IContainerBuilder builder)
        {
            builder.Register<IAssetsService, AddressableAssetsService>(Lifetime.Singleton);
            builder.Register<IShaderlessAssetService, ShaderlessAssetService>(Lifetime.Singleton);
        }
    }
}
