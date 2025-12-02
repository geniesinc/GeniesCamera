using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Genies.Addressables.Editor.DataModels;
using UnityEngine.AddressableAssets.Initialization;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.ResourceManagement.ResourceLocations;
using UnityEngine.ResourceManagement.Util;
using UnityEngine;
using SerializedType = UnityEngine.ResourceManagement.Util.SerializedType;

namespace Genies.Addressables.Editor.Utilities
{
    /// <summary>
    /// Utility that loads, merges, and saves catalogs
    /// </summary>
    public static class CatalogUtility
    {

        #region Public Methods

        /// <summary>
        /// Load Content Catalog
        /// </summary>
        /// <param name="path"></param>
        public static ContentCatalogData Load(string path)
        {
            var jsonStr = File.ReadAllText(path);
            return JsonUtility.FromJson<ContentCatalogData>(jsonStr);
        }
#if ENABLE_JSON_CATALOG
        /// <summary>
        /// Merge Content Catalogs
        /// </summary>
        /// <param name="contentCatalogDataList"></param>
        public static ContentCatalogData Merge(List<ContentCatalogData> contentCatalogDataList)
        {
            try
            {
                var entryIndex = 0;
                var catalogIndex = 0;
                AddressablesRuntimeProperties.ClearCachedPropertyValues();
                var mergedCatalog = new ContentCatalogData(ResourceManagerRuntimeData.kCatalogAddress);
                var combinedContentCatalogDataEntryDataDict = new Dictionary<string, ContentCatalogDataEntryData>();
                string[] currentPrefixes = null;
                SerializedType[] currentResourceTypes = null;
                List<ObjectInitializationData> objectInitializationDatas = null;
                var entries = new List<ContentCatalogDataEntry>();

                foreach (var catalog in contentCatalogDataList)
                {
                    catalogIndex++;
                    var contentCatalogDataEntryDataDict = new Dictionary<string, ContentCatalogDataEntryData>();
                    var locator = catalog.CreateLocator();

                    foreach (var internalId in catalog.InternalIds)
                        foreach (var location in GetAllLocationsByInternalId(internalId, locator.Locations))
                            AddLocationToDictionary(location.PrimaryKey, ref entryIndex, location,
                                ref contentCatalogDataEntryDataDict);

                    foreach (var keyValuePair in locator.Locations)
                        if (keyValuePair.Key.GetType() != typeof(int))
                            foreach (var location in keyValuePair.Value)
                                AddLocationToDictionary(keyValuePair.Key, ref entryIndex, location,
                                    ref contentCatalogDataEntryDataDict);

                    mergedCatalog.InstanceProviderData = catalog.InstanceProviderData;
                    var addPrefixes = catalog.GetFieldValue<string[]>("m_InternalIdPrefixes");
                    currentPrefixes = CombineInternalPrefixes(currentPrefixes, addPrefixes);
                    objectInitializationDatas =
                        CombineResourceProviderData(objectInitializationDatas, catalog.ResourceProviderData);
                    var addResourceTypes = catalog.GetFieldValue<SerializedType[]>("m_resourceTypes");
                    currentResourceTypes = CombineResourceTypes(currentResourceTypes, addResourceTypes);
                    mergedCatalog.SceneProviderData = catalog.SceneProviderData;

                    foreach (var keyValuePair in contentCatalogDataEntryDataDict)
                    {
                        try
                        {
                            combinedContentCatalogDataEntryDataDict.Add(keyValuePair.Key, keyValuePair.Value);
                        }
                        catch
                        {
                            Debug.LogError($"Duplicate catalog key in catalog:{catalogIndex} key:{keyValuePair.Key}");
                        }
                    }
                }

                for (var i = 0; i < combinedContentCatalogDataEntryDataDict.Keys.Count; i++)
                {
                    var contentCatalogDataEntryData = GetEntryByIndex(i, combinedContentCatalogDataEntryDataDict);
                    if (contentCatalogDataEntryData == null) continue;
                    try
                    {
                        foreach (var dataType in contentCatalogDataEntryData.types)
                            entries.Add(new ContentCatalogDataEntry(dataType.type, contentCatalogDataEntryData.internalId,
                                contentCatalogDataEntryData.provider, contentCatalogDataEntryData.keys,
                                contentCatalogDataEntryData.dependencies, dataType.data));
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                        throw;
                    }

                }

                //set all the catalog data
                mergedCatalog.SetFieldValue("m_InternalIdPrefixes", currentPrefixes);
                mergedCatalog.ResourceProviderData = objectInitializationDatas;
                mergedCatalog.SetFieldValue("m_resourceTypes", currentResourceTypes);
#if  UNITY_6000_0_OR_NEWER
                mergedCatalog.SetData(entries);
#else
                mergedCatalog.SetData(entries, true);
#endif                

                //mergedCatalog = new ContentCatalogData(entries);
                return mergedCatalog;
            }
            catch (Exception e)
            {
                Debug.LogError($"Catalog Merge Failed! error:{e}");
                return null;
            }
        }

