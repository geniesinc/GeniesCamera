using Genies.Avatars;
using UMA;
using UnityEngine;

namespace Genies.Ugc
{
    /// <summary>
    /// Contains UGC element data and assets.
    /// </summary>
    public sealed class UgcElementAsset : IAsset
    {
        public string Id => Data.ElementId;
        public string Lod { get; }

        public UgcTemplateElementData Data { get; }
        public Texture AlbedoTransparency { get; }
        public Texture MetallicSmoothness { get; }
        public Texture Normal { get; }
        public Texture RgbaMask { get; }
        public SlotDataAsset[] SlotDataAssets { get; }
        public IGenieComponentCreator[] ComponentCreators { get; }

        public UgcElementAsset(
            string lod,
            UgcTemplateElementData data,
            Texture albedoTransparency,
            Texture metallicSmoothness,
            Texture normal,
            Texture rgbaMask,
            SlotDataAsset[] slotDataAssets,
            IGenieComponentCreator[] componentCreators)
        {
            Lod = lod;
            Data = data;
            AlbedoTransparency = albedoTransparency;
            MetallicSmoothness = metallicSmoothness;
            Normal = normal;
            RgbaMask = rgbaMask;
            SlotDataAssets = slotDataAssets;
            ComponentCreators = componentCreators;
        }
    }
}
