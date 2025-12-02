using System;
using System.Collections.Generic;
using UnityEngine;

namespace Genies.Avatars
{
    [Serializable]
    public struct SerializableHumanBone
    {
        public SerializableHumanLimit limit;
        public string                 boneName;
        public string                 humanName;
        
        public static SerializableHumanBone Convert(HumanBone humanBone)
        {
            return new SerializableHumanBone
            {
                limit     = SerializableHumanLimit.Convert(humanBone.limit),
                boneName  = humanBone.boneName,
                humanName = humanBone.humanName,
            };
        }
        
        public static HumanBone Convert(SerializableHumanBone humanBone)
        {
            return new HumanBone
            {
                limit     = SerializableHumanLimit.Convert(humanBone.limit),
                boneName  = humanBone.boneName,
                humanName = humanBone.humanName,
            };
        }
        
        public static List<SerializableHumanBone> Convert(HumanBone[] humanBones)
        {
            var converted = new List<SerializableHumanBone>(humanBones.Length);
            foreach (HumanBone bone in humanBones)
            {
                converted.Add(Convert(bone));
            }

            return converted;
        }
        
        public static HumanBone[] Convert(List<SerializableHumanBone> humanBones)
        {
            var converted = new HumanBone[humanBones.Count];
            for (int i = 0; i < humanBones.Count; ++i)
            {
                converted[i] = Convert(humanBones[i]);
            }

            return converted;
        }
    }
}