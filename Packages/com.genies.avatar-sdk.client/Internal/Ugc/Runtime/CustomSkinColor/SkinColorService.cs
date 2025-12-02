using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Genies.Assets.Services;
using Genies.CrashReporting;
using Genies.DataRepositoryFramework;
using Genies.Inventory;
using Genies.Models;
using Genies.Refs;
using UnityEngine;

namespace Genies.Ugc.CustomSkin
{
    /// <summary>
    /// Service that handles returning custom and preset skin colors
    /// </summary>
    public class SkinColorService
    {
        private readonly IDataRepository<SkinColorData> _customSkinDataRepository;
        private readonly IDefaultInventoryService _defaultInventoryService;

        private UniTaskCompletionSource _initializationSource;
        private bool _isInitialized = false;

        private HandleCache<string, SkinColorData> _cachedHandles = new();
        private List<ColoredInventoryAsset> _presetColors;

        // Default backup skin color based on SkinMaterialData_skin0007
        private static readonly Color _defaultSkinColor = new Color(0.624f, 0.467f, 0.369f);

        public SkinColorService(
            IDataRepository<SkinColorData> customSkinDataRepository,
            IDefaultInventoryService defaultInventoryService
        )
        {
            _customSkinDataRepository = customSkinDataRepository;
            _defaultInventoryService = defaultInventoryService;
        }

        public async UniTask InitializeAsync()
        {
            if (_isInitialized)
            {
                return;
            }

            if (_initializationSource != null)
            {
                await _initializationSource.Task;
                return;
            }

            _initializationSource = new UniTaskCompletionSource();
            try
            {
                _presetColors = await _defaultInventoryService.GetDefaultColorPresets();
                // Filter for skin color presets - typically by category or subcategory
                _presetColors = _presetColors.Where(c => 
                    c.Category?.ToLower().Contains("skin") == true || 
                    c.SubCategories?.Any(s => s.ToLower().Contains("skin")) == true).ToList();
            }
            catch (Exception ex)
            {
                CrashReporter.LogError($"Failed to initialize SkinColorService presets: {ex.Message}");
                _presetColors = new List<ColoredInventoryAsset>();
            }

            _isInitialized = true;
            _initializationSource.TrySetResult();
            _initializationSource = null;
        }

        public async UniTask<List<string>> GetAllIdsAsync()
        {
            await InitializeAsync();

            var customIds = await GetAllCustomSkinIdsAsync();
            var presetIds = await GetAllPresetSkinIdsAsync();

            var allIds = new List<string>();
            allIds.AddRange(customIds);
            allIds.AddRange(presetIds);
            return allIds;
        }

        public async UniTask<List<string>> GetAllCustomSkinIdsAsync()
        {
            return await _customSkinDataRepository.GetIdsAsync();
        }

        public async UniTask<List<string>> GetAllPresetSkinIdsAsync()
        {
            await InitializeAsync();
            return _presetColors.Select(c => c.AssetId).ToList();
        }

        public async UniTask<bool> CheckIsCustomAsync(string id)
        {
            var customIds = await GetAllCustomSkinIdsAsync();
            return customIds.Contains(id);
        }

        public async UniTask<SkinColorData> CreateOrUpdateCustomSkin(SkinColorData customSkinColorData)
        {
            var ids = await GetAllCustomSkinIdsAsync();

            if (ids.Contains(customSkinColorData.Id))
            {
                return await _customSkinDataRepository.UpdateAsync(customSkinColorData);
            }

            return await _customSkinDataRepository.CreateAsync(customSkinColorData);
        }

        public async UniTask<Ref<SkinColorData>> GetSkinColorForIdAsync(string id)
        {
            await InitializeAsync();
            // check if the color was loaded before and has not been disposed yet
            if (_cachedHandles.TryGetNewReference(id, out Ref<SkinColorData> dataRef))
            {
                return dataRef;
            }

            dataRef = await LoadCustomSkinColor(id);
            if (!dataRef.IsAlive)
            {
                dataRef = await LoadPresetSkinColor(id);
            }

            _cachedHandles.CacheHandle(id, dataRef);

            return dataRef;
        }

        private async UniTask<Ref<SkinColorData>> LoadPresetSkinColor(string id)
        {
            try
            {
                await InitializeAsync();
                var presetColor = _presetColors.FirstOrDefault(c => c.AssetId == id);
                if (presetColor != null && presetColor.Colors != null && presetColor.Colors.Count > 0)
                {
                    return CreateRef.FromAny(new SkinColorData { Id = id, BaseColor = presetColor.Colors[0] });
                }
            }
            catch (Exception e)
            {
                CrashReporter.Log($"SkinColorService's LoadPresetSkinColor for id {id} can't be retrieved, returning default color. " +
                    $"Exception: {e}", LogSeverity.Error);
            }

            //Create a default skin color data to return
            var defaultSkinColorData = new SkinColorData() { Id = id, BaseColor = _defaultSkinColor };
            return CreateRef.FromAny(defaultSkinColorData);
        }

        public async UniTask DeleteCustomSkinAsync(string id)
        {
            await _customSkinDataRepository.DeleteAsync(id);
        }

        private async UniTask<Ref<SkinColorData>> LoadCustomSkinColor(string id)
        {
            if (await CheckIsCustomAsync(id))
            {
                var data = await _customSkinDataRepository.GetByIdAsync(id);
                if (data != null)
                {
                    return CreateRef.FromAny(data);
                }
            }
            return default;
        }

    }
}
