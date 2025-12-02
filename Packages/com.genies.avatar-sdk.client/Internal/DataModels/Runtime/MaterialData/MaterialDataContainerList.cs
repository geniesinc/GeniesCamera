using System.Collections.Generic;
using Genies.Models;
using UnityEngine;

namespace Genies.Models {
#if GENIES_INTERNAL
    [CreateAssetMenu(fileName = "MaterialDataContainerList", menuName = "Genies/MaterialDataContainerList")]
#endif
    public class MaterialDataContainerList : ScriptableObject {
        public List<MaterialDataContainer> MaterialContainers;
    }
}
