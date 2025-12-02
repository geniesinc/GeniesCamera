using System;
using System.Collections.Generic;
using UnityEngine;

namespace Genies.Models
{
    public enum BlendShapeType
    {
        None = 0,
        Eyes = 1,
        Jaw = 2,
        Lips = 3,
        Nose = 4,
        Brow = 5
    }

    public enum BlendShapeTag
    {
        Gen4 = 0,
        Silver = 1
    }

    [Serializable]
    public class DNAItem //Unity serializable pair
    {
        public string Name;
        public float Value;
    }

#if GENIES_INTERNAL
    [CreateAssetMenu(fileName = "BlendShapeDataContainer", menuName = "Blendshapes/BlendShapeDataContainer")]
#endif
    public class BlendShapeDataContainer : OrderedScriptableObject
    {
        public BlendShapeType Type = BlendShapeType.None;
        public BlendShapeTag Tag = BlendShapeTag.Gen4;
        public Texture2D maleIcon;
        public Texture2D femaleIcon;
        public Texture2D unifiedIcon;
        public string blendShapeIdentifier;
        public List<DNAItem> DNA;
    }
}
