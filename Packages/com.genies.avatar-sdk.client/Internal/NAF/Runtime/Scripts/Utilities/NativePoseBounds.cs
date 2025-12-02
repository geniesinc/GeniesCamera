using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Genies.Utilities;

namespace Genies.Naf
{
    /**
     * Encapsulates the bounds and grounding hips offset for a collection of skinned mesh renderers when in a specific
     * skeleton pose. It provides methods to perform and invalidate the calculation.
     */
    public sealed class NativePoseBounds
    {
        /**
         * True if Bounds and GroundingHipsOffset are currently valid (calculation was performed and not invalidated).
         */
        public bool IsCalculationValid { get; private set; }

        /**
         * The calculated bounds. It can be null if no calculation was ever performed or InvalidateCalculation() was
         * called.
         */
        public Bounds? Bounds { get; private set; }

        /**
         * The offset that should be applied to the human hips local position in order to ground the calculated bounds.
         * This only makes sense if the skeleton contains a hips transform. Otherwise, the hips would be considered to
         * be at the world origin. It can be null if no calculation was ever performed or InvalidateCalculation() was
         * called.
         */
        public Vector3? GroundingHipsOffset { get; private set; }

        /**
         * The renderers used to calculate the pose bounds.
         */
        public IReadOnlyList<SkinnedMeshRenderer> Renderers { get; private set; }

        /**
         * The skeleton used to set the pose for the bounds calculation.
         */
        public NativeSkeleton Skeleton { get; private set; }

        /**
         * The transform used as the root for the bounds calculation. If null, the bounds will be calculated in world space.
         */
        public Transform BoundsRoot { get; private set; }

        /**
         * Fired when the calculation is dirtied (when performed or invalidated).
         */
        public event Action CalculationDirtied;

        public NativePoseBounds()
            => Setup(null, null);

        public NativePoseBounds(IEnumerable<SkinnedMeshRenderer> renderers, NativeSkeleton skeleton, Transform boundsRoot = null)
            => Setup(renderers, skeleton, boundsRoot);

        public void Setup(IEnumerable<SkinnedMeshRenderer> renderers, NativeSkeleton skeleton, Transform boundsRoot = null)
        {
            if (renderers is null)
            {
                Renderers = new List<SkinnedMeshRenderer>().AsReadOnly();
            }
            else
            {
                Renderers = renderers.ToList().AsReadOnly();
            }

            Skeleton   = skeleton;
            BoundsRoot = boundsRoot;

            InvalidateCalculation();
        }

        /**
         * Recalculates the bounds and grounding hips offset for the current skeleton pose (if applyHumanPose is false).
         * Set applyHumanPose to true to set the skeleton to its human pose (or default if not human) before the
         * calculation. In that case, the skeleton's pose will be reset before returning.
         *
         * This function is expensive.
         */
        public void PerformCalculation(bool applyHumanPose = false)
        {
            if (Renderers.Count == 0 || Skeleton is null)
            {
                Bounds              = default;
                GroundingHipsOffset = default;
                IsCalculationValid  = true;
                return;
            }

            if (applyHumanPose)
            {
                // apply the skeleton human pose (fallback to default pose if no human description is set)
                Skeleton.ApplyHumanPose(applyDefaultIfNotInHumanDescription: true);
            }

            // fetch relevant transforms
            Transform skeletonRoot = Skeleton.Root;
            Transform hips         = Skeleton.HipsBone;

            // calculate pose matrices
            Matrix4x4 worldPose        = skeletonRoot && skeletonRoot.parent ? skeletonRoot.parent.localToWorldMatrix : Matrix4x4.identity;
            Matrix4x4 rootPose         = BoundsRoot                          ? BoundsRoot.localToWorldMatrix : Matrix4x4.identity;
            Matrix4x4 skeletonRootPose = skeletonRoot                        ? skeletonRoot.localToWorldMatrix : Matrix4x4.identity;
            Matrix4x4 hipsParentPose   = hips && hips.parent                 ? hips.parent.localToWorldMatrix : Matrix4x4.identity;

            // calculate the bounds for the current skeleton pose and make sure we restore the skeleton after
            try
            {
                CalculateBounds(in worldPose, in rootPose);
                CalculateGroundingHipsOffset(in worldPose, in rootPose, in skeletonRootPose, in hipsParentPose);
                IsCalculationValid = true;
            }
            catch (Exception exception)
            {
                Debug.LogError($"[{nameof(NativePoseBounds)}] Exception while calculating pose bounds: {exception}");
                IsCalculationValid  = false;
                Bounds              = null;
                GroundingHipsOffset = null;
            }
            finally
            {
                if (applyHumanPose)
                {
                    Skeleton.RestorePose();
                }
            }

            CalculationDirtied?.Invoke();
        }

