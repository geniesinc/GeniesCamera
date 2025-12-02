using System;
using System.Collections.Generic;
using System.Linq;
using Genies.ServiceManagement;
using Cysharp.Threading.Tasks;

namespace Genies.Inventory.UIData
{
    /// <summary>
    /// UI Data Provider Configurations for Inventory V2 API
    ///
    /// This class contains two types of configurations:
    ///
    /// 1. ASSET CONFIGURATIONS - For discrete inventory items (wearables, avatars, decor, etc.)
    ///    - Use BasicInventoryUiData for most assets
    ///    - Use SimpleColorUiData for assets with color display
    ///
    /// 2. COLOR PRESET CONFIGURATIONS - For color customization (flair, skin, hair, makeup)
    ///    - Use ColorPresetUiData - replaces ColorPresetFromCMSUiData
    ///    - Includes material generation for 4-color shader compatibility
    ///    - Filtered by category to get specific color types (FlairEyebrow, FlairEyelash, etc.)
    ///
    /// NOTES:
    /// - Flair color presets: inner and middle color, border is -1.81f
    /// - Color presets use "Custom/FourColorSwatch" shader for compatibility with existing UI
    /// - Filtering is done by Category and SubCategories from inventory data
    /// </summary>


    public static class UIDataProviderConfigs
    {
        private const string CategoryFlairEyebrow = "flaireyebrow";
        private const string CategoryFlairEyelash = "flaireyelash";

        private static InventoryUIDataProviderConfig<ColorTaggedInventoryAsset, BasicInventoryUiData>
            CreateWearablesConfig(
                Func<IDefaultInventoryService, int?, List<string>, UniTask<List<ColorTaggedInventoryAsset>>> dataFetcher,
                Func<IDefaultInventoryService, UniTask<List<ColorTaggedInventoryAsset>>> loadMoreFetcher)
        {
            return new InventoryUIDataProviderConfig<ColorTaggedInventoryAsset, BasicInventoryUiData>
            {
                DataGetter = async (category, subcategory) =>
                {
                    var service = ServiceManager.Get<IDefaultInventoryService>();
                    // Pass category as a list to the service
                    List<string> categories = !string.IsNullOrEmpty(category) ? new List<string> { category } : null;
                    var data = await dataFetcher(service, InventoryConstants.DefaultPageSize, categories);
                    return new PagedResult<ColorTaggedInventoryAsset>
                    {
                        Data = data,
                        NextCursor = null // NextCursor will be managed by service directly
                    };
                },
                LoadMoreGetter = async () =>
                {
                    var service = ServiceManager.Get<IDefaultInventoryService>();
                    var data = await loadMoreFetcher(service);
                    return new PagedResult<ColorTaggedInventoryAsset>
                    {
                        Data = data,
                        NextCursor = null // NextCursor will be managed by service directly
                    };
                },
                CategorySelector = asset => asset.Category,
                SubcategorySelector = asset => asset.SubCategories?.FirstOrDefault(),
                Sort = asset => asset.Order,
                DataConverter = asset => new BasicInventoryUiData(
                    asset.AssetId,
                    asset.Name,
                    asset.Category,
                    asset.SubCategories?.FirstOrDefault(),
                    asset.Order,
                    null,
                    false)
            };
        }

        public static InventoryUIDataProviderConfig<ColorTaggedInventoryAsset, BasicInventoryUiData> AllWearablesConfig =
            CreateWearablesConfig((s, limit, categories) => s.GetAllWearables(), s => s.GetAllWearables());

        public static InventoryUIDataProviderConfig<ColorTaggedInventoryAsset, BasicInventoryUiData> DefaultWearablesConfig =
            CreateWearablesConfig((s, limit, categories) => s.GetDefaultWearables(limit, categories), s => s.LoadMoreDefaultWearables());

        public static InventoryUIDataProviderConfig<ColorTaggedInventoryAsset, BasicInventoryUiData> UserWearablesConfig =
            CreateWearablesConfig((s, limit, categories) => s.GetUserWearables(limit, categories), s => s.LoadMoreUserWearables());

