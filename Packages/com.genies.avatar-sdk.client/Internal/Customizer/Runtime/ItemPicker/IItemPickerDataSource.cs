using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Genies.Customization.Framework.ItemPicker
{
    public interface IItemPickerDataSource
    {
        ItemPickerCtaConfig GetCtaConfig();
        ItemPickerLayoutConfig GetLayoutConfig();
        int GetCurrentSelectedIndex();
        /// <summary>
        /// Return true if the current selectec item is valid to process a CTA
        /// </summary>
        /// <returns></returns>
        bool ItemSelectedIsValidForProcessCTA();

        UniTask<int> InitializeAndGetCountAsync(CancellationToken cancellationToken);
        ItemPickerCellView GetCellPrefab(int index);
        Vector2 GetCellSize(int index);
        UniTask<bool> OnItemClickedAsync(int index, ItemPickerCellView clickedCell, bool wasSelected, CancellationToken cancellationToken);
        UniTask<bool> InitializeCellViewAsync(ItemPickerCellView view, int index, bool isSelected, CancellationToken cancellationToken);
        void DisposeCellViewAsync(ItemPickerCellView view, int index);

        /// <summary>
        /// Loads more items if pagination is supported
        /// </summary>
        /// <returns>True if more items were successfully loaded</returns>
        UniTask<bool> LoadMoreItemsAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Whether more items are available to load
        /// </summary>
        bool HasMoreItems { get; }

        /// <summary>
        /// Whether items are currently being loaded
        /// </summary>
        bool IsLoadingMore { get; }
    }
}
