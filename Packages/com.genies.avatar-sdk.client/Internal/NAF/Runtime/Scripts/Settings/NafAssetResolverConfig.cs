using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Genies.Naf
{
#if GENIES_INTERNAL
    [CreateAssetMenu(fileName = "AssetResolverConfig", menuName = "Genies/NAF/Asset Resolver Config")]
#endif
    public sealed class NafAssetResolverConfig : ScriptableObject
    {
        public int            cacheSizeMB = 2000;
        public string         cacheFile   = "cache.bin";
        public bool           useCache    = true;
        public string         cachePath   = "cache";
        public List<Resolver> resolvers;

        public string Serialize()
        {
            var resolversArray = new JArray();
            var assetResolver = new JObject
            {
                ["cacheSizeMB"] = cacheSizeMB,
                ["cacheFile"]   = cacheFile,
                ["useCache"]    = useCache,
                ["cachePath"]   = cachePath,
                ["Types"]       = resolversArray
            };

            foreach (Resolver resolver in resolvers)
            {
                if (resolver.disabled)
                {
                    continue;
                }

                var resolverObj = new JObject
                {
                    ["order"]       = resolver.order,
                    ["sourcePath"]  = resolver.sourcePath
                };

                resolverObj["type"] = resolver switch
                {
                    { type: ResolverType.DefaultResolver }  => "DefaultResolver",
                    { type: ResolverType.LocalResolver }    => "LocalResolver",
                    { type: ResolverType.HttpResolver }     => "HTTPResolver",
                    { type: ResolverType.BundleResolver }   => "BundleResolver",
                    _ => throw new ArgumentOutOfRangeException(nameof(resolver.type), resolver.type, null)
                };

                if (resolver.uriSchemes != null && resolver.uriSchemes.Count > 0)
                {
                    var uriSchemesArray = new JArray(resolver.uriSchemes);
                    resolverObj["uriSchemes"] = uriSchemesArray;
                }

                resolversArray.Add(resolverObj);
            }

            var config = new JObject { ["AssetResolver"] = assetResolver };
            var obj    = new JObject { ["Config"] = new JArray(config) };

            return obj.ToString();
        }

        public static NafAssetResolverConfig Default
        {
            get
            {
                if (_default)
                {
                    return _default;
                }

                _default = CreateInstance<NafAssetResolverConfig>();
                return _default;
            }

            set
            {
                if (value)
                {
                    _default = value;
                }
            }
        }

        private static NafAssetResolverConfig _default;

        public enum ResolverType
        {
            DefaultResolver = 0,
            LocalResolver   = 1,
            HttpResolver    = 2,
            BundleResolver  = 3,
        }

        [Serializable]
        public struct Resolver
        {
            public bool         disabled;
            public ResolverType type;
            public int          order;
            public string       sourcePath;
            public List<string> uriSchemes;
        }

        [ContextMenu("Serialize and copy to clipboard")]
        private void SerializeAndCopyToClipboard()
        {
            GUIUtility.systemCopyBuffer = Serialize();
        }
    }
}