        public static InventoryUIDataProviderConfig<ColorTaggedInventoryAsset, BasicInventoryUiData> DefaultAvatarConfig =
            new()
            {
                DataGetter = async (category, subcategory) =>
                {
                    var service = ServiceManager.Get<IDefaultInventoryService>();
                    List<string> categories = !string.IsNullOrEmpty(category) ? new List<string> { category } : null;
                    List<ColorTaggedInventoryAsset> data = await service.GetDefaultAvatar(InventoryConstants.DefaultPageSize, categories);
                    return new PagedResult<ColorTaggedInventoryAsset>
                    {
                        Data = data,
                        NextCursor = null
                    };
                },
                LoadMoreGetter = async () =>
                {
                    var service = ServiceManager.Get<IDefaultInventoryService>();
                    List<ColorTaggedInventoryAsset> data = await service.LoadMoreDefaultAvatar();
                    return new PagedResult<ColorTaggedInventoryAsset>
                    {
                        Data = data,
                        NextCursor = null
                    };
                },
                CategorySelector = asset => asset.Category,
                SubcategorySelector = asset => asset.SubCategories?.FirstOrDefault(),
                Sort = asset => asset.Order,
                DataConverter = asset => new BasicInventoryUiData(
                    asset.AssetId,
                    asset.Name,
                    asset.Category,
                    asset.SubCategories?.FirstOrDefault(),
                    asset.Order,
                    null,
                    false)
            };

        public static InventoryUIDataProviderConfig<DefaultAvatarBaseAsset, BasicInventoryUiData> DefaultAvatarBaseConfig =
            new()
            {
                DataGetter = async (category, subcategory) =>
                {
                    var service = ServiceManager.Get<IDefaultInventoryService>();
                    List<string> categories = !string.IsNullOrEmpty(category) ? new List<string> { category } : null;
                    List<DefaultAvatarBaseAsset> data = await service.GetDefaultAvatarBaseData(InventoryConstants.DefaultPageSize, categories);
                    return new PagedResult<DefaultAvatarBaseAsset>
                    {
                        Data = data,
                        NextCursor = null
                    };
                },
                LoadMoreGetter = async () =>
                {
                    var service = ServiceManager.Get<IDefaultInventoryService>();
                    List<DefaultAvatarBaseAsset> data = await service.LoadMoreDefaultAvatarBaseData();
                    return new PagedResult<DefaultAvatarBaseAsset>
                    {
                        Data = data,
                        NextCursor = null
                    };
                },
                CategorySelector = asset => asset.Category,
                SubcategorySelector = asset => asset.SubCategories?.FirstOrDefault(),
                Sort = asset => asset.Order,
                DataConverter = asset => new BasicInventoryUiData(
                    asset.AssetId,
                    asset.Name,
                    asset.Category,
                    asset.SubCategories?.FirstOrDefault(),
                    asset.Order,
                    null,
                    asset.SubCategories?.FirstOrDefault() == "eyes")
            };

        public static InventoryUIDataProviderConfig<ColoredInventoryAsset, SimpleColorUiData> DefaultAvatarEyesConfig =
            new()
            {
                DataGetter = async (category, subcategory) =>
                {
                    var service = ServiceManager.Get<IDefaultInventoryService>();
                    List<string> categories = !string.IsNullOrEmpty(category) ? new List<string> { category } : null;
                    List<ColoredInventoryAsset> data = await service.GetDefaultAvatarEyes(InventoryConstants.DefaultPageSize, categories);
                    return new PagedResult<ColoredInventoryAsset>
                    {
                        Data = data,
                        NextCursor = null
                    };
                },
                LoadMoreGetter = async () =>
                {
                    var service = ServiceManager.Get<IDefaultInventoryService>();
                    List<ColoredInventoryAsset> data = await service.LoadMoreDefaultAvatarEyes();
                    return new PagedResult<ColoredInventoryAsset>
                    {
                        Data = data,
                        NextCursor = null
                    };
                },
                CategorySelector = asset => asset.Category,
                SubcategorySelector = asset => null,
                Sort = asset => asset.Order,
                DataConverter = asset => new SimpleColorUiData(
                    asset.AssetId,
                    asset.Name,
                    asset.Category,
                    null,
                    asset.Order,
                    null,
                    true,
                    asset.Colors[0],
                    asset.Colors[1],
                    asset.Colors[1],
                    0.0f)
            };

