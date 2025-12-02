using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Genies.CrashReporting;
using Genies.DataRepositoryFramework;
using Genies.ServiceManagement;
using Genies.Utilities;
using UnityEngine;

namespace Genies.Avatars.Services.Flair
{
    public class FlairCustomColorPresetService: IFlairCustomColorPresetService
    {
        private IAvatarService _AvatarService => this.GetService<IAvatarService>();

        //cache the of current flair selected applied on the avatar
        private readonly Dictionary<string, FlairColorPreset> _cacheColorPresets = new Dictionary<string, FlairColorPreset>();
        private readonly IDataRepository<FlairColorPreset> _customFlairColorsDataRepository;
        private UniTaskCompletionSource _initializationSource;
        private bool _isInitialized = false;
        private Dictionary<string, List<FlairColorPreset>> _customColorsByCategory = new Dictionary<string, List<FlairColorPreset>>();
        private bool _usageSynced = false;
        public FlairCustomColorPresetService()
        {
        }
        public FlairCustomColorPresetService(IDataRepository<FlairColorPreset> customFlairColorsDataRepository)
        {
            _customFlairColorsDataRepository = customFlairColorsDataRepository;
            InitializeAsync().Forget();
        }

        private async UniTask InitializeAsync()
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
            _isInitialized = true;
            _initializationSource.TrySetResult();
            _initializationSource = null;
        }

        private async UniTask<List<string>> GetAllCustomizedFlairColors()
        {
            await InitializeAsync();
            return await _customFlairColorsDataRepository.GetIdsAsync();
        }

