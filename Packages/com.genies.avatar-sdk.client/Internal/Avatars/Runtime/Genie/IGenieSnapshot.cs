using System;
using UnityEngine;

namespace Genies.Avatars
{
    /// <summary>
    /// Represents a disposable snapshot of a genie. A snapshot is not a functional avatar (no animator or skinned mesh renderer)
    /// but just a static mesh. Dispose the snapshot to release all resources associated with it (mesh, materials, textures...).
    /// </summary>
    public interface IGenieSnapshot : IDisposable
    {
        string Species { get; }
        GameObject Root { get; }
        bool IsDisposed { get; }

        event Action Disposed;
    }
}