using System.Collections.Generic;
using UnityEngine;

namespace Genies.Avatars
{
    /// <summary>
    /// Contains data related to outfit slots.
    /// </summary>
    public sealed class OutfitSlotsData
    {
        public struct Slot
        {
            public string Name;
            public HashSet<string> IncompatibleSlots;
            public HashSet<string> SuppressedSlots;
        }

        public struct CollisionGroup
        {
            public string Name;
            public HashSet<string> Slots;
        }
        
        public readonly IReadOnlyList<Slot> Slots;
        public readonly IReadOnlyList<CollisionGroup> CollisionGroups;
        
        private readonly List<Slot> _slots;
        private readonly List<CollisionGroup> _collisionGroups;
        private readonly Dictionary<string, Slot> _slotsByName;
        private readonly Dictionary<string, CollisionGroup> _collisionGroupsByName;
        private readonly Dictionary<string, CollisionGroup> _collisionGroupsBySlotName;

        public OutfitSlotsData(IEnumerable<Slot> slots = null, IEnumerable<CollisionGroup> collisionGroups = null)
        {
            _slots = new List<Slot>();
            _collisionGroups = new List<CollisionGroup>();
            _slotsByName = new Dictionary<string, Slot>();
            _collisionGroupsByName = new Dictionary<string, CollisionGroup>();
            _collisionGroupsBySlotName = new Dictionary<string, CollisionGroup>();
            
            Slots = _slots.AsReadOnly();
            CollisionGroups = _collisionGroups.AsReadOnly();
            
            InitializeSlots(slots);
            InitializeCollisionGroups(collisionGroups);
        }

        public bool TryGetSlot(string slotName, out Slot slot)
        {
            return _slotsByName.TryGetValue(slotName, out slot);
        }
        
        public bool TryGetCollisionGroup(string collisionGroupName, out CollisionGroup collisionGroup)
        {
            return _collisionGroupsByName.TryGetValue(collisionGroupName, out collisionGroup);
        }
        
        public bool TryGetCollisionGroupBySlotName(string slotName, out CollisionGroup collisionGroup)
        {
            return _collisionGroupsBySlotName.TryGetValue(slotName, out collisionGroup);
        }

        private void InitializeSlots(IEnumerable<Slot> slots)
        {
            if (slots is null)
            {
                return;
            }

            foreach (Slot slot in slots)
            {
                // checkout if the slot is already added and log an error
                if (_slotsByName.ContainsKey(slot.Name))
                {
                    Debug.LogError($"The {slot.Name} slot is duplicated");
                    continue;
                }
                
                _slotsByName[slot.Name] = slot;
                _slots.Add(slot);
            }
        }
        
        private void InitializeCollisionGroups(IEnumerable<CollisionGroup> collisionGroups)
        {
            if (collisionGroups is null)
            {
                return;
            }

            foreach (CollisionGroup collisionGroup in collisionGroups)
            {
                // checkout if the group is already added and log an error
                if (_collisionGroupsByName.ContainsKey(collisionGroup.Name))
                {
                    Debug.LogError($"The {collisionGroup.Name} collision group is duplicated");
                    continue;
                }
                
                foreach (string slot in collisionGroup.Slots)
                {
                    // checkout if the slot is in multiple collision groups and log an error
                    if (_collisionGroupsBySlotName.TryGetValue(slot, out CollisionGroup registeredGroup))
                    {
                        Debug.LogError($"The {slot} slot is in two different collision groups: {registeredGroup.Name} and {collisionGroup.Name}");
                    }

                    _collisionGroupsBySlotName[slot] = collisionGroup;
                }
                
                _collisionGroups.Add(collisionGroup);
                _collisionGroupsByName[collisionGroup.Name] = collisionGroup;
            }
        }
    }
}