using System.Collections.Generic;
using UnityEngine;

namespace Genies.Models {
#if GENIES_INTERNAL
    [CreateAssetMenu(fileName = "FacePresetsList", menuName = "Genies/FacePresetsList")]
#endif
    public class FacePresetsList : ScriptableObject {
        public List<BlendshapeDataFacePresetContainer> FacePresetContainers;
    }
}
