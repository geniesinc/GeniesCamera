using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Genies.Models {
#if GENIES_INTERNAL
    [CreateAssetMenu(fileName = "BlendShapesList", menuName = "Genies/BlendShapesList")]
#endif
    public class BlendShapesList : ScriptableObject {
        public List<BlendShapeDataContainer> BlendShapeContainers;
    }
}
