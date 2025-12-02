using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Genies.Looks.Customization.UI
{
    public interface IColorPickerController
    {
        IReadOnlyList<Color> Colors { get; }

        /// <summary>
        /// Called when a color ID has been selected. Must return true if the selection
        /// operation was performed successfully (i.e.: the color was applied to the region).
        /// </summary>
        UniTask<bool> OnColorSelectedAsync(Color color);

        /// <summary>
        /// Called when the custom color option is selected (it should go to the color picker view).
        /// </summary>
        void OnCustomColorSelected();
    }
}