        /// <summary>
        /// Add Version Less Key(s) to Content Catalog
        /// </summary>
        /// <param name="contentCatalogData"></param>
        public static ContentCatalogData AddClientVersionLessKeys(ContentCatalogData contentCatalogData)
        {
            var returnCatalog = new ContentCatalogData(ResourceManagerRuntimeData.kCatalogAddress);
            var entries = new List<ContentCatalogDataEntry>();
            var entryIndex = 0;
            var contentCatalogDataEntryDataDict = new Dictionary<string, ContentCatalogDataEntryData>();
            string[] currentPrefixes = null;
            SerializedType[] currentResourceTypes = null;
            List<ObjectInitializationData> objectInitializationDatas = null;
            var locator = contentCatalogData.CreateLocator();

            foreach (var internalId in contentCatalogData.InternalIds)
            foreach (var location in GetAllLocationsByInternalId(internalId, locator.Locations))
                AddLocationToDictionary(location.PrimaryKey, ref entryIndex, location,
                    ref contentCatalogDataEntryDataDict);

            foreach (var keyValuePair in locator.Locations)
                if (keyValuePair.Key.GetType() != typeof(int))
                    foreach (var location in keyValuePair.Value)
                        AddLocationToDictionary(keyValuePair.Key, ref entryIndex, location,
                            ref contentCatalogDataEntryDataDict);

            for (var i = 0; i < contentCatalogDataEntryDataDict.Keys.Count; i++)
            {
                var contentCatalogDataEntryData = GetEntryByIndex(i, contentCatalogDataEntryDataDict);
                var newKeys = contentCatalogDataEntryData.keys.ToList();

                if (contentCatalogDataEntryData.provider == "UnityEngine.ResourceManagement.ResourceProviders.BundledAssetProvider")
                {
                    var clientAndVersionLessKey = ((string)newKeys[0]);
                    var firstIndex = clientAndVersionLessKey.IndexOf('_');
                    var lastIndex = clientAndVersionLessKey.LastIndexOf('_');
                    clientAndVersionLessKey = clientAndVersionLessKey.Substring(firstIndex + 1, lastIndex - firstIndex - 1);
                    if (clientAndVersionLessKey != (string)contentCatalogDataEntryData.keys[0])
                        newKeys.Insert(0, clientAndVersionLessKey);
                }

                foreach (var dataType in contentCatalogDataEntryData.types)
                {
                    entries.Add(new ContentCatalogDataEntry(dataType.type, contentCatalogDataEntryData.internalId,
                        contentCatalogDataEntryData.provider, newKeys,
                        contentCatalogDataEntryData.dependencies, dataType.data));
                }
            }

            //set all the catalog data
            returnCatalog.InstanceProviderData = contentCatalogData.InstanceProviderData;
            returnCatalog.SetFieldValue("m_InternalIdPrefixes", contentCatalogData.GetFieldValue<string[]>("m_InternalIdPrefixes"));
            returnCatalog.ResourceProviderData = contentCatalogData.ResourceProviderData;
            returnCatalog.SetFieldValue("m_resourceTypes", contentCatalogData.GetFieldValue<SerializedType[]>("m_resourceTypes"));
            returnCatalog.SceneProviderData = contentCatalogData.SceneProviderData;
            
#if  UNITY_6000_0_OR_NEWER
            returnCatalog.SetData(entries);
#else
            returnCatalog.SetData(entries, true);
#endif                

            return returnCatalog;
        }


