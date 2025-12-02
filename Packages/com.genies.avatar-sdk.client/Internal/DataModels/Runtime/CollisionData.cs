using System;
using UnityEngine;

namespace Genies.Models
{
    [Serializable]
    public class CollisionData {
        [SerializeField]
        public CollisionType Type;
        [SerializeField]
        public int Layer;
        [SerializeField]
        public CollisionMode Mode;
        [SerializeField]
        public HatHairBehavior HatMode;

    }

    [Serializable]
    public enum CollisionType {
        open,
        closed
    }

    [Serializable]
    public enum CollisionMode {
        none,
        blend,
        simulated
    }

    [Serializable]
    public enum HatHairBehavior {
        none,
        blendshape,
        fallback
    }
}
