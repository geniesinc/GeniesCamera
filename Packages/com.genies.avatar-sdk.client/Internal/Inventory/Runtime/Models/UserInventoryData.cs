using System;
using System.Collections.Generic;
using Genies.Services.Model;

namespace Genies.Inventory
{
    [Serializable]
    public readonly struct UserInventoryData
    {
        public readonly string UserId;
        public readonly IReadOnlyList<UserInventoryItem> Items;

        public UserInventoryData(string userId, List<InventoryAssetInstance> assets)
        {
            UserId = userId;

            // create a mutable array to add items
            var tempItems = new List<UserInventoryItem>(assets.Count);
            foreach (InventoryAssetInstance asset in assets)
            {
                tempItems.Add(new UserInventoryItem(asset));
            }

            // convert to immutable
            Items = tempItems.AsReadOnly();
        }
    }
}