        /// <summary>
        /// Create InternalId in providers as interpolated version {VersionInt}
        /// </summary>
        /// <param name="contentCatalogData"></param>
        /// <param name="identifier"></param>
        public static ContentCatalogData TransformInternalIdToInterpolatedVersion(ContentCatalogData contentCatalogData, string identifier = AddressableInterpolatedValues.InterpolatedVersionString)
        {
            var returnCatalog = new ContentCatalogData(ResourceManagerRuntimeData.kCatalogAddress);
            var entries = new List<ContentCatalogDataEntry>();
            var entryIndex = 0;
            var contentCatalogDataEntryDataDict = new Dictionary<string, ContentCatalogDataEntryData>();
            string[] currentPrefixes = null;
            SerializedType[] currentResourceTypes = null;
            List<ObjectInitializationData> objectInitializationDatas = null;
            var locator = contentCatalogData.CreateLocator();

            foreach (var internalId in contentCatalogData.InternalIds)
            foreach (var location in GetAllLocationsByInternalId(internalId, locator.Locations))
                AddLocationToDictionary(location.PrimaryKey, ref entryIndex, location,
                    ref contentCatalogDataEntryDataDict);

            foreach (var keyValuePair in locator.Locations)
                if (keyValuePair.Key.GetType() != typeof(int))
                    foreach (var location in keyValuePair.Value)
                        AddLocationToDictionary(keyValuePair.Key, ref entryIndex, location,
                            ref contentCatalogDataEntryDataDict);

            for (var i = 0; i < contentCatalogDataEntryDataDict.Keys.Count; i++)
            {
                var contentCatalogDataEntryData = GetEntryByIndex(i, contentCatalogDataEntryDataDict);
                var newKeys = contentCatalogDataEntryData.keys.ToList();

                if (contentCatalogDataEntryData.provider == "UnityEngine.ResourceManagement.ResourceProviders.BundledAssetProvider")
                {
                    for (var index = 0; index < contentCatalogDataEntryData.dependencies.Count; index++)
                    {
                        var dependency = contentCatalogDataEntryData.dependencies[index];
                        contentCatalogDataEntryData.dependencies[index] = $"{dependency}{AddressableInterpolatedValues.InterpolatedInternalIdKeyAddString}";
                    }

                    var interpolatedVersionKey = ((string)newKeys[0]);
                    var owner = interpolatedVersionKey.Split('_')[0];
                    var firstIndex = interpolatedVersionKey.IndexOf('_');
                    var lastIndex = interpolatedVersionKey.LastIndexOf('_');
                    contentCatalogDataEntryData.internalId = $"{contentCatalogDataEntryData.internalId}{AddressableInterpolatedValues.InterpolatedReplaceString}";
                    interpolatedVersionKey = interpolatedVersionKey.Substring(firstIndex + 1, lastIndex - firstIndex - 1);
                    interpolatedVersionKey = $"{interpolatedVersionKey}{AddressableInterpolatedValues.InterpolatedVersionKeyAddString}";
                    if (interpolatedVersionKey != (string)contentCatalogDataEntryData.keys[0])
                    {
                        newKeys[0] = $"{owner}_{interpolatedVersionKey}";
                        newKeys.Insert(0, interpolatedVersionKey);
                    }
                }

                if (contentCatalogDataEntryData.provider == "UnityEngine.ResourceManagement.ResourceProviders.AssetBundleProvider")
                {
                    var internalId = contentCatalogDataEntryData.internalId;
                    var pattern = "_v\\d+.";
                    internalId = Regex.Replace(internalId, pattern, "_v" + identifier + ".");
                    var pattern2 = "/v\\d+/";
                    internalId = Regex.Replace(internalId, pattern2, "/v" + identifier + "/");
                    contentCatalogDataEntryData.internalId = internalId;
                    newKeys[0] = $"{newKeys[0]}{AddressableInterpolatedValues.InterpolatedInternalIdKeyAddString}";
                }

                foreach (var dataType in contentCatalogDataEntryData.types)
                {
                    entries.Add(new ContentCatalogDataEntry(dataType.type, contentCatalogDataEntryData.internalId,
                        contentCatalogDataEntryData.provider, newKeys,
                        contentCatalogDataEntryData.dependencies, dataType.data));
                }
            }

            //set all the catalog data
            returnCatalog.InstanceProviderData = contentCatalogData.InstanceProviderData;
            returnCatalog.SetFieldValue("m_InternalIdPrefixes", contentCatalogData.GetFieldValue<string[]>("m_InternalIdPrefixes"));
            returnCatalog.ResourceProviderData = contentCatalogData.ResourceProviderData;
            returnCatalog.SetFieldValue("m_resourceTypes", contentCatalogData.GetFieldValue<SerializedType[]>("m_resourceTypes"));
            returnCatalog.SceneProviderData = contentCatalogData.SceneProviderData;
            
#if  UNITY_6000_0_OR_NEWER
            returnCatalog.SetData(entries);
#else
            returnCatalog.SetData(entries, true);
#endif                
            
            return returnCatalog;
        }

