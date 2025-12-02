using System.Collections.Generic;

namespace Genies.Avatars
{
    /// <summary>
    /// A rule that validates a set of assets.
    /// </summary>
    public interface IAssetsValidationRule<TAsset>
        where TAsset : IAsset
    {
        void Apply(HashSet<TAsset> assets);
    }
}