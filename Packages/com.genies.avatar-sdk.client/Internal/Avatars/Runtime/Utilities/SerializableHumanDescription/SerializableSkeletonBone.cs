using System;
using System.Collections.Generic;
using UnityEngine;

namespace Genies.Avatars
{
    [Serializable]
    public struct SerializableSkeletonBone
    {
        public string     name;
        public Vector3    position;
        public Quaternion rotation;
        public Vector3    scale;
        
        public static SerializableSkeletonBone Convert(SkeletonBone skeletonBone)
        {
            return new SerializableSkeletonBone
            {
                name     = skeletonBone.name,
                position = skeletonBone.position,
                rotation = skeletonBone.rotation,
                scale    = skeletonBone.scale,
            };
        }
        
        public static SkeletonBone Convert(SerializableSkeletonBone skeletonBone)
        {
            return new SkeletonBone
            {
                name     = skeletonBone.name,
                position = skeletonBone.position,
                rotation = skeletonBone.rotation,
                scale    = skeletonBone.scale,
            };
        }
        
        public static List<SerializableSkeletonBone> Convert(SkeletonBone[] skeletonBones)
        {
            var converted = new List<SerializableSkeletonBone>(skeletonBones.Length);
            foreach (SkeletonBone bone in skeletonBones)
            {
                converted.Add(Convert(bone));
            }

            return converted;
        }
        
        public static SkeletonBone[] Convert(List<SerializableSkeletonBone> skeletonBones)
        {
            var converted = new SkeletonBone[skeletonBones.Count];
            for (int i = 0; i < skeletonBones.Count; ++i)
            {
                converted[i] = Convert(skeletonBones[i]);
            }

            return converted;
        }
    }
}