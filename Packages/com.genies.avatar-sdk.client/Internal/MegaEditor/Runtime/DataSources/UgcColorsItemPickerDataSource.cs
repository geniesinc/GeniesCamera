using System.Threading;
using Cysharp.Threading.Tasks;
using Genies.Customization.Framework.ItemPicker;
using Genies.Looks.Customization.UI;
using Genies.Utilities.Internal;
using UnityEngine;

namespace Genies.Customization.MegaEditor
{
    public sealed class UgcColorsItemPickerDataSource : IItemPickerDataSource
    {
        // dependencies
        private readonly IColorPickerController _controller;
        private readonly ItemPickerLayoutConfig _layoutConfig;
        private readonly GenericItemPickerCellView _cellViewPrefab;
        private readonly Sprite _customColor;

        // state
        private int _selectedColorIndex;

        public UgcColorsItemPickerDataSource(
            IColorPickerController controller,
            ItemPickerLayoutConfig layoutConfig,
            GenericItemPickerCellView cellViewPrefab,
            Sprite customColor)
        {
            _controller = controller;
            _layoutConfig = layoutConfig;
            _cellViewPrefab = cellViewPrefab;
            _customColor = customColor;
        }

        public void SetSelectedColor(Color color)
        {
            int index = _controller.Colors.IndexOf(color);
            _selectedColorIndex = index < 0 ? 0 : index + 1; // index 0 is for the custom color picker option
        }

        public void SetNoColorSelected()
        {
            _selectedColorIndex = -1;
        }

        public ItemPickerCtaConfig GetCtaConfig()
        {
            return null;
        }

        public ItemPickerLayoutConfig GetLayoutConfig()
        {
            return _layoutConfig;
        }

        public int GetCurrentSelectedIndex()
        {
            return _selectedColorIndex;
        }

        public bool ItemSelectedIsValidForProcessCTA()
        {
            return true;
        }

        // Pagination support (default implementation - no pagination for UGC colors yet)
        public bool HasMoreItems => false;
        public bool IsLoadingMore => false;
        public UniTask<bool> LoadMoreItemsAsync(CancellationToken cancellationToken) => UniTask.FromResult(false);

        public UniTask<int> InitializeAndGetCountAsync(CancellationToken cancellationToken)
        {
            return UniTask.FromResult(_controller.Colors.Count + 1); // +1 for the custom color option
        }

        public ItemPickerCellView GetCellPrefab(int index)
        {
            return _cellViewPrefab;
        }

        public Vector2 GetCellSize(int index)
        {
            return new Vector2(48, 48);
        }

        public async UniTask<bool> OnItemClickedAsync(
            int index,
            ItemPickerCellView clickedCell,
            bool wasSelected,
            CancellationToken cancellationToken)
        {
            // if clicked on the custom color option, then go to the color picker
            if (index == 0)
            {
                _controller.OnCustomColorSelected();
                return true;
            }

            if (wasSelected)
            {
                return true;
            }

            bool success = await _controller.OnColorSelectedAsync(_controller.Colors[index - 1]);
            if (success)
            {
                _selectedColorIndex = index;
            }

            return success;
        }

        public UniTask<bool> InitializeCellViewAsync(
            ItemPickerCellView view,
            int index,
            bool isSelected,
            CancellationToken cancellationToken)
        {
            if (!(view is GenericItemPickerCellView genericView))
            {
                return UniTask.FromResult(false);
            }

            genericView.SetIsEditable(false);

            if (index == 0)
            {
                // if this is the custom color option then set the raimbow color sprite
                genericView.thumbnail.sprite = _customColor;
                genericView.thumbnail.color = Color.white;
            }
            else
            {
                genericView.thumbnail.sprite = null;
                genericView.thumbnail.color = _controller.Colors[index - 1];
            }

            return UniTask.FromResult(true);
        }

        public void DisposeCellViewAsync(ItemPickerCellView view, int index)
        {
            view.Dispose();
        }
    }
}
