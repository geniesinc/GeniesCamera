using System.Runtime.CompilerServices;
using UnityEngine;

namespace Genies.Utilities
{
    /// <summary>
    /// Contains some utility methods for baking mesh blend shapes. The methods are intended to be fast so they don't
    /// perform validation (checking for null arrays or differences in length) and they use aggressive inlining.
    /// </summary>
    public static class BlendShapeBakingUtility
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void BakeDeltas(Vector3[] values, Vector3[] deltas, float interpolation)
        {
            for (int i = 0; i < values.Length; ++i)
            {
                values[i] += interpolation * deltas[i];
            }
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void BakeDeltas(Vector3[] values, Vector3[] fromDeltas, Vector3[] toDeltas, float interpolation)
        {
            for (int i = 0; i < values.Length; ++i)
            {
                values[i] += fromDeltas[i] + interpolation * (toDeltas[i] - fromDeltas[i]);
            }
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void BakeTangents(Vector4[] tangents, Vector3[] deltas, float interpolation)
        {
            for (int i = 0; i < tangents.Length; ++i)
            {
                var value = new Vector3(tangents[i].x, tangents[i].y, tangents[i].z);
                value += interpolation * deltas[i];
                tangents[i] = new Vector4(value.x, value.y, value.z, tangents[i].w);
            }
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void BakeTangents(Vector4[] tangents, Vector3[] fromDeltas, Vector3[] toDeltas, float interpolation)
        {
            for (int i = 0; i < tangents.Length; ++i)
            {
                var value = new Vector3(tangents[i].x, tangents[i].y, tangents[i].z);
                value += fromDeltas[i] + interpolation * (toDeltas[i] - fromDeltas[i]);
                tangents[i] = new Vector4(value.x, value.y, value.z, tangents[i].w);
            }
        }
    }
}
