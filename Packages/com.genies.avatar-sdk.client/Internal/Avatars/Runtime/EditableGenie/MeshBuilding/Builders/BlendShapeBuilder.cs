using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Cysharp.Threading.Tasks;
using UMA;
using UnityEngine;

namespace Genies.Avatars
{
    /// <summary>
    /// While the <see cref="MeshDataBuilder"/> can build blend shapes on its own, its very inefficient since it has
    /// to initialize all the vertex buffers for each blend shape. This builder provides a more efficient way of
    /// building <see cref="MeshAsset"/> blend shapes directly into the final mesh asset. You can use this in
    /// combination with the <see cref="MeshDataBuilder"/> by disabling its <see cref="MeshDataBuilder.BuildBlendShapes"/>
    /// property.
    /// <br/><br/>
    /// In order for this to work it is assumed that the mesh/group assets used to rebuild the blendshapes are given
    /// in the same order that was used to build the mesh data.
    /// </summary>
    public sealed class BlendShapeBuilder
    {
        public int BlendShapeCount => _blendShapes.Count;
        public int VertexCount     => _vertexCount;
        
        private readonly List<BlendShapeData>               _blendShapes       = new();
        private readonly Stack<BlendShapeData>              _blendShapesPool   = new();
        private readonly Dictionary<string, BlendShapeData> _blendShapesByName = new();
        
        private int _vertexCount;
        private Vector3[] _deltaVertices;
        private Vector3[] _deltaNormals;
        private Vector3[] _deltaTangents;

        public BlendShapeBuilder()
        { }

        public void Dispose()
        {
            // clear blend shape data
            foreach (BlendShapeData data in _blendShapes)
            {
                data.Clear();
            }

            foreach (BlendShapeData data in _blendShapesByName.Values)
            {
                data.Clear();
            }

            foreach (BlendShapeData data in _blendShapesPool)
            {
                data.Clear();
            }


            // clear collections
            _blendShapes.Clear();
            _blendShapesPool.Clear();
            _blendShapesByName.Clear();

            // clear vector3 arrays
            if (_deltaVertices != null)
            {
                Array.Clear(_deltaVertices, 0, _deltaVertices.Length);
                _deltaVertices = null;
            }
            if (_deltaNormals != null)
            {
                Array.Clear(_deltaNormals, 0, _deltaNormals.Length);
                _deltaNormals = null;
            }
            if (_deltaTangents != null)
            {
                Array.Clear(_deltaTangents, 0, _deltaTangents.Length);
                _deltaTangents = null;
            }

            // reset count
            _vertexCount = 0;
        }
          
        public void Rebuild(IEnumerable<IMeshGroupAsset> groupAssets)
        {
            ClearBuild();
            
            foreach (IMeshGroupAsset groupAsset in groupAssets)
            {
                for (int i = 0; i < groupAsset.AssetCount; ++i)
                {
                    AddAsset(groupAsset.GetAsset(i));
                }
            }
        }
        
        public void Rebuild(IEnumerable<MeshAsset> assets)
        {
            ClearBuild();
            
            foreach (MeshAsset asset in assets)
            {
                AddAsset(asset);
            }
        }

        public void ClearBuild()
        {
            foreach (BlendShapeData info in _blendShapes)
            {
                ReleaseBlendShape(info); // adds to blend shapes pool, so don't clear pool (need to dispose of that data later)
            }

            _blendShapes.Clear();
            _blendShapesByName.Clear();
            _vertexCount = 0;
        }
        
