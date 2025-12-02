#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;

namespace Genies.Utilities.Editor
{
    public class AssetSearchUtils
    {
        private static PackageInfo[] GetAllPackages()
        {
            return PackageInfo.GetAllRegisteredPackages();
        }

        public static IEnumerable<string> GetAllAssetPaths<T>(bool includePackages = true)
        {
            var searchPaths = new List<string>
            {
                "Assets"
            };

            if (includePackages)
            {
                searchPaths.AddRange(GetAllPackages().Select(s => s.assetPath));
            }

            foreach (var path in searchPaths)
            {
                foreach (var guid in AssetDatabase.FindAssets(string.Empty, new[] { path }))
                {
                    var assetPath = AssetDatabase.GUIDToAssetPath(guid);
                    if (AssetDatabase.IsValidFolder(assetPath))
                    {
                        continue;
                    }

                    var asset = AssetDatabase.LoadMainAssetAtPath(assetPath);
                    if (asset is T)
                    {
                        yield return assetPath;
                    }
                    else if (typeof(Component).IsAssignableFrom(typeof(T)) && asset is GameObject go)
                    {
                        if (go.GetComponent(typeof(T)) != null)
                        {
                            yield return assetPath;
                        }
                    }
                }
            }
        }

        public static IEnumerable<T> GetAllAssets<T>(bool includePackages = true) where T : class
        {
            var searchPaths = new List<string>
            {
                "Assets"
            };

            if (includePackages)
            {
                searchPaths.AddRange(GetAllPackages().Select(s => s.assetPath));
            }

            foreach (var path in searchPaths)
            {
                foreach (var guid in AssetDatabase.FindAssets(string.Empty, new[] { path }))
                {
                    var assetPath = AssetDatabase.GUIDToAssetPath(guid);
                    if (AssetDatabase.IsValidFolder(assetPath))
                    {
                        continue;
                    }

                    var asset = AssetDatabase.LoadMainAssetAtPath(assetPath);
                    if (asset is T value)
                    {
                        yield return value;
                    }
                    else if (typeof(Component).IsAssignableFrom(typeof(T)))
                    {
                        if (asset is not GameObject go)
                        {
                            continue;
                        }
                        
                        var comp = go.GetComponents(typeof(T));

                        foreach (var c in comp)
                        {
                            yield return c as T;
                        }
                    }
                }
            }
        }
    }
}
#endif
