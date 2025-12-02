using Genies.DataRepositoryFramework;
using FeatureType = Genies.Services.Model.GameFeature.GameFeatureTypeEnum;

namespace Genies.CloudSave
{
    /// <summary>
    /// The cloud save service allows developers to store user data in json format on the cloud.
    ///
    /// Requirements:
    ///
    /// - The <see cref="FeatureType"/> should be updated on the backend side, else the request will be rejected. https://github.com/geniesinc/genies-mobile-backend/blob/master/lambda/src/types/gamefeature.ts#L8
    /// - The format should be in Json, the client should handle any version differences if the Json is updated.
    ///
    /// </summary>
    public interface ICloudFeatureSaveService<T> : IDataRepository<T>
    {
        FeatureType FeatureTypeEnum { get; }
        void SetJsonSerializer(ICloudSaveJsonSerializer<T> serializer);
    }
}
