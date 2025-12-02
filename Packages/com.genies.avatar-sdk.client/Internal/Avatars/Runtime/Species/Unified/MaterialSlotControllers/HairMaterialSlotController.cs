using UnityEngine;
using Genies.Shaders;

namespace Genies.Avatars
{
    /// <summary>
    /// <see cref="MaterialSlotController"/> implementation for the unified hair and facial hair material slots.
    /// This implementation is needed since the hair materials need to copy over some textures from the previous
    /// material every time it is applied.
    /// </summary>
    public sealed class HairMaterialSlotController : MaterialSlotController
    {
        private static readonly int _albedoTransparencyId = Shader.PropertyToID("_AlbedoTransparency");
        private static readonly int _metallicSmoothnessId = Shader.PropertyToID("_MetallicSmoothness");
        private static readonly int _normalAOId = Shader.PropertyToID("_NormalAO");
        private static readonly int _rgbaMaskId = Shader.PropertyToID("_RGBAMask");

        // old hair shader properties
        private static readonly int _albedoControlId = Shader.PropertyToID("_AlbedoControlTexture");
        private static readonly int _flowMapId = Shader.PropertyToID("_FlowMapTexture");
        private static readonly int _normalMapId = Shader.PropertyToID("_NormalMapTexture");

        public HairMaterialSlotController(string slotId)
            : base(slotId)
        {
            EquippedMaterial = GeniesShaders.MegaHair.NewMaterial();
        }

        public override void OnApplyingMaterial(Material previousMaterial)
        {

            base.OnApplyingMaterial(previousMaterial);

            // if equipped material is null, set up like original material
            if (!EquippedMaterial)
            {
                EquippedMaterial = OriginalMaterial;
            }

            if (!previousMaterial)
            {
                return;
            }

            // TODO this is probably completely deprecated (no color presets using this shader). We should check it and remove this block
            if (previousMaterial.shader.name == "Custom/Hair")
            {
                // uses old hair shader
                Texture albedoControlTexture = previousMaterial.GetTexture(_albedoControlId);
                Texture flowMapTexture = previousMaterial.GetTexture(_flowMapId);
                Texture normalTexture = previousMaterial.GetTexture(_normalMapId);

                EquippedMaterial.SetTexture(_albedoControlId, albedoControlTexture);
                EquippedMaterial.SetTexture(_flowMapId, flowMapTexture);
                EquippedMaterial.SetTexture(_normalMapId, normalTexture);
                return;
            }

            Texture albedoTransparencyTexture = previousMaterial.GetTexture(_albedoTransparencyId);
            Texture metallicSmoothnessTexture = previousMaterial.GetTexture(_metallicSmoothnessId);
            Texture normalMapTexture = previousMaterial.GetTexture(_normalAOId);
            Texture rgbaMaskTexture = previousMaterial.GetTexture(_rgbaMaskId);

            EquippedMaterial.SetTexture(_albedoTransparencyId, albedoTransparencyTexture);
            EquippedMaterial.SetTexture(_metallicSmoothnessId, metallicSmoothnessTexture);
            EquippedMaterial.SetTexture(_normalAOId, normalMapTexture);
            EquippedMaterial.SetTexture(_rgbaMaskId, rgbaMaskTexture);

            // Setting this render queue ensures the hair is always opaque instead of transparent
            EquippedMaterial.renderQueue = 2250;
            EquippedMaterial.name = $"{SlotId}_Material"; // small detail to make naming cleaner for glTF exporting
        }
    }
}
