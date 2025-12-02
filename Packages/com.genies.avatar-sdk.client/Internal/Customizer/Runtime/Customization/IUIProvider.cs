using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;

namespace Genies.Customization.Framework
{
    // Base interface for provider class of UI data
    public interface IUIProvider : IDisposable
    {
        bool HasMoreData { get; }
        bool IsLoadingMore { get; }

        UniTask<List<IAssetUiData>> LoadMoreAsync(string category = null, string subcategory = null);
        UniTask<List<string>> GetAllAssetIds(string category = null, string subcategory = null, int? pageSize = null);
        UniTask<List<string>> GetAllAssetIds(List<string> categories, int? pageSize = null);
        UniTask<IAssetUiData> GetDataForAssetId(string assetId);
    }

    // Base interface for all UI data
    public interface IAssetUiData
    {
        public string AssetId { get; }
        public string DisplayName { get; }
        public string Category { get; }
        public string SubCategory { get; }
        public int Order { get; }
    }
}
