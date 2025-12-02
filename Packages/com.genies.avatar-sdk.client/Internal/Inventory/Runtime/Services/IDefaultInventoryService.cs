using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Genies.Services.Model;
using UnityEngine;

namespace Genies.Inventory
{
    public interface IDefaultInventoryService
    {
        // Initial fetch methods with pagination support
        public UniTask<List<ColorTaggedInventoryAsset>> GetDefaultWearables(int? limit = null, List<string> categories = null);
        public UniTask<List<ColorTaggedInventoryAsset>> GetUserWearables(int? limit = null, List<string> categories = null);
        public UniTask<List<ColorTaggedInventoryAsset>> GetAllWearables();
        public UniTask<List<ColorTaggedInventoryAsset>> GetDefaultAvatar(int? limit = null, List<string> categories = null);
        public UniTask<List<DefaultAvatarBaseAsset>> GetDefaultAvatarBaseData(int? limit = null, List<string> categories = null);
        public UniTask<List<DefaultAnimationLibraryAsset>> GetDefaultAnimationLibrary(int? limit = null, List<string> categories = null);
        public UniTask<List<ColoredInventoryAsset>> GetDefaultAvatarEyes(int? limit = null, List<string> categories = null);
        public UniTask<List<DefaultInventoryAsset>> GetDefaultAvatarFlair(int? limit = null, List<string> categories = null);
        public UniTask<List<DefaultInventoryAsset>> GetDefaultAvatarMakeup(int? limit = null, List<string> categories = null);
        public UniTask<List<ColoredInventoryAsset>> GetDefaultColorPresets(int? limit = null, List<string> categories = null);
        public UniTask<List<ColorTaggedInventoryAsset>> GetDefaultDecor(int? limit = null, List<string> categories = null);
        public UniTask<List<DefaultInventoryAsset>> GetDefaultImageLibrary(int? limit = null, List<string> categories = null);
        public UniTask<List<ColorTaggedInventoryAsset>> GetDefaultModelLibrary(int? limit = null, List<string> categories = null);
        
        // Load more methods for pagination
        public UniTask<List<ColorTaggedInventoryAsset>> LoadMoreDefaultWearables();
        public UniTask<List<ColorTaggedInventoryAsset>> LoadMoreUserWearables();
        public UniTask<List<ColorTaggedInventoryAsset>> LoadMoreDefaultAvatar();
        public UniTask<List<DefaultAvatarBaseAsset>> LoadMoreDefaultAvatarBaseData();
        public UniTask<List<DefaultAnimationLibraryAsset>> LoadMoreDefaultAnimationLibrary();
        public UniTask<List<ColoredInventoryAsset>> LoadMoreDefaultAvatarEyes();
        public UniTask<List<DefaultInventoryAsset>> LoadMoreDefaultAvatarFlair();
        public UniTask<List<DefaultInventoryAsset>> LoadMoreDefaultAvatarMakeup();
        public UniTask<List<ColoredInventoryAsset>> LoadMoreDefaultColorPresets();
        public UniTask<List<ColorTaggedInventoryAsset>> LoadMoreDefaultDecor();
        public UniTask<List<DefaultInventoryAsset>> LoadMoreDefaultImageLibrary();
        public UniTask<List<ColorTaggedInventoryAsset>> LoadMoreDefaultModelLibrary();
        
        // Check if more data is available for pagination
        public bool HasMoreDefaultWearables();
        public bool HasMoreUserWearables();
        public bool HasMoreDefaultAvatar();
        public bool HasMoreDefaultAvatarBaseData();
        public bool HasMoreDefaultAnimationLibrary();
        public bool HasMoreDefaultAvatarEyes();
        public bool HasMoreDefaultAvatarFlair();
        public bool HasMoreDefaultAvatarMakeup();
        public bool HasMoreDefaultColorPresets();
        public bool HasMoreDefaultDecor();
        public bool HasMoreDefaultImageLibrary();
        public bool HasMoreDefaultModelLibrary();
        
        public UniTask<(bool, string)> GiveAssetToUserAsync(string assetId);
        
        public UniTask<List<CustomColorResponse>> GetCustomColors(string category = null);
        public UniTask<string> CreateCustomColor(List<Color> colors, CreateCustomColorRequest.CategoryEnum category);
        
        public UniTask UpdateCustomColor(string instanceId, List<Color> colors);
        public UniTask DeleteCustomColor(string instanceId, List<Color> colors);
        
        public event Action<string> AssetMinted;
    }
}
