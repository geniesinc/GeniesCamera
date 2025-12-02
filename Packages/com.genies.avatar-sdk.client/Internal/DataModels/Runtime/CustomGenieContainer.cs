using System.Collections;
using System.Collections.Generic;
using UMA;
using UMA.CharacterSystem;
using UnityEngine;

namespace Genies.Models {
#if GENIES_INTERNAL
    [CreateAssetMenu(menuName = "Genies/Editor Utilities/Custom Genies/Create Custom Genie Container", fileName = "CustomGenieContainer.asset")]
#endif
    public class CustomGenieContainer : ScriptableObject {
        public UMAWardrobeRecipe Recipe;
        public string Subcategory;
        public DynamicDNAConverterController dnaController;
        public List<OverlayDataAsset> Overlays = new List<OverlayDataAsset>();
        public List<SlotDataAsset> Slots = new List<SlotDataAsset>();
    }
}

