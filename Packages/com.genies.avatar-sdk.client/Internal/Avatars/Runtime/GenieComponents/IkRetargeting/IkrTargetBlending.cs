using System.Collections.Generic;
using UnityEngine;

namespace Genies.Avatars
{
    /// <summary>
    /// Static class containing our target blending algorithm.
    /// </summary>
    public static class IkrTargetBlending
    {
        public struct Result
        {
            public Vector3    PositionOffset;
            public float      PositionWeight;
            public Quaternion RotationOffset;
            public float      RotationWeight;
        }

        /// <summary>
        /// Calculates the transform blending result from the given transform to the given targets.
        /// </summary>
        public static Result BlendTargets(Vector3 position, Quaternion rotation, IEnumerable<IIkrTarget> targets)
        {
            // initialize some variables for the calculation
            Vector3    positionOffset      = Vector3.zero;
            Quaternion rotationOffset      = Quaternion.identity;
            Quaternion rotationInverse     = Quaternion.Inverse(rotation);
            float      totalPositionWeight = 0.0f;
            float      totalRotationWeight = 0.0f;
            
            // iterate over each target to get the transform offsets and total weights
            foreach (IIkrTarget target in targets)
            {
                float weight = target.Weight;
                
                if (target.HasPosition)
                {
                    positionOffset += weight * (target.Position - position);
                    totalPositionWeight += weight;
                }

                if (target.HasRotation)
                {
                    rotationOffset *= Quaternion.SlerpUnclamped(Quaternion.identity, rotationInverse * target.Rotation, weight);
                    totalRotationWeight += weight;
                }
            }
            
            // average the offsets by dividing them by their total weight (avoid divisions by zero)
            float totalPosWeightInverse = totalPositionWeight == 0.0f ? 0.0f : 1.0f / totalPositionWeight;
            float totalRotWeightInverse = totalRotationWeight == 0.0f ? 0.0f : 1.0f / totalRotationWeight;
            positionOffset *= totalPosWeightInverse;
            rotationOffset = Quaternion.SlerpUnclamped(Quaternion.identity, rotationOffset, totalRotWeightInverse);
            
            // get the final blending results
            return new Result
            {
                PositionOffset = positionOffset,
                PositionWeight = Mathf.Clamp01(totalPositionWeight / 1.0f),
                RotationOffset = rotationOffset,
                RotationWeight = Mathf.Clamp01(totalRotationWeight / 1.0f),
            };
        }
    }
}