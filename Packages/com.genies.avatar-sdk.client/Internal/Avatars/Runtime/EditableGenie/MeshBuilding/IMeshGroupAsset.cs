using UnityEngine;

namespace Genies.Avatars
{
    /// <summary>
    /// Represents a group of <see cref="MeshAsset"/> instances that can be merged together in a single Unity submesh.
    /// It is assumed that the data is static and never changes.
    /// </summary>
    public interface IMeshGroupAsset
    {
        Material Material   { get; }
        int      AssetCount { get; }
        
        MeshAsset GetAsset   (int assetIndex);
        Vector2    GetUvOffset(int assetIndex);
        Vector2    GetUvScale (int assetIndex);
    }
}