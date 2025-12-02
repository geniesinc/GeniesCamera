using Genies.CloudSave;
using Genies.DataRepositoryFramework;
using Genies.ServiceManagement;
using Genies.Services.Model;
using VContainer;

namespace Genies.Ugc.CustomSkin
{
    [AutoResolve]
    public class CustomSkinServiceInstaller : IGeniesInstaller
    {
        public void Install(IContainerBuilder builder)
        {
            RegisterCustomSkin(builder);
        }

        private static void RegisterCustomSkin(IContainerBuilder builder)
        {
            //Custom Skin
            builder.Register
                    (
                     _ =>
                     {
                         return new CloudFeatureSaveService<SkinColorData>
                             (
                              GameFeature.GameFeatureTypeEnum.UgcCustomSkin,
                              new CustomSkinCloudSaveJsonSerializer(),
                              (data, id) => data.Id = id,
                              data => data.Id
                             );
                     },
                     Lifetime.Singleton
                    )
                   .As<IDataRepository<SkinColorData>>();


            builder.Register<SkinColorService>(Lifetime.Singleton);
        }
    }
}
