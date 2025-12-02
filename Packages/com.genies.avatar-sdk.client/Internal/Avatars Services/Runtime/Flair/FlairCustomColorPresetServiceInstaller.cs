using Genies.CloudSave;
using Genies.DataRepositoryFramework;
using Genies.ServiceManagement;
using Genies.Services.Model;
using VContainer;

namespace Genies.Avatars.Services.Flair
{
    [AutoResolve]
    public class FlairCustomColorPresetServiceInstaller : IGeniesInstaller
    {
        public void Install(IContainerBuilder builder)
        {
            builder.Register
                (
                    _ =>
                    {
                        return new CloudFeatureSaveService<FlairColorPreset>
                        (
                            GameFeature.GameFeatureTypeEnum.CustomFlairColors,
                            new FlairColorPresetCloudSaveJsonSerializer(),
                            (data, id) => data.Id = id,
                            data => data.Id
                        );
                    },
                    Lifetime.Singleton
                )
                .As<IDataRepository<FlairColorPreset>>();
            builder.Register<IFlairCustomColorPresetService, FlairCustomColorPresetService>(Lifetime.Singleton);
        }

    }
}
