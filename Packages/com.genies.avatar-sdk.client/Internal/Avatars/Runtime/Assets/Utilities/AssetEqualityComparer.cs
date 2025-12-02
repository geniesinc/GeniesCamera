using System.Collections.Generic;

namespace Genies.Avatars
{
    /// <summary>
    /// Equality comparer for <see cref="IAsset"/> types that compares only their ID. Useful to create <see cref="HashSet{T}"/>
    /// or <see cref="Dictionary{TKey,TValue}"/> instances of assets that automatically compares them by their ID.
    /// </summary>
    public sealed class AssetEqualityComparer<TAsset> : IEqualityComparer<TAsset>
        where TAsset : IAsset
    {
        public bool Equals(TAsset x, TAsset y)
            => x?.Id == y?.Id;

        public int GetHashCode(TAsset obj)
            => obj?.Id?.GetHashCode() ?? 0;
    }
}