        /// <summary>
        /// Remove all tags added by Genies addressable groups
        /// </summary>
        /// <param name="contentCatalogData"></param>
        public static ContentCatalogData RemoveGeniesTags(ContentCatalogData contentCatalogData)
        {
            var allTags = Enum.GetValues(typeof(AddressableTags)).Cast<AddressableTags>().Select(tag => tag.ToString()).ToList();
            var returnCatalog = new ContentCatalogData(ResourceManagerRuntimeData.kCatalogAddress);
            var entries = new List<ContentCatalogDataEntry>();
            var entryIndex = 0;
            var contentCatalogDataEntryDataDict = new Dictionary<string, ContentCatalogDataEntryData>();
            string[] currentPrefixes = null;
            SerializedType[] currentResourceTypes = null;
            List<ObjectInitializationData> objectInitializationDatas = null;
            var locator = contentCatalogData.CreateLocator();

            foreach (var internalId in contentCatalogData.InternalIds)
            foreach (var location in GetAllLocationsByInternalId(internalId, locator.Locations))
                AddLocationToDictionary(location.PrimaryKey, ref entryIndex, location,
                    ref contentCatalogDataEntryDataDict);

            foreach (var keyValuePair in locator.Locations)
                if (keyValuePair.Key.GetType() != typeof(int))
                    foreach (var location in keyValuePair.Value)
                        AddLocationToDictionary(keyValuePair.Key, ref entryIndex, location,
                            ref contentCatalogDataEntryDataDict);

            for (var i = 0; i < contentCatalogDataEntryDataDict.Keys.Count; i++)
            {
                var contentCatalogDataEntryData = GetEntryByIndex(i, contentCatalogDataEntryDataDict);
                var oldKeys = contentCatalogDataEntryData.keys.ToList();
                var newKeys = new List<object>();

                if (contentCatalogDataEntryData.provider == "UnityEngine.ResourceManagement.ResourceProviders.BundledAssetProvider")
                {
                    foreach (var key in oldKeys)
                    {
                        if (key is string)
                            if (allTags.Contains(key))
                                continue;

                        newKeys.Add(key);
                    }
                }

                foreach (var dataType in contentCatalogDataEntryData.types)
                {
                    entries.Add(new ContentCatalogDataEntry(dataType.type, contentCatalogDataEntryData.internalId,
                        contentCatalogDataEntryData.provider, newKeys,
                        contentCatalogDataEntryData.dependencies, dataType.data));
                }
            }

            //set all the catalog data
            returnCatalog.InstanceProviderData = contentCatalogData.InstanceProviderData;
            returnCatalog.SetFieldValue("m_InternalIdPrefixes", contentCatalogData.GetFieldValue<string[]>("m_InternalIdPrefixes"));
            returnCatalog.ResourceProviderData = contentCatalogData.ResourceProviderData;
            returnCatalog.SetFieldValue("m_resourceTypes", contentCatalogData.GetFieldValue<SerializedType[]>("m_resourceTypes"));
            returnCatalog.SceneProviderData = contentCatalogData.SceneProviderData;
            
#if  UNITY_6000_0_OR_NEWER
            returnCatalog.SetData(entries);
#else
            returnCatalog.SetData(entries, true);
#endif                
            
            return returnCatalog;
        }
#endif
        /// <summary>
        /// Save Content Catalog
        /// </summary>
        /// <param name="path"></param>
        /// <param name="contentCatalogData"></param>
        /// <param name="generateHash"></param>
        public static void Save(string path, ContentCatalogData contentCatalogData, bool generateHash = true)
        {
            var jsonText = JsonUtility.ToJson(contentCatalogData);
            File.WriteAllText(path, jsonText);

            if (generateHash)
            {
                var stringBase64 = contentCatalogData.GetFieldValue<string>("m_KeyDataString");
                var hash = GenerateSHA256Hash(stringBase64);
                var hashPath = $"{Path.GetDirectoryName(path)}/{Path.GetFileNameWithoutExtension(path)}.hash";
                File.WriteAllText(hashPath, hash);
            }
        }

