using UMA;
using UnityEngine;

namespace Genies.Ugc
{
    public readonly struct GearSubElement
    {
        public bool IsEditable => EditableRegionCount > 0 && EditableRegionsMap;

        public readonly SlotDataAsset    Slot;
        public readonly OverlayDataAsset Overlay;
        public readonly UMAMaterial      Material;
        public readonly Texture2D        EditableRegionsMap;
        public readonly int              EditableRegionCount;

        public GearSubElement(
            SlotDataAsset slot,
            OverlayDataAsset overlay,
            UMAMaterial material,
            Texture2D editableRegionsMap,
            int editableRegionCount)
        {
            Slot = slot;
            Overlay = overlay;
            Material = material;
            EditableRegionsMap = editableRegionsMap;
            EditableRegionCount = editableRegionCount;
        }
    }
}
