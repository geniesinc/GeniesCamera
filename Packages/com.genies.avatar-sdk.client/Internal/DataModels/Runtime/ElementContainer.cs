using System.Collections.Generic;
using Genies.Components.ShaderlessTools;
using UMA;
using UnityEngine;
using UnityEngine.Serialization;

namespace Genies.Models
{
#if GENIES_INTERNAL
    [CreateAssetMenu(fileName = "ElementContainer", menuName = "Genies/UGCW/ElementContainer", order = 0)]
#endif
    public class ElementContainer : ScriptableObject, IShaderlessAsset
    {
        [SerializeField] private string elementId;
        [SerializeField] private List<SlotDataAsset> slotDataAssets;
        [SerializeField] private Texture2D albedoTransparency;
        [SerializeField] private Texture2D metallicSmoothness;
        [SerializeField] private Texture2D normal;
        [SerializeField] private Texture2D rgbaMask;
        [SerializeField] private int availableRegions;
        public List<Object> extras = new();

        // Dont't change FormerlySerializedAs attribute, All shaderless element containers where created with the old field.
        [SerializeField][FormerlySerializedAs("ShaderlessMaterials")]
        private ShaderlessMaterials shaderlessMaterials;
        public ShaderlessMaterials ShaderlessMaterials
        {
            get => shaderlessMaterials;
            set => shaderlessMaterials = value;
        }

        private List<Texture2D> currentTextureList;

        public List<Texture2D> CurrentTextureList
        {
            get
            {
                currentTextureList ??= new List<Texture2D>()
                {
                    albedoTransparency,
                    metallicSmoothness,
                    normal,
                    rgbaMask
                };
                return currentTextureList;
            }
            set => currentTextureList = value;
        }

        public string ElementId
        {
            get => elementId;
            set => elementId = value;
        }

        public List<SlotDataAsset> SlotDataAssets
        {
            get => slotDataAssets;
            set => slotDataAssets = value;
        }

        public Texture2D AlbedoTransparency
        {
            get => albedoTransparency;
            set => albedoTransparency = value;
        }

        public Texture2D MetallicSmoothness
        {
            get => metallicSmoothness;
            set => metallicSmoothness = value;
        }

        public Texture2D Normal
        {
            get => normal;
            set => normal = value;
        }

        public Texture2D RgbaMask
        {
            get => rgbaMask;
            set => rgbaMask = value;
        }

        public int AvailableRegions
        {
            get => availableRegions;
            set => availableRegions = value;
        }
    }
}