        /// <summary>
        /// Applies the current built blend shapes to the given mesh. You can optionally provide with already
        /// initialized vertex buffers for the operation, but their lengths must match the <see cref="VertexCount"/>.
        /// </summary>
        public async UniTask ApplyTo(Mesh mesh, Vector3[] deltaVertices = null, Vector3[] deltaNormals = null, Vector3[] deltaTangents = null, bool spreadCompute = false)
        {
            if (deltaVertices is not null && deltaVertices.Length != _vertexCount)
            {
                throw new Exception($"[{nameof(BlendShapeBuilder)}] provided delta vertices array doesn't match current vertex count");
            }

            if (deltaNormals is not null && deltaNormals.Length != _vertexCount)
            {
                throw new Exception($"[{nameof(BlendShapeBuilder)}] provided delta normals array doesn't match current vertex count");
            }

            if (deltaTangents is not null && deltaTangents.Length != _vertexCount)
            {
                throw new Exception($"[{nameof(BlendShapeBuilder)}] provided delta tangents array doesn't match current vertex count");
            }

            _deltaVertices = deltaVertices ?? new Vector3[VertexCount];
            _deltaNormals = deltaNormals ?? new Vector3[VertexCount];
            _deltaTangents = deltaTangents ?? new Vector3[VertexCount];

            // Apply and break up the compute a bit
            int index = 0;
            int framesBeforeWait = 20;
            foreach (BlendShapeData blendShape in _blendShapes)
            {
                ApplyTo(blendShape, mesh);
                if (++index % framesBeforeWait == 0 && spreadCompute)
                {
                    await UniTask.DelayFrame(1);
                }
            }

            _deltaVertices = null;
            _deltaNormals  = null;
            _deltaTangents = null;
        }
        
