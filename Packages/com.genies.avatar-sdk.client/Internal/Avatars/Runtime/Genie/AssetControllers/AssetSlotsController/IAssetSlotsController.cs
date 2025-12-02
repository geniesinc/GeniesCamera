using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Genies.Refs;

namespace Genies.Avatars
{
    /// <summary>
    /// A controller for an specific type of asset that can load, equip and unequip assets to different slots.
    /// <br/><br/>
    /// Each slot can have zero or one assets equipped and the same asset can be equipped into multiple slots
    /// at the same time.
    /// </summary>
    public interface IAssetSlotsController<TAsset>
        where TAsset : IAsset
    {
        /// <summary>
        /// Currently equipped asset IDs.
        /// </summary>
        IReadOnlyCollection<string> EquippedAssetIds { get; }
        
        /// <summary>
        /// Fired whenever any slot gets cleared or a new asset is equipped.
        /// </summary>
        event Action Updated;
        
        /// <summary>
        /// Loads all the assets from the given IDs and equips them to the given slots, replacing any previously equipped assets.
        /// </summary>
        UniTask LoadAndSetEquippedAssetsAsync(IEnumerable<(string assetId, string slotId)> assets);
        
        /// <summary>
        /// Equips all the given assets to the given slots, replacing any previously equipped assets.
        /// </summary>
        UniTask SetEquippedAssetsAsync(IEnumerable<(Ref<TAsset> assetRef, string slotId)> assets);
        
        /// <summary>
        /// Loads the asset from the given ID and equips it to the given slot.
        /// </summary>
        UniTask LoadAndEquipAssetAsync(string assetId, string slotId);
        
        /// <summary>
        /// Equips the given asset to the given slot.
        /// </summary>
        UniTask EquipAssetAsync(Ref<TAsset> assetRef, string slotId);
        
        /// <summary>
        /// If the given slot has an asset equipped, it will unequip it.
        /// </summary>
        UniTask ClearSlotAsync(string slotId);
        
        /// <summary>
        /// Unequips all assets from all slots.
        /// </summary>
        UniTask ClearAllSlotsAsync();
        
        /// <summary>
        /// Whether or not the given asset ID is currently equipped on at least one slot.
        /// </summary>
        bool IsAssetEquipped(string assetId);
        
        /// <summary>
        /// Whether or not the given asset ID is currently equipped on the given slot.
        /// </summary>
        bool IsAssetEquipped(string assetId, string slotId);

        /// <summary>
        /// Whether or not the given slot has an asset equipped.
        /// </summary>
        bool IsSlotEquipped(string slotId);
        
        /// <summary>
        /// Tries to get the asset ID for the asset equipped on given slot.
        /// </summary>
        /// <returns>True if there was an asset equipped on the given slot.</returns>
        bool TryGetEquippedAssetId(string slotId, out string assetId);
        
        /// <summary>
        /// Tries to get a new asset reference to the asset equipped on the given slot. It also outputs the asset ID.
        /// </summary>
        /// <param name="assetId">The asset ID.</param>
        /// <param name="assetRef">A new reference to the loaded asset that must be owned by the caller.</param>
        /// <returns>True if the asset was found and a new alive reference was outputted.</returns>
        bool TryGetEquippedAsset(string slotId, out string assetId, out Ref<TAsset> assetRef);
        
        /// <summary>
        /// Populates the given collection with all the slot IDs where the given asset ID is equipped.
        /// The collection will not be cleared.
        /// </summary>
        void GetSlotIdsWhereEquipped(string assetId, ICollection<string> slotIds);
    }
}