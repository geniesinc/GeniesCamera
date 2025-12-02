using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Genies.Assets.Services;
using Genies.Models;
using Genies.Refs;
using Genies.Shaders;
using Genies.Ugc;
using UnityEngine;

namespace Genies.Avatars.Context
{
    public sealed class UgcTemplateDataService : IUgcTemplateDataService
    {
        private const string BasicElementIdSuffix = "_lock01";

        // dependencies
        private readonly IAssetsService _assetsService;

        // state
        private readonly Dictionary<string, UniTaskCompletionSource<UgcTemplateData>> _templateDataCache;
        private readonly Dictionary<string, UniTaskCompletionSource<UgcTemplateSplitData>> _splitDataCache;
        private readonly Dictionary<string, UniTaskCompletionSource<UgcTemplateElementData>> _elementDataCache;

        public UgcTemplateDataService(IAssetsService assetsService)
        {
            _assetsService = assetsService;

            _templateDataCache = new Dictionary<string, UniTaskCompletionSource<UgcTemplateData>>();
            _splitDataCache = new Dictionary<string, UniTaskCompletionSource<UgcTemplateSplitData>>();
            _elementDataCache = new Dictionary<string, UniTaskCompletionSource<UgcTemplateElementData>>();
        }

        public UniTask<UgcTemplateData> FetchTemplateDataAsync(string templateId)
        {
            if (!ValidateTemplateId(ref templateId))
            {
                return UniTask.FromResult<UgcTemplateData>(null);
            }

            return CachedFetchDataAsync(templateId, () => NonCachedFetchTemplateDataAsync(templateId), _templateDataCache);
        }

        public UniTask<UgcTemplateSplitData> FetchSplitDataAsync(int splitIndex, string templateId)
        {
            if (!ValidateTemplateId(ref templateId))
            {
                return UniTask.FromResult<UgcTemplateSplitData>(null);
            }

            string splitId = $"{templateId}_split-{splitIndex}";
            return CachedFetchDataAsync(splitId, () => NonCachedFetchSplitDataAsync(splitIndex, templateId), _splitDataCache);
        }

        public UniTask<UgcTemplateElementData> FetchElementDataAsync(string elementId)
        {
            if (string.IsNullOrEmpty(elementId))
            {
                return UniTask.FromResult<UgcTemplateElementData>(null);
            }

            return CachedFetchDataAsync(elementId, () => NonCachedFetchElementDataAsync(elementId), _elementDataCache);
        }

        // the real FetchTemplateDataAsync method that does not include results caching
        private async UniTask<UgcTemplateData> NonCachedFetchTemplateDataAsync(string templateId, string lod = AssetLod.Default)
        {
            // since we just need the data and not the assets, we don't need to keep this ref alive when the method ends
            using Ref<UgcTemplate> templateRef = await _assetsService.LoadAssetAsync<UgcTemplate>(templateId, lod:lod);
            if (!templateRef.IsAlive || !templateRef.Item)
            {
                return null;
            }

            // the UgcTemplate class is from the content build and it contains a mixture of data and assets (even the thumbnail)
            // so we need to extract the real template data from it and create a UgcTemplateData instance which is the model used by the Avatars package
            UgcTemplate template = templateRef.Item;

            // fetch splits data and instantiate the template data
            List<UgcTemplateSplitData> splitsData = await FetchSplitsDataAsync(templateId, template);
            var templateData = new UgcTemplateData
            (
                templateId,
                template.Slot,
                template.Subcategory,
                template.IsBasic(),
                template.CollisionData.ToOutfitCollisionData(),
                splitsData
            );

            return templateData;
        }

        // the real FetchSplitDataAsync method that does not include results caching
        private async UniTask<UgcTemplateSplitData> NonCachedFetchSplitDataAsync(int splitIndex, string templateId, string lod = AssetLod.Default)
        {
            // since we just need the data and not the assets, we don't need to keep this ref alive when the method ends
            using Ref<UgcTemplate> templateRef = await _assetsService.LoadAssetAsync<UgcTemplate>(templateId, lod:lod);
            if (!templateRef.IsAlive || !templateRef.Item)
            {
                return null;
            }

            List<string> elementIds = GetSplitElementIds(splitIndex, templateRef.Item);

            // generate parallel fetching tasks for all elements
            IEnumerable<UniTask<UgcTemplateElementData>> tasks = elementIds.Select(FetchElementDataAsync);

            // await for all elements to be fetched and filter non-successful ones
            UgcTemplateElementData[] results = await UniTask.WhenAll(tasks);
            IEnumerable<UgcTemplateElementData> successfulResults = results.Where(elementData => elementData != null);

            // instantiate the split data object
            var splitData = new UgcTemplateSplitData
            (
                splitIndex,
                successfulResults
            );

            return splitData;
        }

