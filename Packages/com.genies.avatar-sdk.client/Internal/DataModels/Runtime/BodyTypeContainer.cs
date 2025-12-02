using System.Collections.Generic;
using UMA;
using UnityEngine;

namespace Genies.Models
{
#if GENIES_INTERNAL
    [CreateAssetMenu(fileName = "Body Type Container", menuName = "Genies/Content/Body Type Container")]
#endif
    public class BodyTypeContainer : ScriptableObject
    {
        public RaceData Race;
        public RuntimeAnimatorController IdleAnimator;
        public DynamicUMADnaAsset DnaAsset;
        public DynamicDNAConverterController DnaController;
        public List<OverlayDataAsset> Overlays = new();
        public List<SlotDataAsset> SlotDataAssets = new();
        public List<SlotDataAssetGroup> slotGroups = new();
        public List<Object> Extras = new();
    }
}
