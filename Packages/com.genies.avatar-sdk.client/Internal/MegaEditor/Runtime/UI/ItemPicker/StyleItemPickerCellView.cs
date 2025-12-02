using TMPro;
using UnityEngine;

namespace Genies.Customization.Framework.ItemPicker
{
    public class StyleItemPickerCellView : GenericItemPickerCellView
    {
        [SerializeField] private TMP_Text _label;

        public void SetLabel(string label)
        {
            _label.text = label;
        }

        protected override void OnDispose()
        {
            base.OnDispose();
            _label.text = "";
        }
    }
}