        /// <summary>
        /// Get Byte Array for the Content Catalog
        /// </summary>
        /// <param name="contentCatalogData"></param>
        public static byte[] ToBytes(ContentCatalogData contentCatalogData)
        {
            var jsonText = JsonUtility.ToJson(contentCatalogData);
            using var memoryStream = new MemoryStream();

            using var writer = new StreamWriter(memoryStream, Encoding.UTF8);
            writer.Write(jsonText);
            writer.Flush();

            return memoryStream.ToArray();
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Get the hash for the catalog
        /// </summary>
        /// <param name="input"></param>
        private static string GenerateSHA256Hash(string input)
        {
            using SHA256 sha256Hash = SHA256.Create();
            // Compute full SHA-256 hash from the input string
            byte[] inputBytes = Encoding.UTF8.GetBytes(input);
            byte[] fullHashBytes = sha256Hash.ComputeHash(inputBytes);

            // Truncate the hash to 128 bits (16 bytes)
            byte[] truncatedHashBytes = new byte[16];
            Array.Copy(fullHashBytes, truncatedHashBytes, 16);

            // Convert truncated hash byte array to a string representation (hexadecimal)
            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < truncatedHashBytes.Length; i++)
            {
                builder.Append(truncatedHashBytes[i].ToString("x2"));
            }

            return builder.ToString();
        }