        // the real FetchElementDataAsync method that does not include results caching
        private async UniTask<UgcTemplateElementData> NonCachedFetchElementDataAsync(string elementId, string lod = AssetLod.Default)
        {
            // unfortunately we don't have other way for fetching the element's region count
            // we don't need the assets loaded in the element container so the ref will be when ending the method
            using Ref<ElementContainer> containerRef = await _assetsService.LoadAssetAsync<ElementContainer>(elementId, lod:lod);
            if (!containerRef.IsAlive || !containerRef.Item)
            {
                return null;
            }

            var elementData = new UgcTemplateElementData
            (
                elementId,
                containerRef.Item.AvailableRegions,
                GeniesShaders.MegaShader.Version
            );

            return elementData;
        }

        private async UniTask<List<UgcTemplateSplitData>> FetchSplitsDataAsync(string templateId, UgcTemplate template)
        {
            int splitCount = template.IsBasic() ? 1 : template.ElementsPerSplit.Count;

            // generate parallel fetching tasks for all splits
            IEnumerable<UniTask<UgcTemplateSplitData>> tasks =
                Enumerable.Range(0, splitCount)
                    .Select(splitIndex => FetchSplitDataAsync(splitIndex, templateId));

            // await for all splits to be fetched and filter non-successful ones
            UgcTemplateSplitData[] results = await UniTask.WhenAll(tasks);
            IEnumerable<UgcTemplateSplitData> successfulResults = results.Where(splitData => splitData != null);

            var splitsData = new List<UgcTemplateSplitData>(successfulResults);
            return splitsData;
        }

        /// <summary>
        /// Given a split index and a <see cref="UgcTemplate"/> object it will return a list
        /// with the available element IDs for that split.
        /// </summary>
        public static List<string> GetSplitElementIds(int splitIndex, UgcTemplate template)
        {
            // compute available elements
            int elementsCount = template.ElementsPerSplit[splitIndex];
            var elementIds = new List<string>(elementsCount);

            // if there is lock available and we are on the lock split (splitIndex 0), then we only have one lock element
            if (template.IsLockAvailable && splitIndex == 0)
            {
                var elementId = $"{template.AssetId}{BasicElementIdSuffix}";
                elementIds.Add(elementId);
                return elementIds;
            }

            int splitNumber = splitIndex + 1;

            for (var elementNumber = 1; elementNumber <= elementsCount; ++elementNumber)
            {
                // numbers must be formatted with two digits (i.e.: number 2 would be "02")
                var split = splitNumber.ToString("D2");
                var element = elementNumber.ToString("D2");
                var elementId = $"{template.AssetId}_s{split}e{element}";

                elementIds.Add(elementId);
            }

            return elementIds;
        }

        private static bool ValidateTemplateId(ref string templateId)
        {
            if (string.IsNullOrEmpty(templateId))
            {
                return false;
            }

            if (!templateId.EndsWith("_template"))
            {
                templateId += "_template";
            }

            return true;
        }

        // handy method to avoid boiler plate for caching async fetched results
        private static async UniTask<T> CachedFetchDataAsync<T>(
            string dataId,
            Func<UniTask<T>> fetchFunc,
            Dictionary<string, UniTaskCompletionSource<T>> cache)
        {
            // if the result is cached, then skip fetching it again (ongoing fetching operations are cached too)
            if (cache.TryGetValue(dataId, out UniTaskCompletionSource<T> fetchingOperation))
            {
                T data = await fetchingOperation.Task;
                return data;
            }

            // start a fetching operation
            cache[dataId] = fetchingOperation = new UniTaskCompletionSource<T>();

            try
            {
                T data = await fetchFunc();
                fetchingOperation.TrySetResult(data);
                return data;
            }
            catch (Exception exception)
            {
                Debug.LogError($"[{nameof(UgcTemplateDataService)}] failed to fetch data with ID \"{dataId}\".\n{exception}");

                // remove the cached operation so next time we try again
                cache.Remove(dataId);
                fetchingOperation.TrySetResult(default);
                return default;
            }
        }
    }
}
