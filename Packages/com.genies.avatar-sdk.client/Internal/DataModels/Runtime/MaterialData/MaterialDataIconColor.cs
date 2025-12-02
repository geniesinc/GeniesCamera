using UnityEngine;

#if UNITY_EDITOR
#endif
namespace Genies.Models
{
#if GENIES_INTERNAL
    [CreateAssetMenu(fileName = "MaterialDataIconColor", menuName = "Genies/MaterialData/MaterialDataIconColor")]
#endif
    public class MaterialDataIconColor : MaterialDataContainer
    {
        public Color IconColor;
        public string TargetContainerType;
    }
}