        private void ApplyTo(BlendShapeData blendShape, Mesh mesh)
        {
            
            // every blend shape data must at least have one asset
            ShapePointer firstPointer = blendShape.Pointers[0];
            UMABlendShape firstShape = firstPointer.Asset.BlendShapes[firstPointer.ShapeIndex];
            int frameCount = firstShape.frames.Length;

            for (int frameIndex = 0; frameIndex < frameCount; ++frameIndex)
            {
                int vertexIndex = 0;

                for (int pointerIndex = 0; pointerIndex < blendShape.PointerCount; ++pointerIndex)
                {
                    ShapePointer pointer = blendShape.Pointers[pointerIndex];
                    
                    // make sure to clear the vertex buffers space between assets
                    int clearVertexCount = pointer.VertexIndexOffset - vertexIndex;
                    ClearBuffers(ref vertexIndex, clearVertexCount, blendShape.HasNormals, blendShape.HasTangents);

                    // copy the pointed blend shape frame to the buffers
                    UMABlendFrame frame = pointer.Asset.BlendShapes[pointer.ShapeIndex].frames[frameIndex];
                    CopyBlendShapeFrameToBuffers(frame, ref vertexIndex, pointer.Asset.Vertices.Length,
                        blendShape.HasNormals, blendShape.HasTangents);
                }
                
                // clear the vertex buffers space remaining after the last asset
                int remainingVertexCount = _vertexCount - vertexIndex;
                ClearBuffers(ref vertexIndex, remainingVertexCount, blendShape.HasNormals, blendShape.HasTangents);

                float weight = firstShape.frames[frameIndex].frameWeight;
                mesh.AddBlendShapeFrame(blendShape.Name, weight,
                    _deltaVertices,
                    blendShape.HasNormals  ? _deltaNormals : null,
                    blendShape.HasTangents ? _deltaTangents : null);
            }
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ClearBuffers(ref int vertexIndex, in int vertexCount,
            in bool hasNormals, in bool hasTangents)
        {
            // not pretty but is the fastest way to implement this, and we really need performance on this class
            if (hasNormals)
            {
                if (hasTangents)
                {
                    for (int i = 0; i < vertexCount; ++i, ++vertexIndex)
                    {
                        _deltaVertices[vertexIndex] = Vector3.zero;
                        _deltaNormals [vertexIndex] = Vector3.zero;
                        _deltaTangents[vertexIndex] = Vector3.zero;
                    }
                    
                    return;
                }
                
                for (int i = 0; i < vertexCount; ++i, ++vertexIndex)
                {
                    _deltaVertices[vertexIndex] = Vector3.zero;
                    _deltaNormals [vertexIndex] = Vector3.zero;
                }
                
                return;
            }
            
            for (int i = 0; i < vertexCount; ++i, ++vertexIndex)
            {
                _deltaVertices[vertexIndex] = Vector3.zero;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void CopyBlendShapeFrameToBuffers(UMABlendFrame frame, ref int vertexIndex, in int vertexCount,
            in bool hasNormals, in bool hasTangents)
        {
            // not pretty but is the fastest way to implement this, and we really need performance on this class
            if (hasNormals)
            {
                if (hasTangents)
                {
                    for (int i = 0; i < vertexCount; ++i, ++vertexIndex)
                    {
                        _deltaVertices[vertexIndex] = frame.deltaVertices[i];
                        _deltaNormals [vertexIndex] = frame.deltaNormals[i];
                        _deltaTangents[vertexIndex] = frame.deltaTangents[i];
                    }
                    
                    return;
                }
                
                for (int i = 0; i < vertexCount; ++i, ++vertexIndex)
                {
                    _deltaVertices[vertexIndex] = frame.deltaVertices[i];
                    _deltaNormals [vertexIndex] = frame.deltaNormals[i];
                }
                
                return;
            }
            
            for (int i = 0; i < vertexCount; ++i, ++vertexIndex)
            {
                _deltaVertices[vertexIndex] = frame.deltaVertices[i];
            }
        }
        
        private void AddAsset(MeshAsset asset)
        {
            // process asset blend shapes
            for (int i = 0; i < asset.BlendShapes.Length; ++i)
            {
                string name = asset.BlendShapes[i].shapeName;
                
                // if the blend shape data was already created, then just add this asset to it
                if (_blendShapesByName.TryGetValue(name, out BlendShapeData blendShape))
                {
                    blendShape.Add(asset, i, _vertexCount);
                    continue;
                }
                
                // no blend shape data existed, so create a new one, then initialize and register it
                blendShape = NewBlendShape();
                blendShape.Initialize(asset, i, _vertexCount);
                _blendShapes.Add(blendShape);
                _blendShapesByName.Add(name, blendShape);
            }
            
            _vertexCount += asset.Vertices.Length;
        }

        private BlendShapeData NewBlendShape()
        {
            return _blendShapesPool.TryPop(out BlendShapeData blendShape) ? blendShape : new BlendShapeData();
        }

        private void ReleaseBlendShape(BlendShapeData blendShape)
        {
            blendShape.Clear();
            _blendShapesPool.Push(blendShape);
        }

        private readonly struct ShapePointer
        {
            public readonly MeshAsset Asset;
            public readonly int        ShapeIndex;
            public readonly int        VertexIndexOffset;

            public ShapePointer(MeshAsset asset, int shapeIndex, int vertexIndexOffset)
            {
                Asset = asset;
                ShapeIndex = shapeIndex;
                VertexIndexOffset = vertexIndexOffset;
            }
        }
        
        private sealed class BlendShapeData
        {
            private const int PointerCapacity = 256;
            
            public string Name;
            public int    FrameCount;
            public bool   HasNormals;
            public bool   HasTangents;
            public int    PointerCount;
            
            public readonly List<float>    FrameWeights = new();
            public readonly ShapePointer[] Pointers = new ShapePointer[PointerCapacity];
            
            public void Initialize(MeshAsset asset, int shapeIndex, int vertexIndexOffset)
            {
                Clear();
                UMABlendShape shape = asset.BlendShapes[shapeIndex];
                
                Name = shape.shapeName;
                FrameCount = shape.frames.Length;
                HasNormals = false;
                HasTangents = false;
                PointerCount = 1;
                FrameWeights.Clear();
                Pointers[0] = new ShapePointer(asset, shapeIndex, vertexIndexOffset);

                if (FrameCount == 0)
                {
                    Debug.LogError($"[{nameof(BlendShapeBuilder)}] found mesh asset blend shape with 0 frames: {Name}");
                    Clear();
                    return;
                }
                
                HasNormals  = shape.frames[0].HasNormals();
                HasTangents = shape.frames[0].HasTangents();
                FrameWeights.Add(shape.frames[0].frameWeight);

                for (int i = 1; i < FrameCount; ++i)
                {
                    bool hasNormals  = shape.frames[i].HasNormals();
                    bool hasTangents = shape.frames[i].HasTangents();

                    if (hasNormals != HasNormals || hasTangents != HasTangents)
                    {
                        Debug.LogError($"[{nameof(BlendShapeBuilder)}] found mesh asset blend shape where frame normal/tangent buffers differ (some frames have the buffers and some not): {Name}");
                        Clear();
                        return;
                    }
                    
                    HasNormals  |= hasNormals;
                    HasTangents |= hasTangents;
                    FrameWeights.Add(shape.frames[i].frameWeight);
                }
            }

            // assumes that this instance is already initialized and that the caller already checked that the given shapeIndex for the given asset has the same blend shape name
            public void Add(MeshAsset asset, int shapeIndex, int vertexIndexOffset)
            {
                // means this instance is not initialized or initialization failed for the first added shape
                if (Name is null)
                {
                    return;
                }

                // means this blendshape doesn't need to be added
                if (!IsShapeValid(asset, shapeIndex))
                {
                    return;
                }

                // don't add the asset if it cannot be merged
                if (!IsShapeCompatible(asset, shapeIndex))
                {
                    Debug.LogWarning($"[{nameof(BlendShapeBuilder)}] found unmerge-able mesh asset {asset.Id} for blend shape: {Name} ");
                    return;
                }
                
                if (PointerCount == PointerCapacity)
                {
                    throw new Exception($"[{nameof(BlendShapeBuilder)}] reached max mesh assets per blend shape: {Name}");
                }

                Pointers[PointerCount++] = new ShapePointer(asset, shapeIndex, vertexIndexOffset);

                // update has normals/tangents flags based on what this asset has
                for (int i = 0; i < FrameCount; ++i)
                {
                    HasNormals  |= asset.BlendShapes[shapeIndex].frames[i].HasNormals();
                    HasTangents |= asset.BlendShapes[shapeIndex].frames[i].HasTangents();
                }
            }
            
            public void Clear()
            {
                Name = null;
                FrameCount = 0;
                HasNormals = false;
                HasTangents = false;
                FrameWeights.Clear();
                PointerCount = 0;
                Array.Clear(Pointers, 0, Pointers.Length);
            }
            
            private bool IsShapeCompatible(MeshAsset asset, int shapeIndex)
            {
                // must have same number of frames
                UMABlendShape shape = asset.BlendShapes[shapeIndex];
                if (shape.frames.Length != FrameCount)
                {
                    return false;
                }

                // must have same frame weights and vertex buffers
                for (int i = 0; i < FrameCount; ++i)
                {
                    UMABlendFrame frame = shape.frames[i];
                    if (frame.frameWeight != FrameWeights[i] || frame.HasNormals() != HasNormals || frame.HasTangents() != HasTangents)
                    {
                        return false;
                    }
                }
            
                return true;
            }
            
            private bool IsShapeValid(MeshAsset asset, int shapeIndex)
            {
                if (shapeIndex < 0)
                {
                    return false;
                }

                // must have some frames
                UMABlendShape shape = asset.BlendShapes[shapeIndex];
                if (shape.frames.Length == 0)
                {
                    return false;
                }

                for (int i = 0; i < shape.frames.Length; ++i)
                {
                    // check that the frame has vertices, frame weight is > 0, and that the frames have valid deltas
                    UMABlendFrame frame = shape.frames[i];
                    if (frame.deltaVertices.Length == 0 
                        || frame.frameWeight <= 0
                        || UMABlendFrame.isAllZero(frame.deltaVertices))
                    {
                        return false;
                    }
                }
            
                return true;
            }
        }
    }
}