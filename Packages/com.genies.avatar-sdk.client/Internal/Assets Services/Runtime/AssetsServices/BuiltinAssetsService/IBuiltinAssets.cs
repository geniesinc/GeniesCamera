using System;
using System.Collections.Generic;
using UnityEngine.ResourceManagement.ResourceLocations;

namespace Genies.Assets.Services
{
    /// <summary>
    /// Gives access to a group of builtin assets mapped with string keys. It is meant to be used
    /// with <see cref="BuiltinAssetsService"/>.
    /// </summary>
    public interface IBuiltinAssets
    {
        bool TryGetAsset<T>(string key, out T asset);
        bool TryGetAsset<T>(IResourceLocation location, out T asset);
        IList<IResourceLocation> GetResourceLocations(string key, Type type);
        IList<IResourceLocation> GetResourceLocations(IEnumerable<string> keys, MergingMode mergingMode, Type type);
    }
}
