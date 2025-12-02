using Genies.MakeupPresets;
using UnityEngine;

namespace Genies.Models.Makeup
{
    #if GENIES_INTERNAL
    [CreateAssetMenu(fileName = "BlushTemplate", menuName = "Genies/Makeup/BlushTemplate")]
    #endif
    public class BlushTemplate : MakeupTemplate
    {
        //Fallback based on Blush_1_MaterialDataMakeupColor
        [SerializeField] private Color _fallbackColor1 = new Color(0.9339623F, 0.4108703F, 0.3411765F, 1F);
        [SerializeField] private Color _fallbackColor2 = new Color(0.9339623F, 0.4108703F, 0.3411765F, 1F);
        [SerializeField] private Color _fallbackColor3 = new Color(0.9339623F, 0.4108703F, 0.3411765F, 1F);
        [SerializeField] private float _opacity = 1.0F;

        public override MakeupPresetCategory Category => MakeupPresetCategory.Blush;
        public Color FallbackColor1
        {
            get => _fallbackColor1;
            set => _fallbackColor1 = value;
        }
        public Color FallbackColor2
        {
            get => _fallbackColor2;
            set => _fallbackColor2 = value;
        }
        public Color FallbackColor3
        {
            get => _fallbackColor3;
            set => _fallbackColor3 = value;
        }
        public float Opacity
        {
            get => _opacity;
            set => _opacity = value;
        }
    }
}
