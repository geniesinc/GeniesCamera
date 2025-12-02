using System;
using System.Runtime.CompilerServices;

namespace Genies.Avatars
{
    /// <summary>
    /// Set collection of <see cref="MeshAssetTriangleFlags"/> targeting <see cref="TargetMeshAssetId"/>. The collection
    /// will ensure no duplicated tirangle flags with the same id. It's optimized for high performance flag retrieval.
    /// </summary>
    public sealed class MeshAssetTriangleFlagsSet
    {
        public int Count => _count;
        
        public readonly string TargetMeshAssetId;
        public readonly int    Capacity;

        private readonly MeshAssetTriangleFlags[] _triangles;

        private int _count;

        public MeshAssetTriangleFlagsSet(string targetMeshAssetId, int capacity)
        {
            TargetMeshAssetId = targetMeshAssetId;
            Capacity = capacity;
            _triangles = new MeshAssetTriangleFlags[capacity];
        }

        public void Add(MeshAssetTriangleFlags triangles)
        {
            if (triangles.TargetMeshAssetId != TargetMeshAssetId)
            {
                throw new Exception($"[{nameof(MeshAssetTriangleFlagsSet)}] adding triangle flags targeted to another mesh asset:\nTarget: {TargetMeshAssetId}\nTriangles Target:{triangles.TargetMeshAssetId}");
            }

            if (Contains(triangles))
            {
                return;
            }

            if (_count == Capacity)
            {
                throw new Exception($"[{nameof(MeshAssetTriangleFlagsSet)}] reached maximum capacity: {Capacity}");
            }

            _triangles[_count++] = triangles;
        }
            
        public void Remove(MeshAssetTriangleFlags triangles)
        {
            int index = IndexOf(triangles.Id);
            if (index > -1)
            {
                RemoveAt(index);
            }
        }

        public void Clear()
        {
            for (int i = 0; i < _count; ++i)
            {
                _triangles[i] = default;
            }

            _count = 0;
        }

        public bool Contains(MeshAssetTriangleFlags triangles)
        {
            return IndexOf(triangles.Id) > -1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool HasTriangle(int index)
        {
            for (int i = 0; i < _count; ++i)
            {
                if (_triangles[i].Triangles[index])
                {
                    return true;
                }
            }
            
            return false;
        }

        private int IndexOf(string trianglesId)
        {
            for (int i = 0; i < _count; ++i)
            {
                if (_triangles[i].Id == trianglesId)
                {
                    return i;
                }
            }
            
            return -1;
        }

        private void RemoveAt(int index)
        {
            for (int i = index + 1; i < _count; ++i)
            {
                _triangles[i] = _triangles[i - 1];
            }

            _triangles[--_count] = default;
        }
    }
}