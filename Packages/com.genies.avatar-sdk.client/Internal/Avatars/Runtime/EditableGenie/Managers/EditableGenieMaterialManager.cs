using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Genies.Avatars
{
    internal sealed class EditableGenieMaterialManager : IDisposable
    {
        public IReadOnlyCollection<IGenieMaterial> Materials { get; }
        public bool IsAnyMaterialDirty { get; private set; }

        // dependencies
        private readonly EditableGenie _genie;

        // state
        private readonly Renderer _renderer;
        private readonly Dictionary<string, Slot> _slots;
        private readonly List<IGenieMaterial> _genieMaterials; // redundant collection used for the Materials property

        // helpers
        private readonly List<Material> _materials;
        private bool _materialWasReplaced;

        public EditableGenieMaterialManager(EditableGenie genie, IDictionary<string, string> mappedUmaIdentifiers = null)
        {
            _genie = genie;

            _renderer = genie.GetComponentInChildren<Renderer>();
            if (_renderer is null)
            {
                Debug.LogError($"[{nameof(EditableGenieMaterialManager)}] couldn't get the renderer component from the UMA genie");
                return;
            }

            _slots = new Dictionary<string, Slot>();
            _genieMaterials = new List<IGenieMaterial>();
            _materials = new List<Material>();
            Materials = _genieMaterials.AsReadOnly();

            // initialize known slots (the ones mapped to a uma identifier)
            if (mappedUmaIdentifiers != null)
            {
                foreach (KeyValuePair<string, string> pair in mappedUmaIdentifiers)
                {
                    string slotId = pair.Key;
                    string identifier = pair.Value;
                    Slot slot = new Slot(slotId, identifier);
                    slot.MaterialDirtied += OnMaterialDirtied;
                    _slots[slotId] = slot;
                }
            }

            RefreshSlots();
        }

        public void AddMaterial(IGenieMaterial material)
        {
            if (material is null)
            {
                Debug.LogError($"[{nameof(EditableGenieMaterialManager)}] tried to add a null material");
                return;
            }

            string slotId = material.SlotId;
            if (!TryGetSlot(slotId, out Slot slot))
            {
                Debug.LogError($"[{nameof(EditableGenieMaterialManager)}] invalid genie material slot ID: {slotId ?? "[null]"}");
                return;
            }

            // if already added do nothing
            if (material == slot.GenieMaterial)
            {
                return;
            }

            if (slot.GenieMaterial != null)
            {
                _genieMaterials.Remove(slot.GenieMaterial);
            }

            _genieMaterials.Add(material);
            slot.SetMaterial(material);

            // if the slot is active make sure the material is dirty
            if (slot.IsActive)
            {
                slot.IsMaterialDirty = true;
                IsAnyMaterialDirty = true;
            }
        }

        public void RemoveMaterial(IGenieMaterial material)
        {
            if (material is null || !TryGetSlot(material.SlotId, out Slot slot, createIfNotFound: false) || slot.GenieMaterial != material)
            {
                return;
            }

            _genieMaterials.Remove(slot.GenieMaterial);
            slot.SetMaterial(null);
            slot.IsMaterialDirty = false;

            if (!slot.IsActive)
            {
                return;
            }

            slot.RestorePrevMaterials(_renderer);
        }

        public void ClearMaterialSlot(string slotId)
        {
            if (!TryGetSlot(slotId, out Slot slot, createIfNotFound: false) || slot.GenieMaterial is null)
            {
                return;
            }

            _genieMaterials.Remove(slot.GenieMaterial);
            slot.SetMaterial(null);
            slot.IsMaterialDirty = false;

            if (!slot.IsActive)
            {
                return;
            }

            slot.RestorePrevMaterials(_renderer);
        }

        public bool TryGetMaterial(string slotId, out IGenieMaterial material)
        {
            if (TryGetSlot(slotId, out Slot slot, createIfNotFound: false) && slot.GenieMaterial != null)
            {
                material = slot.GenieMaterial;
                return true;
            }

            material = null;
            return false;
        }

        public bool TryGetSharedMaterial(string slotId, out Material material)
        {
            // if for some reason there are no materials in the list, update it from the renderer
            if (_materials.Count == 0)
            {
                _renderer.GetSharedMaterials(_materials);
            }

            if (TryGetSlot(slotId, out Slot slot, createIfNotFound: false) && slot.Indices.Count > 0)
            {
                int index = slot.Indices[0];
                if (index >= 0 && index < _materials.Count)
                {
                    material = _materials[index];
                    return true;
                }
            }

            material = null;
            return false;
        }

        /// <summary>
        /// Applies all the dirty genie materials and sets them to the renderer. If forced is set to true, it will also apply
        /// those materials that were not updated since the last apply.
        /// </summary>
        public void ApplyMaterials(bool forced = false)
        {
            BeginMaterialsUpdate();

            Material[] materials = _genie.Renderers[0].sharedMaterials;
            foreach (Slot slot in _slots.Values)
            {
                ApplyMaterialInSlot(slot, materials, forced);
            }

            FinishMaterialsUpdate();
            IsAnyMaterialDirty = false;
        }

        public void RefreshSlots()
        {
            Material[] materials = _renderer.sharedMaterials;
            foreach (Slot slot in _slots.Values)
            {
                RefreshSlot(slot, materials);
            }
        }

        public void Dispose()
        {
            // clear all materials on slots so the slots unsubscribe from the updated event
            foreach (Slot slot in _slots.Values)
            {
                slot.MaterialDirtied -= OnMaterialDirtied;
                slot.Dispose();
            }

            _slots.Clear();
            _genieMaterials.Clear();
            _materials.Clear();
        }

        private void BeginMaterialsUpdate()
        {
            _materials.Clear();
            _renderer.GetSharedMaterials(_materials);
            _materialWasReplaced = false;
        }

        private void ApplyMaterialInSlot(Slot slot, Material[] materials, bool forced = false)
        {
            // no material is currently added on this slot
            if (slot.GenieMaterial is null)
            {
                return;
            }

            bool isDirty = slot.IsMaterialDirty;

            // every time we "apply" a material we set it not dirty
            slot.IsMaterialDirty = false;

            // only apply the material if it is dirty or it is a forced update
            if (!forced && !isDirty)
            {
                return;
            }

            // only apply the material if active
            if (!slot.IsActive)
            {
                return;
            }

            // notify the material that it is being applied (it may want to copy some configuration from the current material)
            Material previousMaterial = _materials[slot.Indices[0]];
            slot.GenieMaterial.OnApplyingMaterial(previousMaterial);

            // if it is null or the same instance than the previously applied one, then do nothing
            Material materialToApply = slot.GenieMaterial.Material;

            // if not using the same shader, destroy the previously applied material and replace it with a new instance
            _materialWasReplaced = true; // needed to know if we need to re-set the materials back to the renderer

            foreach (int index in slot.Indices)
            {
                _materials[index] = materialToApply;
            }
        }

        private void FinishMaterialsUpdate()
        {
            if (_materialWasReplaced)
            {
                _renderer.sharedMaterials = _materials.ToArray();
            }
        }

        private void RefreshSlot(Slot slot, Material[] materials)
        {
            slot.Indices.Clear();
            slot.PrevMaterials.Clear();

            for (int i = 0; i < materials.Length; ++i)
            {
                Material material = materials[i];
                if (!material || !material.name.Contains(slot.Identifier))
                {
                    continue;
                }

                slot.Indices.Add(i);
                slot.PrevMaterials.Add(material);
            }

            slot.SetActive(slot.Indices.Count > 0);
        }

        private bool TryGetSlot(string slotId, out Slot slot, bool createIfNotFound = true)
        {
            if (string.IsNullOrEmpty(slotId))
            {
                slot = null;
                return false;
            }

            // if the slot doesn't exist yet, create a new one
            if (!_slots.TryGetValue(slotId, out slot))
            {
                if (!createIfNotFound)
                {
                    return false;
                }

                _slots[slotId] = slot = new Slot(slotId, slotId); // this slot ID is not mapped to an identifier so we use the slot ID as the identifier
                slot.MaterialDirtied += OnMaterialDirtied;
            }

            return true;
        }

        private void OnMaterialDirtied()
        {
            IsAnyMaterialDirty = true;
        }

        /// <summary>
        /// Private class used to manage the state of a slot.
        /// </summary>
        private sealed class Slot
        {
            public string SlotId { get; }
            public string Identifier { get; }
            public IGenieMaterial GenieMaterial { get; private set; }
            public bool IsMaterialDirty { get; set; }
            public List<int> Indices { get; } = new();
            public List<Material> PrevMaterials { get; } = new();

            public event Action MaterialDirtied;

            /// <summary>
            /// Whether or not this slot is currently active in the UMA character. If the slot is not active it means
            /// that this material cannot be replaced in the SkinnedMeshRenderer because it doesn't currently exist on the character,
            /// i.e.: if there is no hair equipped the hair slot will not be active)
            /// </summary>
            public bool IsActive { get; private set; }

            public Slot(string slotId, string identifier)
            {
                SlotId = slotId;
                Identifier = identifier;
            }

            public void SetMaterial(IGenieMaterial material)
            {
                if (GenieMaterial != null)
                {
                    GenieMaterial.Updated -= OnMaterialUpdated;
                }

                GenieMaterial = material;

                if (material is null)
                {
                    IsMaterialDirty = false;
                }
                else
                {
                    GenieMaterial.Updated += OnMaterialUpdated;
                    IsMaterialDirty = true;
                }
            }

            public void SetActive(bool active)
            {
                if (IsActive == active)
                {
                    return;
                }

                bool wasDirty = IsMaterialDirty;
                // if the material was previously inactive and just got active, then set it dirty
                IsMaterialDirty = GenieMaterial != null && !IsActive;
                IsActive = active;

                // if just got dirty, invoke the event
                if (!wasDirty && IsMaterialDirty)
                {
                    MaterialDirtied?.Invoke();
                }
            }

            public void RestorePrevMaterials(Renderer renderer)
            {
                Material[] materials = renderer.sharedMaterials;

                for (int i = 0; i < Indices.Count && i < PrevMaterials.Count; ++i)
                {
                    int index = Indices[i];
                    if (index >= 0 && index < materials.Length)
                    {
                        materials[index] = PrevMaterials[i];
                    }
                }

                renderer.sharedMaterials = materials;
            }

            private void OnMaterialUpdated()
            {
                // if the material is not active we will not set it dirty
                if (!IsActive)
                {
                    return;
                }

                IsMaterialDirty = true;
                MaterialDirtied?.Invoke();
            }

            public void Dispose()
            {
                SetMaterial(null);

                foreach (Material m in PrevMaterials)
                {
                    Object.Destroy(m);
                }

                PrevMaterials.Clear();
                Indices.Clear();

                if (GenieMaterial != null)
                {
                    Object.Destroy(GenieMaterial.Material);
                }
            }
        }
    }
}
