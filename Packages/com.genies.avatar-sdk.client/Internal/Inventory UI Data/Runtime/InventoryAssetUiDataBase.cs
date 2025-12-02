using System;
using Genies.Customization.Framework;
using UnityEngine;
using Genies.Refs;

namespace Genies.Inventory.UIData
{

    /// <summary>
    /// UI Data Types Guide:
    ///
    /// BasicInventoryUiData - For discrete assets (wearables, avatars, decor, etc.) with thumbnails
    ///
    /// SimpleColorUiData - For simple color displays that only need "Genies/ColorPresetIcon" shader (3 colors + border)
    ///   • Used for: Simple skin tones, eye colors
    ///   • Creates: Simple circular color swatch UI
    ///
    /// GradientColorUiData - For complex color presets that need BOTH shaders (4 colors)
    ///   • Used for: Flair, hair, makeup color presets
    ///   • Creates: Full 4-color gradient data (Material property)
    ///   • Also creates: Simple UI swatches (GetSwatchMaterial() method)
    ///   • Supports: Both color preset application AND thumbnail display
    /// </summary>


    public abstract class InventoryAssetUiDataBase : IAssetUiData
    {
        public string AssetId { get; }
        public string DisplayName { get; }
        public string Category { get; }
        public string SubCategory { get; }
        public int Order { get; }
        public bool IsEditable { get; set; }

        protected InventoryAssetUiDataBase(string assetId, string displayName, string category, string subCategory, int order, bool isEditable)
        {
            AssetId = assetId;
            DisplayName = displayName;
            Category = category;
            SubCategory = subCategory;
            Order = order;
            IsEditable = isEditable;
        }
    }

    // Basic UI data (name + thumbnail) - for most assets
    public class BasicInventoryUiData : InventoryAssetUiDataBase, IDisposable
    {
        public Ref<Sprite> Thumbnail { get; set; }
        public string Description { get; set; }

        public BasicInventoryUiData(
            string assetId,
            string displayName,
            string category,
            string subCategory,
            int order,
            string description,
            bool isEditable)
            : base(assetId, displayName, category, subCategory, order, isEditable)
        {
            Description = description;

            // Thumbnail is set by InventoryUIDataProvider
        }

        public void Dispose()
        {
            if (Thumbnail.IsAlive)
            {
                Thumbnail.Dispose();
            }
        }
    }


    // Color UI data - for color selection
    public class SimpleColorUiData : BasicInventoryUiData
    {
        public Material Material { get; }
        public Color InnerColor { get; }
        public Color MiddleColor { get; }
        public Color OuterColor { get; }

        public SimpleColorUiData(
            string assetId,
            string displayName,
            string category,
            string subCategory,
            int order,
            string description,
            bool isEditable,
            Color innerColor,
            Color middleColor,
            Color outerColor,
            float borderValue)
            : base(assetId, displayName, category, subCategory, order, description, isEditable)
        {

            InnerColor = innerColor;
            MiddleColor = middleColor;
            OuterColor = outerColor;

            // apply to material
            var material = new Material(Shader.Find("Genies/ColorPresetIcon"));
            int border = Shader.PropertyToID("_Border");
            int sInnerColor = Shader.PropertyToID("_InnerColor");
            int sMiddleColor = Shader.PropertyToID("_MidColor");
            int sOuterColor = Shader.PropertyToID("_OuterColor");

            material.SetColor(sInnerColor, innerColor);
            material.SetColor(sMiddleColor, middleColor);
            material.SetColor(sOuterColor, outerColor);
            material.SetFloat(border, borderValue);

            Material = material;

            // Thumbnail is set by InventoryUIDataProvider
        }
    }

    public class GradientColorUiData : InventoryAssetUiDataBase
    {
        public Color ColorBase { get; }
        public Color ColorR { get; }
        public Color ColorG { get; }
        public Color ColorB { get; }

        // Full 4-color gradient material (for color preset data)
        public Material Material { get; }

        // Simple swatch shader properties (for UI thumbnails)
        private static readonly int s_border = Shader.PropertyToID("_Border");
        private static readonly int s_innerColor = Shader.PropertyToID("_InnerColor");
        private static readonly int s_midColor = Shader.PropertyToID("_MidColor");

        public GradientColorUiData(
            string assetId,
            string displayName,
            string category,
            string subCategory,
            int order,
            bool isEditable,
            Color colorBase,
            Color colorR,
            Color colorG,
            Color colorB)
            : base(assetId, displayName, category, subCategory, order, isEditable)
        {
            ColorBase = colorBase;
            ColorR = colorR;
            ColorG = colorG;
            ColorB = colorB;

            // Create the 4-color gradient material (main color preset data)
            var material = new Material(Shader.Find("Custom/FourColorSwatch"));
            int sColorBase = Shader.PropertyToID("_ColorBase");
            int sColorR_prop = Shader.PropertyToID("_ColorR");
            int sColorG_prop = Shader.PropertyToID("_ColorG");
            int sColorB_prop = Shader.PropertyToID("_ColorB");

            material.SetColor(sColorBase, colorBase);
            material.SetColor(sColorR_prop, colorR);
            material.SetColor(sColorG_prop, colorG);
            material.SetColor(sColorB_prop, colorB);

            Material = material;
        }

        /// <summary>
        /// Creates a simple swatch material for UI thumbnails (like FlairColorItemPickerDataSource.GetSwatchMaterial)
        /// Uses "Genies/ColorPresetIcon" shader showing just the base color
        /// </summary>
        public Material GetSwatchMaterial(float borderValue = -1.81f)
        {
            var swatchMaterial = new Material(Shader.Find("Genies/ColorPresetIcon"));

            var mainColor = ColorBase;
            mainColor.a = 1f;

            swatchMaterial.SetFloat(s_border, borderValue);
            swatchMaterial.SetColor(s_innerColor, mainColor);
            swatchMaterial.SetColor(s_midColor, Color.white);

            return swatchMaterial;
        }

        /// <summary>
        /// Gets all 4 colors as an array (for compatibility with existing color mapping logic)
        /// </summary>
        public Color[] GetColorsArray()
        {
            return new Color[] { ColorBase, ColorR, ColorG, ColorB };
        }
    }

    public enum ColorMainTypes
    {
        Makeup = 0, //blush, eyeshadow, etc..
        Base = 1, //hair, eyes, etc...
        Skin = 2, //Handled separately
    }

}
