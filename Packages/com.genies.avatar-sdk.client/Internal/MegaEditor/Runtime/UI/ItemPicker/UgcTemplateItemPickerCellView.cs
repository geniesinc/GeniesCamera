using UnityEngine;
using UnityEngine.UI;

namespace Genies.Customization.Framework.ItemPicker
{
    public class UgcTemplateItemPickerCellView : ItemPickerCellView
    {
        public Image thumbnail;

        [SerializeField]
        private GameObject _editableView;

        public void SetIsEditable(bool isEditable)
        {
            _editableView.SetActive(isEditable);
        }

        protected override void OnSelectionChanged(bool isSelected)
        {

        }

        protected override void OnInitialize()
        {
        }

        protected override void OnDispose()
        {
            _editableView.SetActive(false);
        }
    }
}
