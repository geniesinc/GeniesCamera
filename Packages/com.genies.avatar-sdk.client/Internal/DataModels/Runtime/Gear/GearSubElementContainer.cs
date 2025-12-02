using UMA;
using UnityEngine;

namespace Genies.Models
{
    public sealed class GearSubElementContainer : OrderedScriptableObject
    {
        public bool IsEditable => editableRegionCount > 0 && editableRegionsMap;
        public string UmaMaterialAddress => slotDataAsset.material.name;

        public SlotDataAsset    slotDataAsset;
        public OverlayDataAsset overlayDataAsset;
        public Texture2D        editableRegionsMap;
        public int              editableRegionCount;
    }
}