        /**
         * Invalidates previous calculation (sets bounds and grounding hips offset to null).
         */
        public void InvalidateCalculation()
        {
            if (!IsCalculationValid)
            {
                return;
            }

            IsCalculationValid  = false;
            Bounds              = null;
            GroundingHipsOffset = null;

            CalculationDirtied?.Invoke();
        }

        private void CalculateBounds(in Matrix4x4 worldPose, in Matrix4x4 rootPose)
        {
            // get the mixed bounds from all renderers
            Bounds bounds = GetRendererBoundsForCurrentPose(in worldPose);

            // transform the bounds to the root's local space
            Matrix4x4 rootPoseInv = rootPose.inverse;
            bounds.center = worldPose.MultiplyPoint3x4(bounds.center);
            bounds.center  = rootPoseInv.MultiplyPoint3x4(bounds.center);
            bounds.extents = worldPose.MultiplyVector(bounds.extents);
            bounds.extents = rootPoseInv.MultiplyVector(bounds.extents);

            Bounds = bounds;
        }

        private void CalculateGroundingHipsOffset(
            in Matrix4x4 worldPose,
            in Matrix4x4 rootPose,
            in Matrix4x4 skeletonRootPose,
            in Matrix4x4 hipsParentPose
        ) {
            /**
             * We want to ground on the world Y axis. The "world" will be represented by the _worldPose matrix, which is
             * the parent of the skeleton root (if no parent, it will be the identity matrix). First we need to get the
             * bounds in the local space to our world matrix. Then calculate the offset on the Y axis needed to ground
             * the hips and transform that offset into the hips parent local space so we can apply it to the hips local
             * position.
             */

            Matrix4x4 invWorldPose = worldPose.inverse;

            // bounds are in root's local space, so transform it to the world's local space
            Bounds bounds = Bounds.Value;
            bounds.center = rootPose.MultiplyPoint3x4(bounds.center);
            bounds.center = invWorldPose.MultiplyPoint3x4(bounds.center);
            bounds.extents = rootPose.MultiplyVector(bounds.extents);
            bounds.extents = invWorldPose.MultiplyVector(bounds.extents);

            // get the skeleton root's position in the world's local space
            Vector3 skeletonRootPosition = invWorldPose.MultiplyPoint3x4(skeletonRootPose.GetPosition());

            float groundOffset = -bounds.min.y + skeletonRootPosition.y;

            // get the offset vector in the hips parent local space and return it
            var offset = new Vector3(0.0f, groundOffset, 0.0f);
            offset = worldPose.MultiplyVector(offset);
            offset = hipsParentPose.inverse.MultiplyVector(offset);

            GroundingHipsOffset = offset;
        }

        private Bounds GetRendererBoundsForCurrentPose(in Matrix4x4 worldPose)
        {
            var bounds = new Bounds(Vector3.zero, float.MinValue * Vector3.one);
            bool foundBounds = false;

            foreach (SkinnedMeshRenderer renderer in Renderers)
            {
                if (!renderer.sharedMesh)
                {
                    continue;
                }

                /**
                 * Calculate the bounds relative to the "world" matrix so it is axis aligned. IMPORTANT: since bounds
                 * area always axis-aligned, if we calculate them in the bounds root's local space, they will cause
                 * unprecise grounding offsets. So even though we are setting the final bounds into the bounds root's
                 * local space, its important we calculate them in the "world" space first, and transform it after.
                 */
                Bounds rendererBounds = renderer.GetPoseBounds(worldPose);
                bounds.Encapsulate(rendererBounds);
                foundBounds = true;
            }

            return foundBounds ? bounds : new Bounds();
        }
    }
}
