using System.Collections.Generic;
using System.Runtime.InteropServices;
using GnWrappers;
using UnityEngine;

namespace Genies.Naf
{
    public static class SkeletonExtensions
    {
        /**
         * Creates a hierarchy of GameObjects rooted at the given root (or the scene root if null) from the given
         * Skeleton. Adds any generated root bones (those without parents within the given skeleton) to the given
         * rootBones list (it will not be cleared). Adds all generated bones to the given bones list (it will not be
         * cleared either). The bones list indexing will match the skeleton bones indexing.
         */
        public static void CreateSkeleton(this Skeleton skeleton, IList<Transform> rootBones, IList<Transform> bones, Transform root = null)
        {
            uint skeletonSize = skeleton.Size();
            var   nativeBones  = new Bone[skeletonSize];
            
            try
            {
                int indexOffset = bones.Count;
                
                // initialize Bone wrappers and GameObjects
                for (uint i = 0; i < skeletonSize; i++)
                {
                    nativeBones[i] = skeleton.Bone(i);
                    bones.Add(new GameObject(nativeBones[i].Name()).transform);
                }

                for (uint i = 0; i < skeletonSize; i++)
                {
                    Bone bone = nativeBones[i];
                    Transform boneTransform = bones[indexOffset + (int)i];

                    if (bone.HasParent() && !bone.IsParentExternal())
                    {
                        Transform parent = bones[indexOffset + (int)bone.ParentIndex()];
                        boneTransform.SetParent(parent, worldPositionStays: false);
                    }
                    else
                    {
                        boneTransform.SetParent(root, worldPositionStays: false);
                        rootBones.Add(boneTransform);
                    }
                    
                    boneTransform.localPosition = Marshal.PtrToStructure<Vector3>(bone.Position());
                    boneTransform.localRotation = Marshal.PtrToStructure<Quaternion>(bone.Rotation());
                    boneTransform.localScale    = Marshal.PtrToStructure<Vector3>(bone.Scale());
                }
            }
            finally
            {
                foreach (Bone bone in nativeBones)
                {
                    bone?.Dispose();
                }
            }
        }
    }
}