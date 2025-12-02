using Genies.UIFramework;
using UnityEngine;

namespace Genies.Customization.Framework.ItemPicker
{
    public class GalleryItemPicker : ScrollingItemPicker
    {
        public AdjustGridLayoutCellSize adjustGridLayoutCellSize;
        public GeniesButton CollapseButton;

        public new virtual void Show(IItemPickerDataSource dataSource)
        {
            if (dataSource == null)
            {
                Hide();
                return;
            }

            SetGridLayoutCellSize(dataSource);
            base.Show(dataSource).Forget();
        }

        public void SetGridLayoutCellSize(IItemPickerDataSource dataSource)
        {
            var gridLayoutConfig = dataSource.GetLayoutConfig().gridLayoutConfig;
            adjustGridLayoutCellSize.SetSize(gridLayoutConfig.cellSize.x, gridLayoutConfig.cellSize.y);
        }

        public new virtual void Hide()
        {
            base.Hide();
        }
    }
}
