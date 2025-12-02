using System.Collections;

namespace Genies.Avatars
{
    /// <summary>
    /// Represents a unique flags array of triangles from a target mesh asset. This is our equivalent to UMA's mesh hide
    /// assets.
    /// </summary>
    public struct MeshAssetTriangleFlags
    {
        public string   Id; // id of this triangle flags, should be unique among other instances
        public string   TargetMeshAssetId;
        public BitArray Triangles;
    }
}