        public static InventoryUIDataProviderConfig<DefaultAnimationLibraryAsset, BasicInventoryUiData> DefaultAnimationLibraryConfig =
            new()
            {
                DataGetter = async (category, subcategory) =>
                {
                    var service = ServiceManager.Get<IDefaultInventoryService>();
                    List<string> categories = !string.IsNullOrEmpty(category) ? new List<string> { category } : null;
                    List<DefaultAnimationLibraryAsset> data = await service.GetDefaultAnimationLibrary(InventoryConstants.DefaultPageSize, categories);
                    return new PagedResult<DefaultAnimationLibraryAsset>
                    {
                        Data = data,
                        NextCursor = null
                    };
                },
                LoadMoreGetter = async () =>
                {
                    var service = ServiceManager.Get<IDefaultInventoryService>();
                    List<DefaultAnimationLibraryAsset> data = await service.LoadMoreDefaultAnimationLibrary();
                    return new PagedResult<DefaultAnimationLibraryAsset>
                    {
                        Data = data,
                        NextCursor = null
                    };
                },
                CategorySelector = asset => asset.Category,
                SubcategorySelector = asset => asset.MoodsTag,
                Sort = asset => asset.Order,
                DataConverter = asset => new BasicInventoryUiData(
                    asset.AssetId,
                    asset.Name,
                    asset.Category,
                    asset.MoodsTag,
                    asset.Order,
                    null,
                    false)
            };

        public static InventoryUIDataProviderConfig<DefaultInventoryAsset, BasicInventoryUiData> DefaultAvatarFlairConfig =
            new()
            {
                DataGetter = async (category, subcategory) =>
                {
                    var service = ServiceManager.Get<IDefaultInventoryService>();
                    List<string> categories = !string.IsNullOrEmpty(category) ? new List<string> { category } : null;
                    List<DefaultInventoryAsset> data = await service.GetDefaultAvatarFlair(InventoryConstants.DefaultPageSize, categories);
                    return new PagedResult<DefaultInventoryAsset>
                    {
                        Data = data,
                        NextCursor = null
                    };
                },
                LoadMoreGetter = async () =>
                {
                    var service = ServiceManager.Get<IDefaultInventoryService>();
                    List<DefaultInventoryAsset> data = await service.LoadMoreDefaultAvatarFlair();
                    return new PagedResult<DefaultInventoryAsset>
                    {
                        Data = data,
                        NextCursor = null
                    };
                },
                CategorySelector = asset => asset.Category,
                SubcategorySelector = null,
                Sort = asset => asset.Order,
                DataConverter = asset => new BasicInventoryUiData(
                    asset.AssetId,
                    asset.Name,
                    asset.Category,
                    null,
                    asset.Order,
                    null,
                    false)
            };

        public static InventoryUIDataProviderConfig<DefaultInventoryAsset, BasicInventoryUiData> DefaultAvatarMakeupConfig =
            new()
            {
                DataGetter = async (category, subcategory) =>
                {
                    var service = ServiceManager.Get<IDefaultInventoryService>();
                    List<string> categories = !string.IsNullOrEmpty(category) ? new List<string> { category } : null;
                    List<DefaultInventoryAsset> data = await service.GetDefaultAvatarMakeup(InventoryConstants.DefaultPageSize, categories);
                    return new PagedResult<DefaultInventoryAsset>
                    {
                        Data = data,
                        NextCursor = null
                    };
                },
                LoadMoreGetter = async () =>
                {
                    var service = ServiceManager.Get<IDefaultInventoryService>();
                    List<DefaultInventoryAsset> data = await service.LoadMoreDefaultAvatarMakeup();
                    return new PagedResult<DefaultInventoryAsset>
                    {
                        Data = data,
                        NextCursor = null
                    };
                },
                CategorySelector = asset => asset.Category,
                SubcategorySelector = asset => asset.SubCategories?.FirstOrDefault(),
                Sort = asset => asset.Order,
                DataConverter = asset => new BasicInventoryUiData(
                    asset.AssetId,
                    asset.Name,
                    asset.Category,
                    asset.SubCategories?.FirstOrDefault(),
                    asset.Order,
                    null,
                    true)
            };