        /// <summary>
        /// Add new key to the private location dictionary
        /// </summary>
        /// <param name="dictKey"></param>
        /// <param name="index"></param>
        /// <param name="location"></param>
        /// <param name="contentCatalogDataEntryDataDict"></param>
        private static void AddLocationToDictionary(object dictKey, ref int index, IResourceLocation location, ref Dictionary<string, ContentCatalogDataEntryData> contentCatalogDataEntryDataDict)
        {
            Type type = location.GetType();

            var internalId = location.InternalId;
            internalId = internalId.Replace("DynBaseUrl", "{DynBaseUrl}");
            var provider = location.ProviderId;

            var deps = location.Dependencies;
            var key = location.PrimaryKey;

            if (contentCatalogDataEntryDataDict.ContainsKey(internalId))
            {
                var contentCatalogDataEntryData = contentCatalogDataEntryDataDict[internalId];
                if (!contentCatalogDataEntryData.keys.Contains(dictKey))
                {
                    contentCatalogDataEntryData.keys.Add(dictKey);
                }

                if (!contentCatalogDataEntryData.types.Exists(s => s.type == location.ResourceType))
                {
                    contentCatalogDataEntryData.types.Add(new ContentCatalogDataEntryData.DataType()
                    {
                        type = location.ResourceType,
                        data = location.Data
                    });
                }

                if (deps != null)
                {
                    foreach (var dep in deps)
                    {
                        if (!contentCatalogDataEntryData.dependencies.Contains(dep.PrimaryKey))
                        {
                            contentCatalogDataEntryData.dependencies.Add(dep.PrimaryKey);
                        }
                    }
                }
            }
            else
            {
                List<string> depKeys = null;

                if (deps != null)
                {
                    depKeys = new List<string>();
                    foreach (var dep in deps)
                    {
                        depKeys.Add(dep.PrimaryKey);
                    }
                }

                var contentCatalogDataEntryData = new ContentCatalogDataEntryData
                {
                    index = index,
                    types = new List<ContentCatalogDataEntryData.DataType>() {
                        new()
                        {
                            type = location.ResourceType,
                            data = location.Data
                        }},
                    internalId = internalId,
                    provider = provider,
                    keys = new List<object>() {key},
                    dependencies = depKeys,
                };

                contentCatalogDataEntryDataDict.Add(internalId, contentCatalogDataEntryData);

                index++;
            }
        }

        /// <summary>
        /// Get the location buy dictionary key (internalId)
        /// </summary>
        /// <param name="internalId"></param>
        /// <param name="dict"></param>
        private static IList<IResourceLocation> GetAllLocationsByInternalId(string internalId, Dictionary<object,IList<IResourceLocation>> dict)
        {
            internalId = internalId.Replace("{BaseUrl}", "BaseUrl");
            var locations = new List<IResourceLocation>();

            foreach (var keyValuePair in dict)
            {
                foreach (var location in keyValuePair.Value)
                {
                    if (location.InternalId == internalId)
                    {
                        locations.Add(location);
                    }
                }
            }

            return locations;
        }

        /// <summary>
        /// Get dictionary entry by ContentCatalogDataEntryData index (For resuming same order as original catalogs)
        /// </summary>
        /// <param name="index"></param>
        /// <param name="dict"></param>
        private static ContentCatalogDataEntryData GetEntryByIndex(int index, Dictionary<string, ContentCatalogDataEntryData> dict)
        {
            foreach (var keyValuePair in dict)
            {
                if (keyValuePair.Value?.index == index)
                {
                    return keyValuePair.Value;
                }
            }

            return null;
        }

        private static string[] CombineInternalPrefixes(string[] currentArr, IEnumerable<string> addArr)
        {
            var currentList = new List<string>();
            if (currentArr != null)
            {
                currentList = currentArr.ToList();
            }

            foreach (var add in addArr)
            {
                if (!currentList.Contains(add))
                {
                    currentList.Add(add);
                }
            }

            return currentList.ToArray();
        }

        private static List<ObjectInitializationData> CombineResourceProviderData(List<ObjectInitializationData> currentList, List<ObjectInitializationData> addList)
        {
            currentList ??= new List<ObjectInitializationData>();

            foreach (var add in addList)
            {
                if (!currentList.Exists(s => s.ObjectType.ClassName == add.ObjectType.ClassName))
                {
                    currentList.Add(add);
                }
            }

            return currentList;
        }

        private static SerializedType[] CombineResourceTypes(SerializedType[] currentArr, SerializedType[] addArr)
        {
            var currentList = new List<SerializedType>();
            if (currentArr != null)
            {
                currentList = currentArr.ToList();
            }

            foreach (var add in addArr)
            {
                if (!currentList.Exists(s => s.ClassName == add.ClassName))
                {
                    currentList.Add(add);
                }
            }

            return currentList.ToArray();
        }

        #endregion
    }
}
