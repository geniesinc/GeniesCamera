using System;
using System.Linq;
using System.Collections.Generic;
using Genies.ColorPresetManager;
using Genies.MakeupPresets;
using UnityEngine;

namespace Genies.Inventory.UIData
{
    /// <summary>
    /// Utility class to get all the subcategories for each of our <see cref="ColorMainTypes"/>
    /// </summary>
    public static class ColorPresetUtils
    {
        public static IReadOnlyList<string> DnaColorCategories;
        public static IReadOnlyList<string> MakeupColorCategories;
        public static IReadOnlyList<string> SkinColorCategories;

        static ColorPresetUtils()
        {
            var categories          = (ColorPresetCategory[])Enum.GetValues(typeof(ColorPresetCategory));
            var makeupSubcategories = (MakeupPresetCategory[])Enum.GetValues(typeof(MakeupPresetCategory));

            DnaColorCategories = categories.Where(
                    c => c != ColorPresetCategory.MakeupColor &&
                         c != ColorPresetCategory.SkinColor &&
                         c != ColorPresetCategory.SkinColorIcon
                )
                .Select(c => c.ToString())
                .ToList();

            MakeupColorCategories = makeupSubcategories.Select(m => m.ToString()).ToList();
            SkinColorCategories = new List<string>() { ColorPresetCategory.SkinColor.ToString(), ColorPresetCategory.SkinColorIcon.ToString() };
        }
    }
}
