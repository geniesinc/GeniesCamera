using UnityEngine;
using UnityEngine.UI;

namespace Genies.Customization.Framework.ItemPicker
{
    public class BodyTypeItemPickerCellView : ItemPickerCellView
    {
        public Image thumbnail;

        protected override void OnInitialize()
        {
        }

        protected override void OnDispose()
        {
            thumbnail.color = Color.grey;
        }

        protected override void OnSelectionChanged(bool isSelected)
        {
            thumbnail.color = !isSelected ? Color.grey : Color.white;
        }
    }
}
