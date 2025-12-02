using System;
using UnityEngine;

namespace Genies.Naf
{
    /**
     * Given a native skeleton, an array of bones and an array of mesh bindposes (inverted), this struct determines the
     * best root bone within the skeleton and its bind pose. The array of bones is assumed to not have any null bones
     * and all of them being part of the native skeleton. The bones can have a length of zero, in which case the root
     * bone will be null. The bindposes array cannot be null and must have the same length as the bones array.
     */
    public struct NativeRootBone
    {
        public Transform Bone;
        public Matrix4x4 Bindpose;

        public NativeRootBone(NativeSkeleton skeleton, Transform[] bones, Matrix4x4[] bindposes)
        {
            Bone     = null;
            Bindpose = Matrix4x4.identity;

            // first try to find a common parent for all the given bones. This will be the root bone
            Transform rootBone = FindCommonParent(bones);
            if (!rootBone)
            {
                return;
            }

            // see if the root bone is already within the bones array, so we can use its original bindpose
            int rootBoneIndex = Array.IndexOf(bones, rootBone);
            if (rootBoneIndex >= 0)
            {
                Bone     = rootBone;
                Bindpose = bindposes[rootBoneIndex];
                return;
            }

            /**
             * The root bone was not within the given bones. This is fine as it still could be part of the skeleton. In
             * that case, use the first bone as a reference to calculate the root bone bindpose in the mesh's bindpose
             * space.
             */
            bool foundRootPose = skeleton.TryGetDefaultPose(rootBone.name, out Matrix4x4 rootSkeletonPose);
            bool foundRefPose  = skeleton.TryGetDefaultPose(bones[0].name, out Matrix4x4 refSkeletonPos);
            if (!foundRefPose || !foundRootPose)
            {
                return;
            }

            Bone     = rootBone;
            Bindpose = rootSkeletonPose.inverse * refSkeletonPos * bindposes[0];
        }

        /**
         * Returns the common parent for the given transforms, or null if no common parent can be found. This function
         * assumes that no null transforms are present in the array.
         */
        public static Transform FindCommonParent(Transform[] transforms)
        {
            if (transforms is not { Length: > 0 })
            {
                return null;
            }

            if (transforms.Length == 1)
            {
                return transforms[0];
            }

            Transform commonParent = transforms[0];
            while (commonParent)
            {
                bool isCommonParent = true;
                foreach (Transform transform in transforms)
                {
                    if (!transform.IsChildOf(commonParent))
                    {
                        commonParent = commonParent.parent;
                        isCommonParent = false;
                        break;
                    }
                }

                if (isCommonParent)
                {
                    break;
                }
            }

            return commonParent;
        }
    }
}
