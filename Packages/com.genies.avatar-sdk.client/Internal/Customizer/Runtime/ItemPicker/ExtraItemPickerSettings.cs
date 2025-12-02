using System;
using UnityEngine;

namespace Genies.Customization.Framework.ItemPicker
{
    /// <summary>
    ///  Settings for adding specific item pickers inside of a list on a specific
    /// index list to show and option for a different action
    /// such as None CTA or Chaos Sliders
    /// </summary>
    [Serializable]
    public class ExtraItemPickerSettings
    {
        public enum ExtraItemPickerType
        {
            NoneCTA = 1,
            ChaosSliders = 2,
        }

        [SerializeField] private int _staticIndexOnList;
        [SerializeField] private GenericItemPickerCellView _extraItemPicker;
        [SerializeField] private ExtraItemPickerType _type;

        public int StaticIndexOnList => _staticIndexOnList;
        public GenericItemPickerCellView ExtraItemPicker => _extraItemPicker;
        public ExtraItemPickerType Type => _type;
    }
}
