using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Genies.Naf.Content
{
    public interface IInventoryNafLocationsProvider
    {
        public UniTask UpdateAssetLocations(string assetId);
        public UniTask AddCustomResourceLocationsFromInventory(bool includeV1Inventory = false);
    }
}
