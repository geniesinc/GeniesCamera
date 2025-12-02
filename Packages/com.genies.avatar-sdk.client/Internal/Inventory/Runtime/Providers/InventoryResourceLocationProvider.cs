using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Genies.Addressables.CustomResourceLocation;
using Genies.Assets.Services;
using Genies.Models;

namespace Genies.Inventory.Providers
{
    public static class InventoryResourceLocationProvider
    {
        public static readonly string[] _assetLods = { "", $"_{AssetLod.Medium}", $"_{AssetLod.Low}" };
        public static readonly string[] _iconSizes = { "", $"_x512", $"_x1024" };
    }
}
