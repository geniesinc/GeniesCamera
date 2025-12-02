using Genies.MakeupPresets;
using UnityEngine;

namespace Genies.Models.Makeup
{
#if GENIES_INTERNAL
    [CreateAssetMenu(fileName = "EyeshadowTemplate", menuName = "Genies/Makeup/EyeshadowTemplate")]
#endif
    public class EyeshadowTemplate : MakeupTemplate
    {
        //Fallback based on Shadow_2_MaterialDataMakeupColor
        [SerializeField] private Color _fallbackColor1 = new Color(0F, 0F, 0F, 1F);
        [SerializeField] private Color _fallbackColor2 = new Color(1F, 1F, 1F, 1F);
        [SerializeField] private Color _fallbackColor3 = new Color(1F, 1F, 1F, 1F);

        public override MakeupPresetCategory Category => MakeupPresetCategory.Eyeshadow;
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
    }
}