        public static InventoryUIDataProviderConfig<ColoredInventoryAsset, SimpleColorUiData> DefaultColorPresetsConfig =
            new()
            {
                DataGetter = async (category, subcategory) =>
                {
                    var service = ServiceManager.Get<IDefaultInventoryService>();
                    List<ColoredInventoryAsset> data = await service.GetDefaultColorPresets(InventoryConstants.LargePageSize);
                    return new PagedResult<ColoredInventoryAsset>
                    {
                        Data = data,
                        NextCursor = null
                    };
                },
                LoadMoreGetter = async () =>
                {
                    var service = ServiceManager.Get<IDefaultInventoryService>();
                    List<ColoredInventoryAsset> data = await service.LoadMoreDefaultColorPresets();
                    return new PagedResult<ColoredInventoryAsset>
                    {
                        Data = data,
                        NextCursor = null
                    };
                },
                CategorySelector = asset => asset.Category,
                SubcategorySelector = asset => asset.SubCategories?.FirstOrDefault(),
                Sort = asset => asset.Order,
                DataConverter = asset => new SimpleColorUiData(
                    asset.AssetId,
                    asset.Name,
                    asset.Category,
                    asset.SubCategories?.FirstOrDefault(),
                    asset.Order,
                    null,
                    false,
                    asset.Colors?.Count > 0 ? asset.Colors[0] : UnityEngine.Color.white,
                    asset.Colors?.Count > 1 ? asset.Colors[1] : UnityEngine.Color.white,
                    asset.Colors?.Count > 2 ? asset.Colors[2] : UnityEngine.Color.white,
                    0.0f)
            };

        public static InventoryUIDataProviderConfig<ColorTaggedInventoryAsset, BasicInventoryUiData> DefaultDecorConfig =
            new()
            {
                DataGetter = async (category, subcategory) =>
                {
                    var service = ServiceManager.Get<IDefaultInventoryService>();
                    List<string> categories = !string.IsNullOrEmpty(category) ? new List<string> { category } : null;
                    List<ColorTaggedInventoryAsset> data = await service.GetDefaultDecor(InventoryConstants.DefaultPageSize, categories);
                    return new PagedResult<ColorTaggedInventoryAsset>
                    {
                        Data = data,
                        NextCursor = null
                    };
                },
                LoadMoreGetter = async () =>
                {
                    var service = ServiceManager.Get<IDefaultInventoryService>();
                    List<ColorTaggedInventoryAsset> data = await service.LoadMoreDefaultDecor();
                    return new PagedResult<ColorTaggedInventoryAsset>
                    {
                        Data = data,
                        NextCursor = null
                    };
                },
                CategorySelector = asset => asset.Category,
                SubcategorySelector = asset => asset.SubCategories?.FirstOrDefault(),
                Sort = asset => asset.Order,
                DataConverter = asset => new BasicInventoryUiData(
                    asset.AssetId,
                    asset.Name,
                    asset.Category,
                    asset.SubCategories?.FirstOrDefault(),
                    asset.Order,
                    null,
                    false)
            };

        public static InventoryUIDataProviderConfig<DefaultInventoryAsset, BasicInventoryUiData> DefaultImageLibraryConfig =
            new()
            {
                DataGetter = async (category, subcategory) =>
                {
                    var service = ServiceManager.Get<IDefaultInventoryService>();
                    List<string> categories = !string.IsNullOrEmpty(category) ? new List<string> { category } : null;
                    List<DefaultInventoryAsset> data = await service.GetDefaultImageLibrary(InventoryConstants.DefaultPageSize, categories);
                    return new PagedResult<DefaultInventoryAsset>
                    {
                        Data = data,
                        NextCursor = null
                    };
                },
                LoadMoreGetter = async () =>
                {
                    var service = ServiceManager.Get<IDefaultInventoryService>();
                    List<DefaultInventoryAsset> data = await service.LoadMoreDefaultImageLibrary();
                    return new PagedResult<DefaultInventoryAsset>
                    {
                        Data = data,
                        NextCursor = null
                    };
                },
                CategorySelector = asset => asset.Category,
                SubcategorySelector = asset => asset.SubCategories?.FirstOrDefault(),
                Sort = asset => asset.Order,
                DataConverter = asset => new BasicInventoryUiData(
                    asset.AssetId,
                    asset.Name,
                    asset.Category,
                    asset.SubCategories?.FirstOrDefault(),
                    asset.Order,
                    null,
                    false)
            };

