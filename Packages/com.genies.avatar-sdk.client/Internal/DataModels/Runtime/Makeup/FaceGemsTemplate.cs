using Genies.MakeupPresets;
using UnityEngine;

namespace Genies.Models.Makeup
{
#if GENIES_INTERNAL
    [CreateAssetMenu(fileName = "FaceGemsTemplate", menuName = "Genies/Makeup/FaceGemsTemplate")]
#endif
    public class FaceGemsTemplate : MakeupTemplate
    {
        //Fallback based on FaceGems_1_MaterialDataMakeupColor
        [SerializeField] private Color _fallbackColor1 = new Color(0.3148642F, 0.5377358F, 0.2815503F, 1F);
        [SerializeField] private Color _fallbackColor2 = new Color(0.2663507F, 0.2423015F, 0.5188679F, 1F);
        [SerializeField] private Color _fallbackColor3 = new Color(0.7830189F, 0.4616857F, 0.6539594F, 1F);
        [SerializeField] private float _opacity = 1.0F;

        public override MakeupPresetCategory Category => MakeupPresetCategory.FaceGems;
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
