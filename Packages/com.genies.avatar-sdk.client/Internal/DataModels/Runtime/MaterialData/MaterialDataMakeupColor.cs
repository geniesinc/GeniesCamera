using Genies.MakeupPresets;
using UnityEngine;

namespace Genies.Models.MaterialData
{
#if GENIES_INTERNAL
    [CreateAssetMenu(fileName = "MaterialDataMakeupColor", menuName = "Genies/MaterialData/MaterialDataMakeupColor")]
#endif
    public class MaterialDataMakeupColor : MaterialDataIconColor
    {
        public Color IconColor2;
        public Color IconColor3;
        public MakeupPresetCategory makeupPresetCategory;
    }
}