        public static InventoryUIDataProviderConfig<ColorTaggedInventoryAsset, BasicInventoryUiData> DefaultModelLibraryConfig =
            new()
            {
                DataGetter = async (category, subcategory) =>
                {
                    var service = ServiceManager.Get<IDefaultInventoryService>();
                    List<string> categories = !string.IsNullOrEmpty(category) ? new List<string> { category } : null;
                    List<ColorTaggedInventoryAsset> data = await service.GetDefaultModelLibrary(InventoryConstants.DefaultPageSize, categories);
                    return new PagedResult<ColorTaggedInventoryAsset>
                    {
                        Data = data,
                        NextCursor = null
                    };
                },
                LoadMoreGetter = async () =>
                {
                    var service = ServiceManager.Get<IDefaultInventoryService>();
                    List<ColorTaggedInventoryAsset> data = await service.LoadMoreDefaultModelLibrary();
                    return new PagedResult<ColorTaggedInventoryAsset>
                    {
                        Data = data,
                        NextCursor = null
                    };
                },
                CategorySelector = asset => asset.Category,
                SubcategorySelector = asset => asset.SubCategories?.FirstOrDefault(),
                Sort = asset => asset.Order,
                DataConverter = asset => new BasicInventoryUiData(
                    asset.AssetId,
                    asset.Name,
                    asset.Category,
                    asset.SubCategories?.FirstOrDefault(),
                    asset.Order,
                    null,
                    false)
            };

        // ===== COLOR PRESET CONFIGURATIONS =====
        // These are specifically for color presets used in flair, skin, hair, makeup customization

        /// <summary>
        /// Color presets for flair eyebrows - replaces CMS-based color preset provider
        /// Uses GradientColorUiData which provides BOTH:
        /// • Material property: 4-color gradient using "Custom/FourColorSwatch" (for color preset data)
        /// • GetSwatchMaterial(): Simple UI swatch using "Genies/ColorPresetIcon" (for thumbnail display)
        /// </summary>
        public static InventoryUIDataProviderConfig<ColoredInventoryAsset, GradientColorUiData> FlairEyebrowColorPresetsConfig =
            new()
            {
                DataGetter = async (category, subcategory) =>
                {
                    var service = ServiceManager.Get<IDefaultInventoryService>();
                    List<string> categories = !string.IsNullOrEmpty(category) ? new List<string> { category } : null;
                    List<ColoredInventoryAsset> data = await service.GetDefaultColorPresets(InventoryConstants.DefaultPageSize, categories);
                    // Filter for eyebrow color presets
                    var eyebrowPresets = data.Where(p => p.Category?.Equals(CategoryFlairEyebrow, StringComparison.OrdinalIgnoreCase) == true).ToList();
                    return new PagedResult<ColoredInventoryAsset>
                    {
                        Data = data,
                        NextCursor = null
                    };
                },
                CategorySelector = asset => CategoryFlairEyebrow,
                SubcategorySelector = asset => asset.SubCategories?.FirstOrDefault(),
                Sort = asset => asset.Order,
                DataConverter = asset => new GradientColorUiData(
                    asset.AssetId,
                    asset.Name,
                    CategoryFlairEyebrow,
                    asset.SubCategories?.FirstOrDefault(),
                    asset.Order,
                    false,
                    asset.Colors?.Count > 0 ? asset.Colors[0] : UnityEngine.Color.white, // Base
                    asset.Colors?.Count > 1 ? asset.Colors[1] : asset.Colors?.Count > 0 ? asset.Colors[0] : UnityEngine.Color.white, // R
                    asset.Colors?.Count > 2 ? asset.Colors[2] : asset.Colors?.Count > 0 ? asset.Colors[0] : UnityEngine.Color.white, // G
                    asset.Colors?.Count > 3 ? asset.Colors[3] : asset.Colors?.Count > 0 ? asset.Colors[0] : UnityEngine.Color.white) // B
            };

