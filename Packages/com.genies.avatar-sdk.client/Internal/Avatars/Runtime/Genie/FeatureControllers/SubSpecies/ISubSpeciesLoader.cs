using Cysharp.Threading.Tasks;
using Genies.Assets.Services;
using Genies.Refs;

namespace Genies.Avatars
{
    public interface ISubSpeciesLoader
    {
        /// <summary>
        /// Loads and returns a reference to a <see cref="SubSpeciesAsset"/> instance identified by the given <see cref="assetId"/>.
        /// </summary>
        UniTask<Ref<SubSpeciesAsset>> LoadAsync(string assetId, string lod = AssetLod.Default);
    }
}
