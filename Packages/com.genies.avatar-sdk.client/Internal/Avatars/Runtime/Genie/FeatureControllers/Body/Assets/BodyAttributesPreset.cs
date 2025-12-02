using System.Collections.Generic;
using UnityEngine;

namespace Genies.Avatars
{
#if GENIES_INTERNAL
    [CreateAssetMenu(fileName = "BodyAttributesPreset", menuName = "Genies/Body Attributes Preset")]
#endif
    public sealed class BodyAttributesPreset : ScriptableObject
    {
        public List<BodyAttributeState> attributesStates = new();
    }
}