        /// <summary>
        /// Color presets for flair eyelashes - replaces CMS-based color preset provider
        /// Uses GradientColorUiData which provides BOTH:
        /// • Material property: 4-color gradient using "Custom/FourColorSwatch" (for color preset data)
        /// • GetSwatchMaterial(): Simple UI swatch using "Genies/ColorPresetIcon" (for thumbnail display)
        /// </summary>
        public static InventoryUIDataProviderConfig<ColoredInventoryAsset, GradientColorUiData> FlairEyelashColorPresetsConfig =
            new()
            {
                DataGetter = async (category, subcategory) =>
                {
                    var service = ServiceManager.Get<IDefaultInventoryService>();
                    List<string> categories = !string.IsNullOrEmpty(category) ? new List<string> { category } : null;
                    List<ColoredInventoryAsset> data = await service.GetDefaultColorPresets(InventoryConstants.DefaultPageSize, categories);
                    // Filter for eyelash color presets
                    var eyelashPresets = data.Where(p => p.Category?.Equals(CategoryFlairEyelash, StringComparison.OrdinalIgnoreCase) == true).ToList();
                    return new PagedResult<ColoredInventoryAsset>
                    {
                        Data = data,
                        NextCursor = null
                    };
                },
                CategorySelector = asset => CategoryFlairEyelash,
                SubcategorySelector = asset => asset.SubCategories?.FirstOrDefault(),
                Sort = asset => asset.Order,
                DataConverter = asset => new GradientColorUiData(
                    asset.AssetId,
                    asset.Name,
                    CategoryFlairEyelash,
                    asset.SubCategories?.FirstOrDefault(),
                    asset.Order,
                    false,
                    asset.Colors?.Count > 0 ? asset.Colors[0] : UnityEngine.Color.white, // Base
                    asset.Colors?.Count > 1 ? asset.Colors[1] : asset.Colors?.Count > 0 ? asset.Colors[0] : UnityEngine.Color.white, // R
                    asset.Colors?.Count > 2 ? asset.Colors[2] : asset.Colors?.Count > 0 ? asset.Colors[0] : UnityEngine.Color.white, // G
                    asset.Colors?.Count > 3 ? asset.Colors[3] : asset.Colors?.Count > 0 ? asset.Colors[0] : UnityEngine.Color.white) // B
            };

        /// <summary>
        /// Color presets for skin customization - replaces CMS-based color preset provider
        /// Uses SimpleColorUiData for simple 3-color display (inner, middle, outer)
        /// Note: If skin also needs complex gradients + simple swatches, change to GradientColorUiData
        /// </summary>
        public static InventoryUIDataProviderConfig<ColoredInventoryAsset, SimpleColorUiData> SkinColorPresetsConfig =
            new()
            {
                DataGetter = async (category, subcategory) =>
                {
                    var service = ServiceManager.Get<IDefaultInventoryService>();
                    List<ColoredInventoryAsset> data = await service.GetDefaultColorPresets(InventoryConstants.DefaultPageSize, new List<string>{ "skin" });
                    return new PagedResult<ColoredInventoryAsset>
                    {
                        Data = data,
                        NextCursor = null
                    };
                },
                CategorySelector = asset => "skin",
                SubcategorySelector = asset => asset.SubCategories?.FirstOrDefault(),
                Sort = asset => asset.Order,
                DataConverter = asset => new SimpleColorUiData(
                    asset.AssetId,
                    asset.Name,
                    "skin",
                    asset.SubCategories?.FirstOrDefault(),
                    asset.Order,
                    null,
                    false,
                    asset.Colors?.Count > 0 ? asset.Colors[0] : UnityEngine.Color.white, // Inner (main skin tone)
                    asset.Colors?.Count > 1 ? asset.Colors[1] : asset.Colors?.Count > 0 ? asset.Colors[0] : UnityEngine.Color.white, // Middle
                    asset.Colors?.Count > 2 ? asset.Colors[2] : asset.Colors?.Count > 0 ? asset.Colors[0] : UnityEngine.Color.white, // Outer
                    0.0f) // Border value
            };

