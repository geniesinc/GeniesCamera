using System.Collections.Generic;
using UnityEngine;
using System;

namespace Genies.Avatars
{
    [System.Serializable]
    public struct GSkelModValue : IComparable<GSkelModValue>
    {
        public string Name;
        public float Value;

        public int CompareTo(GSkelModValue other)
        {
            if (this.Name == null && other.Name == null)
            {
                return 0;
            }

            if (this.Name == null)
            {
                return -1;
            }

            if (other.Name == null)
            {
                return 1;
            }

            return this.Name.CompareTo(other.Name);
        }
    }

#if GENIES_INTERNAL
    [CreateAssetMenu(menuName = "Genies/Chaos Mode/GSkelModifierPreset", fileName = "gSkelModifierPreset.asset")]
#endif
    public class GSkelModifierPreset : ScriptableObject
    {
        public string Name;
        public string StartingBodyVariation;
        public List<GSkelModValue> GSkelModValues;

        /// <summary>
        /// Do these presets equal eachother in the sense that they're visually the same
        /// when applied to a genie
        /// </summary>
        public bool EqualsVisually(GSkelModifierPreset other)
        {
            if (StartingBodyVariation != other.StartingBodyVariation)
            {
                return false;
            }

            var thisMods = new Dictionary<string, float>();
            var otherMods = new Dictionary<string, float>();

            foreach (var mod in GSkelModValues)
            {
                if (mod.Value != 0)
                {
                    thisMods[mod.Name] = mod.Value;
                }
            }

            foreach (var mod in other.GSkelModValues)
            {
                if (mod.Value != 0)
                {
                    otherMods[mod.Name] = mod.Value;
                }
            }

            // Compare dictionary counts
            if (thisMods.Count != otherMods.Count)
            {
                return false;
            }

            // Compare key-value pairs
            foreach (var kv in thisMods)
            {
                if (!otherMods.TryGetValue(kv.Key, out float otherValue))
                {
                    return false;
                }

                if (kv.Value != otherValue)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
