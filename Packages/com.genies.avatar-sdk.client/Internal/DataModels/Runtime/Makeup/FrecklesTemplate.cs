using Genies.MakeupPresets;
using UnityEngine;

namespace Genies.Models.Makeup
{
#if GENIES_INTERNAL
    [CreateAssetMenu(fileName = "FrecklesTemplate", menuName = "Genies/Makeup/FrecklesTemplate")]
#endif
    public class FrecklesTemplate : MakeupTemplate
    {
        //Fallback based on Freckles_1_MaterialDataMakeupColor
        [SerializeField] private Color _fallbackColor = new Color(0.2075472F, 0.0865865F, 0.03622286F, 1F);
        [SerializeField] private float _opacity = 1.0F;

        public override MakeupPresetCategory Category => MakeupPresetCategory.Freckles;
        public Color FallbackColor
        {
            get => _fallbackColor;
            set => _fallbackColor = value;
        }
        public float Opacity
        {
            get => _opacity;
            set => _opacity = value;
        }
    }
}
