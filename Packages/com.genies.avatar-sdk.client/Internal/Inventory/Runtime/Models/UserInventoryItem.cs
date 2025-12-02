using Genies.Services.Model;

namespace Genies.Inventory
{
    public readonly struct UserInventoryItem
    {
        public string AssetId { get; }
        public string AssetType { get; }
        public string InstanceId { get; }
        public long? DateCreated { get; }
        public string Origin { get;}
        public string Creator { get; }
        public InventoryItemAsset Asset { get; }
        public InventoryTags Tags { get;}

        public UserInventoryItem(InventoryAssetInstance inventoryAsset)
        {
            AssetId = inventoryAsset.Asset.AssetId;
            AssetType = inventoryAsset.Asset.AssetType;
            InstanceId = inventoryAsset.AssetInstanceId;
            DateCreated = inventoryAsset.DateCreated;
            Asset = inventoryAsset.Asset;
            Tags = inventoryAsset.Tags;
            Origin = inventoryAsset.Origin;
            Creator = inventoryAsset.Creator;
        }
    }
}
