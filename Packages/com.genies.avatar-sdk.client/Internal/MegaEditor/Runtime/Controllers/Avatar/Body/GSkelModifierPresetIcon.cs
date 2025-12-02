using UnityEngine;

namespace Genies.MegaEditor
{
    public enum GSkelPresetGender
    {
        None = 0,
        Female = 1,
        Male = 2,
        Androgynous = 3
    }

    #if GENIES_INTERNAL
    [CreateAssetMenu(menuName = "Genies/Chaos Mode/GSkelModifierPresetIcon", fileName = "gSkelModifierPresetIcon.asset")]
    #endif

    public class GSkelModifierPresetIcon : ScriptableObject
    {
        public string PresetAddress;
        public Sprite Icon;
        public GSkelPresetGender FilterGender;
    }
}