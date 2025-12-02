using System;
using UnityEngine;

namespace Genies.Customization.Framework.ItemPicker
{
    [Serializable]
    public class LayoutConfigBase
    {
        public RectOffset padding;
    }

    [Serializable]
    public class HorizontalOrVerticalLayoutConfig : LayoutConfigBase
    {
        public float spacing = 8;
    }

    [Serializable]
    public class GridLayoutConfig : LayoutConfigBase
    {
        public Vector2 spacing = new Vector2(8, 8);
        public int columnCount = 4;
        public Vector2 cellSize = new Vector2(88, 96);
    }
    
    [Serializable]
    public class ItemPickerLayoutConfig
    {
        public HorizontalOrVerticalLayoutConfig horizontalOrVerticalLayoutConfig;
        public GridLayoutConfig gridLayoutConfig;
    }
}