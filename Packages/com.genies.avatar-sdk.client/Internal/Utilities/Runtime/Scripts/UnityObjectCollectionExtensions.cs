using System.Collections.Generic;
using Object = UnityEngine.Object;

namespace Genies.Utilities
{
    public static class UnityObjectCollectionExtensions
    {
        public static bool TryGetAsset<T>(this IEnumerable<Object> assets, out T result)
        {
            foreach (Object asset in assets)
            {
                if (asset is not T tAsset)
                {
                    continue;
                }

                result = tAsset;
                return true;
            }
            
            result = default;
            return false;
        }

        public static List<T> GetAssets<T>(this IEnumerable<Object> assets)
        {
            var results = new List<T>();
            GetAssets(assets, results);
            return results;
        }
        
        public static List<T> GetAssets<T>(this ICollection<Object> assets)
        {
            var results = new List<T>(assets.Count);
            GetAssets(assets, results);
            return results;
        }
        
        public static void GetAssets<T>(this IEnumerable<Object> assets, ICollection<T> results)
        {
            foreach (Object asset in assets)
            {
                if (asset is T tAsset)
                {
                    results.Add(tAsset);
                }
            }
        }
    }
}
