using System.Collections.Generic;

namespace Genies.Avatars
{
    /// <summary>
    /// A rule that validates a single-asset adjustment operation on a set of assets.
    /// This rule can be executed both for an asset that is being added or removed from the set.
    /// Depending on the implementation it may be called before or after the adjustment took place.
    /// </summary>
    public interface IAssetsAdjustmentRule<TAsset>
        where TAsset : IAsset
    {
        void Apply(HashSet<TAsset> assets, TAsset adjustedAsset);
    }
}