        /// <summary>
        /// Color presets for hair customization - replaces CMS-based color preset provider
        /// Uses GradientColorUiData which provides BOTH:
        /// • Material property: 4-color gradient using "Custom/FourColorSwatch" (for color preset data)
        /// • GetSwatchMaterial(): Simple UI swatch using "Genies/ColorPresetIcon" (for thumbnail display)
        /// </summary>
        public static InventoryUIDataProviderConfig<ColoredInventoryAsset, GradientColorUiData> HairColorPresetsConfig =
            new()
            {
                DataGetter = async (category, subcategory) =>
                {
                    var service = ServiceManager.Get<IDefaultInventoryService>();
                    List<ColoredInventoryAsset> data = await service.GetDefaultColorPresets(InventoryConstants.DefaultPageSize, new List<string>{ "hair" });
                    return new PagedResult<ColoredInventoryAsset>
                    {
                        Data = data,
                        NextCursor = null
                    };
                },
                CategorySelector = asset => "hair",
                SubcategorySelector = asset => asset.SubCategories?.FirstOrDefault(),
                Sort = asset => asset.Order,
                DataConverter = asset => new GradientColorUiData(
                    asset.AssetId,
                    asset.Name,
                    "hair",
                    asset.SubCategories?.FirstOrDefault(),
                    asset.Order,
                    false,
                    asset.Colors?.Count > 0 ? asset.Colors[0] : UnityEngine.Color.white, // Base
                    asset.Colors?.Count > 1 ? asset.Colors[1] : asset.Colors?.Count > 0 ? asset.Colors[0] : UnityEngine.Color.white, // R
                    asset.Colors?.Count > 2 ? asset.Colors[2] : asset.Colors?.Count > 0 ? asset.Colors[0] : UnityEngine.Color.white, // G
                    asset.Colors?.Count > 3 ? asset.Colors[3] : asset.Colors?.Count > 0 ? asset.Colors[0] : UnityEngine.Color.white) // B
            };

        /// <summary>
        /// Color presets for makeup customization - replaces CMS-based color preset provider
        /// Uses GradientColorUiData which provides BOTH:
        /// • Material property: 4-color gradient using "Custom/FourColorSwatch" (for color preset data)
        /// • GetSwatchMaterial(): Simple UI swatch using "Genies/ColorPresetIcon" (for thumbnail display)
        /// </summary>
        public static InventoryUIDataProviderConfig<ColoredInventoryAsset, GradientColorUiData> MakeupColorPresetsConfig =
            new()
            {
                DataGetter = async (category, subcategory) =>
                {
                    var service = ServiceManager.Get<IDefaultInventoryService>();
                    List<string> categories = !string.IsNullOrEmpty(category) ? new List<string> { category } : null;
                    List<ColoredInventoryAsset> data = await service.GetDefaultColorPresets(InventoryConstants.DefaultPageSize, categories);
                    return new PagedResult<ColoredInventoryAsset>
                    {
                        Data = data,
                        NextCursor = null
                    };
                },
                CategorySelector = asset => "makeup",
                SubcategorySelector = asset => asset.SubCategories?.FirstOrDefault(),
                Sort = asset => asset.Order,
                DataConverter = asset => new GradientColorUiData(
                    asset.AssetId,
                    asset.Name,
                    "makeup",
                    asset.SubCategories?.FirstOrDefault(),
                    asset.Order,
                    false,
                    asset.Colors?.Count > 0 ? asset.Colors[0] : UnityEngine.Color.white, // Base
                    asset.Colors?.Count > 1 ? asset.Colors[1] : asset.Colors?.Count > 0 ? asset.Colors[0] : UnityEngine.Color.white, // R
                    asset.Colors?.Count > 2 ? asset.Colors[2] : asset.Colors?.Count > 0 ? asset.Colors[0] : UnityEngine.Color.white, // G
                    asset.Colors?.Count > 3 ? asset.Colors[3] : asset.Colors?.Count > 0 ? asset.Colors[0] : UnityEngine.Color.white) // B
            };
    }
}
