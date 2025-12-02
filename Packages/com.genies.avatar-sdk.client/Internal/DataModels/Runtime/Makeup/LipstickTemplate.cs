using Genies.MakeupPresets;
using UnityEngine;

namespace Genies.Models.Makeup
{
#if GENIES_INTERNAL
    [CreateAssetMenu(fileName = "LipstickTemplate", menuName = "Genies/Makeup/LipstickTemplate")]
#endif
    public class LipstickTemplate : MakeupTemplate
    {
        //Fallback based on Lip_1_MaterialDataMakeupColor
        [SerializeField] private Color _fallbackColor1 = new Color(0.7264151F, 0.4831346F, 0.7196573F, 1F);
        [SerializeField] private Color _fallbackColor2 = new Color(0.3396226F, 0.2162691F, 0.3252606F, 1F);
        [SerializeField] private Color _fallbackColor3 = new Color(0.7254902F, 0.482353F, 0.7215686F, 1F);

        public override MakeupPresetCategory Category => MakeupPresetCategory.Lipstick;
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
