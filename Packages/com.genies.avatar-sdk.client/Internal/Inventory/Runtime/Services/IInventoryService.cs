using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;

namespace Genies.Inventory
{
    /// <summary>
    /// Responsible to manage the metadata of user's inventory
    /// </summary>
    public interface IInventoryService
    {
        /// <summary>
        /// Get the inventory for the signed-in user, uses v1 endpoint
        /// </summary>
        /// <param name="limit">The max number of assets that can be returned, newest first.
        /// If null, all are returned</param>
        /// <returns></returns>
        UniTask<UserInventoryData> GetUserInventory(int? limit = null);
        UniTask<UserInventoryDecorData> GetUserInventoryDecor();
        
        /// <summary>
        /// Clears cached inventory items
        /// </summary>
        public void ClearCache();
    }

    [Serializable]
    public struct UserInventoryDecorData
    {
        public string UserId;
        public List<InventoryDecorData> DecorList;
    }
    [Serializable]
    public struct InventoryDecorData
    {
        public string AssetId;
    }
}
