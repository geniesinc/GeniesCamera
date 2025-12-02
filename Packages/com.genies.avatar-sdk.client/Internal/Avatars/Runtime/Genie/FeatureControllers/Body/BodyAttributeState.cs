using System;
using UnityEngine;

namespace Genies.Avatars
{
    [Serializable]
    public struct BodyAttributeState
    {
        public string name;
        [Range(-1.0f, 1.0f)]
        public float weight;

        public BodyAttributeState(string name, float weight)
        {
            this.name = name;
            this.weight = weight;
        }
    }
}