using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Genies.Refs;

namespace Genies.Avatars
{
    /// <summary>
    /// A controller for an specific type of asset that can load, equip and unequip assets.
    /// </summary>
    public interface IAssetsController<TAsset>
        where TAsset : IAsset
    {
        /// <summary>
        /// Currently equipped asset IDs.
        /// </summary>
        IReadOnlyCollection<string> EquippedAssetIds { get; }
        
        /// <summary>
        /// Fired whenever equipped assets changes.
        /// </summary>
        event Action Updated;
        
        /// <summary>
        /// Loads all the assets from the given IDs and equips them, replacing any previously equipped assets.
        /// </summary>
        UniTask LoadAndSetEquippedAssetsAsync(IEnumerable<string> assetIds);
        
        /// <summary>
        /// Equips all the given assets, replacing any previously equipped assets.
        /// </summary>
        UniTask SetEquippedAssetsAsync(IEnumerable<Ref<TAsset>> assetRefs);
        
        /// <summary>
        /// Loads the asset from the given ID and equips it.
        /// </summary>
        UniTask LoadAndEquipAssetAsync(string assetId);
        
        /// <summary>
        /// Equips the given asset.
        /// </summary>
        UniTask EquipAssetAsync(Ref<TAsset> assetRef);
        
        /// <summary>
        /// Unequips the asset identified by the given ID, if any.
        /// </summary>
        UniTask UnequipAssetAsync(string assetId);
        
        /// <summary>
        /// Unequips all currently equipped assets.
        /// </summary>
        UniTask UnequipAllAssetsAsync();
        
        /// <summary>
        /// Whether or not the given asset ID is currently equipped.
        /// </summary>
        bool IsAssetEquipped(string assetId);
        
        /// <summary>
        /// Tries to get a new asset reference to the asset represented by the given ID, if it is currently loaded and equipped.
        /// </summary>
        /// <param name="assetRef">A new reference to the asset that must be owned by the caller.</param>
        /// <returns>True if the asset was found and a new alive reference was returned.</returns>
        bool TryGetEquippedAsset(string assetId, out Ref<TAsset> assetRef);
    }
}