        public bool IsCustomColor(string guid)
        {
            foreach (var keyValuePair in _customColorsByCategory)
            {
                foreach (FlairColorPreset flairColorPreset in keyValuePair.Value)
                {
                    if (guid.Equals(flairColorPreset.Guid))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public async UniTask<List<FlairColorPreset>> TryGetAllCustomColorsByCategory(string category)
        {
            //return cache if the list of that category exist
            if (_customColorsByCategory?.Count > 0 && _customColorsByCategory.TryGetValue(category, out  _))
            {
                return _customColorsByCategory[category];
            }

            List<string> ids = await GetAllCustomizedFlairColors();

            //validate only the standard guids
            var validGuids = ids.Where(id => Guid.TryParse(id, out _)).ToList();
            var rawPresets = new List<FlairColorPreset>();

            for (int q = 0; q < validGuids.Count; q++)
            {
                var item = await _customFlairColorsDataRepository.GetByIdAsync(validGuids[q]);
                rawPresets.Add(item);
            }

            // Check if the current usage is already saved on backend
            if (!_usageSynced)
            {
                _usageSynced = true;
                // Check the current definition
                Naf.AvatarDefinition avatarDefinition = await _AvatarService.GetAvatarDefinitionAsync();
                FlairColorPreset avatarCurrentPreset = await TryGetCustomPresetFromAvatarsDefinition(category, avatarDefinition);
                if (avatarCurrentPreset != null)
                {
                    //if the current preset is a custom color applied on the avatar and not saved in cloud, we send the update
                    if (!validGuids.Contains(avatarCurrentPreset.Id) && Guid.TryParse(avatarCurrentPreset.Id, out Guid validGuid))
                    {
                        //making sure we're saving a valid guid
                        avatarCurrentPreset.Id = validGuid.ToString();
                        await _customFlairColorsDataRepository.CreateAsync(avatarCurrentPreset);
                        rawPresets.Add(avatarCurrentPreset);
                    }
                }
            }

            _customColorsByCategory = DictionaryUtils.ToDictionaryGraceful(rawPresets.GroupBy(m => m.FlairType.ToString()),
                grouping => grouping.Key,
                grouping => grouping.Select(m => m)
                    .ToList(), key => key != string.Empty);

            if (_customColorsByCategory.TryGetValue(category, out List<FlairColorPreset> flairs))
            {
                return flairs;
            }

            return new List<FlairColorPreset>();
        }

        public async UniTask<FlairColorPreset> GetCustomColorById(string id)
        {
            await InitializeAsync();
            
            if (string.IsNullOrEmpty(id))
            {
                return null;
            }
            
            try
            {
                // Try to get the preset directly from the data repository
                var preset = await _customFlairColorsDataRepository.GetByIdAsync(id);
                return preset;
            }
            catch (Exception ex)
            {
                CrashReporter.LogError($"Failed to get custom flair color by ID {id}: {ex.Message}");
                return null;
            }
        }

        public async UniTask DeleteCustomFlairColor(string guid, string category)
        {
            if (_customColorsByCategory.TryGetValue(category, out List<FlairColorPreset> currentCache))
            {
                foreach (FlairColorPreset flairColorPreset in currentCache)
                {
                    
                    if (flairColorPreset.Id.Equals(guid))
                    {
                        await _customFlairColorsDataRepository.DeleteAsync(flairColorPreset.Id);
                        InvalidateCacheByCategory(category);
                        break;
                    }
                }
            }
            else
            {
                CrashReporter.Log($"No cache found for category: {category}");
            }
        }

        public UniTask<FlairColorPreset> TryGetCustomPresetFromAvatarsDefinition(string category, Naf.AvatarDefinition avatarDefinition)
        {
            /*
            try
            {
                var currentPreset = string.Empty;

                switch (category)
                {
                    case UnifiedMaterialSlot.Eyebrows:
                        currentPreset = avatarDefinition.EyebrowColorPreset;
                        if (!currentPreset.StartsWith(IFlairCustomColorPresetService.CustomEyebrowPrefix))
                        {
                            return null;
                        }

                        //custom already exist
                        CacheSync(UnifiedMaterialSlot.Eyebrows,
                            IFlairCustomColorPresetService.CustomEyebrowPrefix,
                            avatarDefinition.EyebrowColorPreset,
                            TryParseColorList(avatarDefinition.EyebrowColors));

                        return _cacheColorPresets[category];

                    case UnifiedMaterialSlot.Eyelashes:
                        currentPreset = avatarDefinition.EyelashColorPreset;

                        if (!currentPreset.StartsWith(IFlairCustomColorPresetService.CustomEyelashPrefix))
                        {
                            return null;
                        }

                        //custom already exist
                        CacheSync(UnifiedMaterialSlot.Eyelashes,
                            IFlairCustomColorPresetService.CustomEyelashPrefix,
                            avatarDefinition.EyelashColorPreset,
                            TryParseColorList(avatarDefinition.EyelashColors));

                        return _cacheColorPresets[category];
                }
            }
            catch (Exception e)
            {
                CrashReporter.LogError($"Failed to process Custom Color Preset From AvatarsDefinition {e}");
            }
            */

            return UniTask.FromResult<FlairColorPreset>(null);
        }

        public async UniTask<FlairColorPreset> SaveOrCreateCustomPreset(string category, Naf.AvatarDefinition avatarDefinition, string id, Color[] colors)
        {
            List<string> ids = await GetAllCustomizedFlairColors();

            var prefix = string.Empty;

            switch (category)
            {
                case UnifiedMaterialSlot.Eyebrows:
                    prefix = IFlairCustomColorPresetService.CustomEyebrowPrefix;
                    break;
                case UnifiedMaterialSlot.Eyelashes:
                    prefix = IFlairCustomColorPresetService.CustomEyelashPrefix;
                    break;
            }

            //custom color that already exist
            if (ids.Contains(id))
            {
                var updatedFlair = new FlairColorPreset()
                {
                    FlairType = category,
                    Guid = $"{prefix}-{id}",
                    Id = id,
                    Colors = new []
                    {
                        colors[0],
                        colors[1],
                        colors[1],
                        colors[1],
                    },
                };

                if (_cacheColorPresets.TryGetValue(category, out FlairColorPreset _))
                {
                    _cacheColorPresets[category] = updatedFlair;
                }
                else
                {
                    _cacheColorPresets.Add(category, updatedFlair);
                }

                await _customFlairColorsDataRepository.UpdateAsync(_cacheColorPresets[category]);
            }
            // new custom color
            else
            {
                var _guid = Guid.NewGuid().ToString();
                var updatedFlair = new FlairColorPreset()
                {
                    FlairType = category,
                    Guid = $"{prefix}-{_guid}",
                    Id = _guid,
                    Colors = new []
                    {
                        colors[0],
                        colors[1],
                        colors[1],
                        colors[1],
                    },
                };

                if (_cacheColorPresets.TryGetValue(category, out FlairColorPreset _))
                {
                    _cacheColorPresets[category] = updatedFlair;
                }
                else
                {
                    _cacheColorPresets.Add(category, updatedFlair);
                }

                await _customFlairColorsDataRepository.CreateAsync(_cacheColorPresets[category]);
            }

            //save in the avatar definition
            switch (category)
            {
                case UnifiedMaterialSlot.Eyebrows:
                 //   avatarDefinition.EyebrowColorPreset = _cacheColorPresets[category].Guid;
                   // avatarDefinition.EyebrowColors = TrySerializeColorList(_cacheColorPresets[category].Colors);
                    break;
                case UnifiedMaterialSlot.Eyelashes:
                  //  avatarDefinition.EyelashColorPreset = _cacheColorPresets[category].Guid;
                   // avatarDefinition.EyelashColors = TrySerializeColorList(_cacheColorPresets[category].Colors);
                    break;
            }

            InvalidateCacheByCategory(category);
            return _cacheColorPresets[category];
        }

        private void CacheSync(string category, string prefix, string currentGuid, Color[] colors)
        {
            if (!_cacheColorPresets.TryGetValue(category, out FlairColorPreset _))
            {
                var _guid = Guid.NewGuid().ToString();
                var _id = string.IsNullOrEmpty(currentGuid) ? _guid : currentGuid.Replace($"{prefix}-", "");
                var _customGuid = string.IsNullOrEmpty(currentGuid) ? $"{prefix}-{_guid}" : currentGuid;

                _cacheColorPresets.Add(category, new FlairColorPreset()
                {
                    FlairType = category,
                    Id = _id,
                    Guid = _customGuid,
                    Colors = colors
                });
            }
        }

        private Color[] TryParseColorList(string[] colorHexList)
        {
            if (colorHexList == null)
            {
                return new []{Color.black,Color.black,Color.black,Color.black };
            }

            var colors = new Color[colorHexList.Length];

            for (var i = 0; i < colorHexList.Length; i++)
            {
                ColorUtility.TryParseHtmlString(colorHexList[i], out Color rawColor);
                colors[i] = rawColor;
            }

            return colors;
        }

        private string[] TrySerializeColorList(Color[] colorList)
        {
            if (colorList == null)
            {
                return TrySerializeColorList(new []{Color.black,Color.black,Color.black,Color.black });
            }

            var colors = new string[colorList.Length];

            for (var i = 0; i < colorList.Length; i++)
            {
                colors[i] = $"#{ColorUtility.ToHtmlStringRGBA(colorList[i])}";
            }

            return colors;
        }

        private void InvalidateCacheByCategory(string category)
        {
            if(_customColorsByCategory.TryGetValue(category, out _))
            {
                _customColorsByCategory.Remove(category);
            }
        }

        public void RefreshCacheForCategory(string category)
        {
            InvalidateCacheByCategory(category);
        }
    }
}
