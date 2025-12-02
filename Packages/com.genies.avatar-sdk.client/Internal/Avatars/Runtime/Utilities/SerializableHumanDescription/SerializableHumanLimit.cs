using System;
using UnityEngine;

namespace Genies.Avatars
{
    [Serializable]
    public struct SerializableHumanLimit
    {
        public bool    useDefaultValues;
        public Vector3 min;
        public Vector3 max;
        public Vector3 center;
        public float   axisLength;
        
        public static SerializableHumanLimit Convert(HumanLimit humanLimit)
        {
            return new SerializableHumanLimit
            {
                useDefaultValues = humanLimit.useDefaultValues,
                min              = humanLimit.min,
                max              = humanLimit.max,
                center           = humanLimit.center,
                axisLength       = humanLimit.axisLength,
            };
        }
        
        public static HumanLimit Convert(SerializableHumanLimit humanLimit)
        {
            return new HumanLimit
            {
                useDefaultValues = humanLimit.useDefaultValues,
                min              = humanLimit.min,
                max              = humanLimit.max,
                center           = humanLimit.center,
                axisLength       = humanLimit.axisLength,
            };
        }
    }
}