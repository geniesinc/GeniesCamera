using System;
using Genies.Assets.Services;
using Genies.CloudSave;
using Genies.DataRepositoryFramework;
using Genies.Inventory;
using Genies.ServiceManagement;
using Genies.Services.Model;
using UnityEngine;
using VContainer;

namespace Genies.Ugc.CustomHair
{
    [AutoResolve]
    [Serializable]
    public class CustomHairColorServiceInstaller : IGeniesInstaller
    {
        public Shader CustomHairShader;

        public CustomHairColorServiceInstaller(Shader customHairShader)
        {
            CustomHairShader = customHairShader;
        }

        public CustomHairColorServiceInstaller()
        {
        }

        public void Install(IContainerBuilder builder)
        {
            if (CustomHairShader == null)
            {
                return;
            }
            
            RegisterCustomHairService(builder);

        }

        private void RegisterCustomHairService(IContainerBuilder builder)
        {
            //Custom hair
            builder.Register
                    (
                     _ =>
                     {
                         return new CloudFeatureSaveService<CustomHairColorData>
                             (
                              GameFeature.GameFeatureTypeEnum.UgcCustomHair,
                              new CustomHairCloudSaveJsonSerializer(),
                              (data, id) => data.Id = id,
                              data => data.Id
                             );
                     },
                     Lifetime.Singleton
                    )
                   .As<IDataRepository<CustomHairColorData>>();

            builder.Register<HairColorService>(Lifetime.Singleton).WithParameter(CustomHairShader);
        }
    }
}
