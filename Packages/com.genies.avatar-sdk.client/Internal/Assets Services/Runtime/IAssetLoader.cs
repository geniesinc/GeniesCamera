using Cysharp.Threading.Tasks;
using Genies.Refs;

namespace Genies.Assets.Services
{
    /// <summary>
    /// Asynchronously loads T instances. It returns a <see cref="Ref{T}"/>
    /// to the asset that must be disposed when the asset is no longer used.
    /// </summary>
    public interface IAssetLoader<T>
    {
        /// <summary>
        /// Loads and returns a reference to a T identified by the given <see cref="assetId"/>.
        /// </summary>
        UniTask<Ref<T>> LoadAsync(string assetId, string lod = AssetLod.Default);
    }
}
