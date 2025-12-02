using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Genies.Avatars
{
    /// <summary>
    /// Used by the <see cref="MaterialController"/> to control each material slot. It extends the <see cref="IGenieMaterial"/>
    /// interface so the slot itself is actually added to the controlled <see cref="IEditableGenie"/> as a genie material.
    /// </summary>
    public class MaterialSlotController : IGenieMaterial, IDisposable
    {
        public string SlotId { get; }
        public Material Material => EquippedMaterial ? EquippedMaterial : OriginalMaterial;

        public event Action Updated;

        protected Material EquippedMaterial;
        protected Material OriginalMaterial;

        public MaterialSlotController(string slotId)
        {
            SlotId = slotId;
        }

        public void EquipMaterial(Material material)
        {
            if (!material || material == EquippedMaterial)
            {
                return;
            }

            EquippedMaterial = material;
            NotifyUpdate();
        }

        public void ClearSlot()
        {
            if (!EquippedMaterial)
            {
                return;
            }

            EquippedMaterial = null;
            NotifyUpdate();
        }

        public virtual void OnApplyingMaterial(Material previousMaterial)
        {
            if (OriginalMaterial)
            {
                return;
            }

            // save the material that was applied originally to this slot so we can apply it when the slot is cleared
            OriginalMaterial = new Material(previousMaterial);
        }

        public void Dispose()
        {
            if (OriginalMaterial)
            {
                Object.Destroy(OriginalMaterial);
                OriginalMaterial = null;
            }

            if (EquippedMaterial)
            {
                Object.Destroy(EquippedMaterial);
                EquippedMaterial = null;
            }
        }

        protected void NotifyUpdate()
        {
            Updated?.Invoke();
        }
    }
}
