using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Genies.Assets.Services;
using Genies.Avatars;
using Genies.CrashReporting;
using Genies.DataRepositoryFramework;
using Genies.Inventory;
using Genies.Models;
using Genies.Refs;
using UnityEngine;

namespace Genies.Ugc.CustomHair
{
    /// <summary>
    /// Service that handles returning custom and preset hair colors.
    /// </summary>
    public class HairColorService
    {
        private readonly Shader _hairShader;
        private readonly IAssetsService _addressableAssetService;
        private readonly IDataRepository<CustomHairColorData> _customHairDataRepository;
        private readonly IDefaultInventoryService _defaultInventoryService;

        private UniTaskCompletionSource _initializationSource;
        private bool _isInitialized = false;

        private readonly HandleCache<string, Material> _cachedHandles = new();
        private List<ColoredInventoryAsset> _presetColors;

        private static readonly int s_hairColorBase = Shader.PropertyToID("_ColorBase");
        private static readonly int s_hairColorR = Shader.PropertyToID("_ColorR");
        private static readonly int s_hairColorG = Shader.PropertyToID("_ColorG");
        private static readonly int s_hairColorB = Shader.PropertyToID("_ColorB");

        public HairColorService(
            Shader hairShader,
            IAssetsService addressableAssetService,
            IDataRepository<CustomHairColorData> customHairDataRepository,
            IDefaultInventoryService defaultInventoryService
        )
        {
            _hairShader = hairShader;
            _addressableAssetService = addressableAssetService;
            _customHairDataRepository = customHairDataRepository;
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
                // Filter for hair color presets - typically by category or subcategory
                _presetColors = _presetColors.Where(c => 
                    c.Category?.ToLower().Contains("hair") == true || 
                    c.SubCategories?.Any(s => s.ToLower().Contains("hair")) == true).ToList();
            }
            catch (Exception ex)
            {
                CrashReporter.LogError($"Failed to initialize HairColorService presets: {ex.Message}");
                _presetColors = new List<ColoredInventoryAsset>();
            }

            _isInitialized = true;
            _initializationSource.TrySetResult();
            _initializationSource = null;
        }

        public async UniTask<List<string>> GetAllIdsAsync()
        {
            await InitializeAsync();
            var customIds = await GetAllCustomHairIdsAsync();
            var presetIds = await GetAllPresetHairIdsAsync();

            var allIds = new List<string>();
            allIds.AddRange(customIds);
            allIds.AddRange(presetIds);
            return allIds;
        }

        public async UniTask<List<string>> GetAllCustomHairIdsAsync()
        {
            await InitializeAsync();
            return await _customHairDataRepository.GetIdsAsync();
        }

        public async UniTask<List<string>> GetAllPresetHairIdsAsync()
        {
            await InitializeAsync();
            return _presetColors.Select(c => c.AssetId).ToList();
        }

        public async UniTask<bool> CheckIsCustomAsync(string id)
        {
            var customIds = await GetAllCustomHairIdsAsync();
            return customIds.Contains(id);
        }

        public async UniTask<CustomHairColorData> CreateOrUpdateCustomHair(CustomHairColorData customHairColorData)
        {
            await InitializeAsync();
            var ids = await GetAllCustomHairIdsAsync();

            if (ids.Contains(customHairColorData.Id))
            {
                return await _customHairDataRepository.UpdateAsync(customHairColorData);
            }

            return await _customHairDataRepository.CreateAsync(customHairColorData);
        }

        public async UniTask<Ref<Material>> GetHairMaterialForIdAsync(string id)
        {
            await InitializeAsync();
            // check if the material was loaded before and has not been disposed yet
            if (_cachedHandles.TryGetNewReference(id, out Ref<Material> materialRef))
            {
                return materialRef;
            }

            materialRef = await LoadCustomHairColorMaterial(id);
            if (!materialRef.IsAlive)
            {
                materialRef = await LoadPresetHairColorMaterial(id);
            }

            _cachedHandles.CacheHandle(id, materialRef);

            return materialRef;
        }

        private async UniTask<Ref<Material>> LoadPresetHairColorMaterial(string id)
        {
            await InitializeAsync();
            
            var presetColor = _presetColors.FirstOrDefault(c => c.AssetId == id);
            if (presetColor?.Colors != null && presetColor.Colors.Count >= 4)
            {
                var material = new Material(_hairShader);
                
                // Map the first 4 colors to the hair color shader properties
                material.SetColor(s_hairColorBase, presetColor.Colors[0]);
                material.SetColor(s_hairColorR, presetColor.Colors.Count > 1 ? presetColor.Colors[1] : Color.black);
                material.SetColor(s_hairColorG, presetColor.Colors.Count > 2 ? presetColor.Colors[2] : Color.black);
                material.SetColor(s_hairColorB, presetColor.Colors.Count > 3 ? presetColor.Colors[3] : Color.black);
                
                return CreateRef.FromUnityObject(material);
            }
            else if (presetColor?.Colors != null && presetColor.Colors.Count > 0)
            {
                // Fallback: if less than 4 colors, use the first color for base and black for others
                var material = new Material(_hairShader);
                material.SetColor(s_hairColorBase, presetColor.Colors[0]);
                material.SetColor(s_hairColorR, Color.black);
                material.SetColor(s_hairColorG, Color.black);
                material.SetColor(s_hairColorB, Color.black);
                
                return CreateRef.FromUnityObject(material);
            }

            return default;
        }

        private async UniTask<Ref<Material>> LoadCustomHairColorMaterial(string id)
        {
            await InitializeAsync();
            
            CustomHairColorData data = null;
            if (await CheckIsCustomAsync(id))
            {
                data = await _customHairDataRepository.GetByIdAsync(id);
                    
                // register to avatar embedded data every time we successfully load from data repository
                if (data is not null)
                {
                    AvatarEmbeddedData.SetData(id, data);
                }
            }
            
            // if we cannot fetch the data from the repository then fallback to the AvatarEmbeddedData
            if (data is null && !AvatarEmbeddedData.TryGetData(id, out data))
            {
                return default;
            }

            var material = new Material(_hairShader);

            material.SetColor(s_hairColorBase, data.ColorBase);
            material.SetColor(s_hairColorR,    data.ColorR);
            material.SetColor(s_hairColorG,    data.ColorG);
            material.SetColor(s_hairColorB,    data.ColorB);

            return CreateRef.FromUnityObject(material);
        }

        public async UniTask DeleteCustomHairAsync(string id)
        {
            await _customHairDataRepository.DeleteAsync(id);
        }

        public async UniTask DeleteAllCustomAsync()
        {
            await _customHairDataRepository.DeleteAllAsync();
        }

        public async UniTask<CustomHairColorData> CustomColorDataAsync(string id)
        {
            if (!await CheckIsCustomAsync(id))
            {
                return null;
            }

            return await _customHairDataRepository.GetByIdAsync(id);
        }
    }
}
