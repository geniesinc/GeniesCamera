using Unity.Collections;
using UnityEngine;

namespace Genies.Avatars
{
    /// <summary>
    /// Can solve the triangle shape weights for a given set of target points based on the given reference triangles.
    /// TSW stands for triangulated shape weights. All implementations should be serializable in order to support
    /// serialization of the <see cref="TriangulatedShape"/>.
    /// </summary>
    public interface ITswSolver
    {
        TriangulatedShapeWeights SolveWeights (MeshTriangles referenceTriangles, NativeArray<Vector3> targetPoints, Allocator allocator);
    }
}