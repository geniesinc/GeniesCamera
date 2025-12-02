using Genies.Avatars;
using UMA;

namespace Genies.Ugc
{
    /// <summary>
    /// Contains Gear asset data and assets.
    /// </summary>
    public sealed class GearAsset : IAsset
    {
        public string Id => AssetData.Id;
        public string Lod { get; }

        public readonly GearAssetData            AssetData;
        public readonly MeshHideAsset[]          MeshHideAssets;
        public readonly GearElementAsset[]       Elements;
        public readonly IGenieComponentCreator[] ComponentCreators;

        public GearAsset(
            string lod,
            GearAssetData assetData,
            MeshHideAsset[] meshHideAssets,
            GearElementAsset[] elements,
            IGenieComponentCreator[] componentCreators)
        {
            Lod = lod;
            AssetData = assetData;
            MeshHideAssets = meshHideAssets;
            Elements = elements;
            ComponentCreators = componentCreators;
        }
    }
}
