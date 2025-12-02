using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Genies.Naf.Content
{
    /// <summary>
    /// Base class for NafContentService implementations that provides shared functionality
    /// for asset ID conversion and parameter fetching
    /// </summary>
    public abstract class NafContentServiceBase : IAssetParamsService, IAssetIdConverter
    {
        protected bool _initialized = false;
        protected readonly Dictionary<string, NafContentMetadata> _assetsByAddress = new();
        protected readonly IReadOnlyDictionary<string, string> _avatarBaseGuidMap = StaticToBaseMap.LocalGuidMap;
        protected readonly List<string> _staticMapping = new() {"AvatarDna", "AvatarTattoo"};
        protected readonly List<string> _overrideVersion = new() {"AvatarBase",};
        protected string _AvatarBaseVersionFromConfig = null;

        /// <summary>
        /// Initialize the service with data from the specific source (inventory, CMS, etc.)
        /// </summary>
        public abstract UniTask Initialize();

        public async UniTask<string> ConvertToUniversalIdAsync(string assetId)
        {
            await InitializeIfNeededAsync();

            var resultId = GetUniversalId(assetId);

            // Temp Hotfix Remove spaces, using %20 does not work, until next Naf update
            resultId = resultId.Replace(' ', '+');
            return await UniTask.FromResult<string>(resultId);
        }

        public async UniTask<Dictionary<string, string>> FetchParamsAsync(string assetId)
        {
            await InitializeIfNeededAsync();

            Dictionary<string, string> result = GetParams(assetId, LodLevels.DefaultLod);
            return await UniTask.FromResult<Dictionary<string, string>>(result);
        }

        /// <summary>
        /// Returns the input if it cant determine or translate the assetId
        /// </summary>
        protected string GetUniversalId(string assetId)
        {
            if (!_assetsByAddress.TryGetValue(assetId, out var result))
            {
                // fallback try lookup avatarBaseGuids instead
                var mappedId = ToAvatarBaseMapping(assetId);
                if (!_assetsByAddress.TryGetValue(mappedId, out result))
                {
                    return assetId;
                }
            }

            var pipelineId = !string.IsNullOrEmpty(result.PipelineId)
                ? (_staticMapping.Contains(result.PipelineId) ? "Static/" : $"{result.PipelineId}/")
                : string.Empty;

            return string.IsNullOrEmpty(pipelineId)? assetId : 
                string.Equals(pipelineId, "Static/")? $"{pipelineId}{result.AssetAddress}" : $"{pipelineId}{result.Guid}";
        }

        protected Dictionary<string, string> GetParams(string assetId, string lod = null)
        {
            if (!_assetsByAddress.TryGetValue(ToLookupKey(assetId), out NafContentMetadata result))
            {
                return default;
            }

            var assetParams = new Dictionary<string, string>();
            
            if (result.UniversalBuildVersion != null)
            {
                var version = string.IsNullOrEmpty(result.UniversalBuildVersion) ? "0" : result.UniversalBuildVersion;
                
                // use config override for AvatarBase if set, otherwise use the version from the metadata
                if (_overrideVersion.Contains(result.PipelineId))
                {
                    version = !string.IsNullOrEmpty(_AvatarBaseVersionFromConfig) ? _AvatarBaseVersionFromConfig : result.UniversalBuildVersion;
                }
                
                assetParams.Add("v", version);
            }

            if (!string.IsNullOrEmpty(lod))
            {
                assetParams.Add("lod", lod);
            }

            return assetParams;
        }

        protected async UniTask InitializeIfNeededAsync()
        {
            if (!_initialized)
            {
                await Initialize();
            }
        }

        /// <summary>
        /// Strips assetType/ from assetAddress or just returns the assetId if no type is present.
        /// eg: recSjNgdNxWYeuLeD || WardrobeGear/recSjNgdNxWYeuLeD => recSjNgdNxWYeuLeD
        /// eg: Genie_Unified_gen13gp_Race_Container || Static/Genie_Unified_gen13gp_Race_Container => Genie_Unified_gen13gp_Race_Container
        /// Finds key for both types of assetIds
        /// </summary>
        protected static string ToLookupKey(string assetId)
        {
            // just substrings last part after '/'
            var pathIdx = assetId.LastIndexOf('/');
            return pathIdx == -1 ? assetId : assetId.Substring(pathIdx + 1);
        }

        /// <summary>
        /// Temporary Fix while migration is underway, to get AvatarBase guids using static ids
        /// only works for BodyType Containers, assets that do not show on the UI
        /// </summary>
        /// <param name="assetId"></param>
        /// <returns></returns>
        protected string ToAvatarBaseMapping(string assetId)
        {
            // also include the same metadata for static ids of BodyType Containers.
            return _avatarBaseGuidMap.TryGetValue(assetId, out var avatarBaseGuid) ? avatarBaseGuid : assetId;
        }

        /// <summary>
        /// Merges source dictionary into target dictionary, overwriting duplicates.
        /// </summary>
        protected static void Merge<TKey, TValue>(IDictionary<TKey, TValue> target, IDictionary<TKey, TValue> source)
        {
            foreach (KeyValuePair<TKey, TValue> kvp in source)
            {
                target[kvp.Key] = kvp.Value; // overwrites duplicates
            }
        }
        
        // Lods on Mac need to be High by default due normals not matching for mac m gpus
        protected static class LodLevels
        {
            public const string High = "0";
            public const string Mid = "1";
            public const string Low = "2";
            public static string DefaultLod => Application.platform is RuntimePlatform.OSXEditor or RuntimePlatform.OSXPlayer ? High : Low;
        }
        
        protected static class StaticToBaseMap
        {
            // during integration we have to support static ids and avatarBase ids
            // this map is fallback when controllers call using Legacy Core BodyTypes ids
            public static readonly IReadOnlyDictionary<string, string> LocalGuidMap = new Dictionary<string, string>()
            {
                // all Body Type Containers
                {"Genie_Unified_gen13gp_Race_Container", "recmDqoKYpEG1TQV"},
                {"Static/Genie_Unified_gen13gp_Race_Container", "recmDqoKYpEG1TQV"},

                {"Genie_Unified_gen12gp_Container", "recMdZ4WQ4HSkb8U"},
                {"Static/Genie_Unified_gen12gp_Container", "recMdZ4WQ4HSkb8U"},

                {"Genie_Unified_gen11gp_Container", "recmdz4WQ4hM30ZC"},
                {"Static/Genie_Unified_gen11gp_Container", "recmdz4WQ4hM30ZC"},

                {"DollGen1_RaceData_Container", "recMdZ4wQ4HQS1uC"},
                {"Static/DollGen1_RaceData_Container", "recMdZ4wQ4HQS1uC"},

                {"BlendShapeContainer_body_female", "recmdZ4C4enmt630"},
                {"Static/BlendShapeContainer_body_female", "recmdZ4C4enmt630"},

                {"BlendShapeContainer_body_male", "recmdZ4c4ENEO817"},
                {"Static/BlendShapeContainer_body_male", "recmdZ4c4ENEO817"},
            };
        }
    }
}
