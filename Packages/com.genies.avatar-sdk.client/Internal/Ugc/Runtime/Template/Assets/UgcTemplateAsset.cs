using Genies.Assets.Services;
using Genies.Avatars;
using UMA;

namespace Genies.Ugc
{
    /// <summary>
    /// Contains UGC template data and assets.
    /// </summary>
    public sealed class UgcTemplateAsset : IAsset
    {
        public string Id => Data.TemplateId;
        public string Lod => AssetLod.Default;

        public UgcTemplateData Data { get; }
        public MeshHideAsset[] MeshHideAssets { get; }
        public IGenieComponentCreator[] ComponentCreators { get; }

        public UgcTemplateAsset(
            UgcTemplateData data,
            MeshHideAsset[] meshHideAssets,
            IGenieComponentCreator[] componentCreators)
        {
            Data = data;
            MeshHideAssets = meshHideAssets;
            ComponentCreators = componentCreators;
        }

    }
}
