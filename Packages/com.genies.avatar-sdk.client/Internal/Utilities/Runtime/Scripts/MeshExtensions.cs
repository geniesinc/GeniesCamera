using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Genies.Utilities
{
    public static class MeshExtensions
    {
        public static void AddBlendShape(this Mesh mesh, BlendShape blendShape)
        {
            for (int i = 0; i < blendShape.FrameCount; ++i)
            {
                BlendShapeFrame frame = blendShape.Frames[i];
                mesh.AddBlendShapeFrame(blendShape.Name, frame.Weight, frame.DeltaVertices, frame.DeltaNormals, frame.DeltaVertices);
            }
        }
        
        public static BlendShape GetBlendShape(this Mesh mesh, int shapeIndex, BlendShapeProperties properties = BlendShapeProperties.All)
        {
            int frameCount = mesh.GetBlendShapeFrameCount(shapeIndex);
            var frames = new BlendShapeFrame[frameCount];
            for (int frameIndex = 0; frameIndex < frameCount; ++frameIndex)
            {
                frames[frameIndex] = GetBlendShapeFrame(mesh, shapeIndex, frameIndex, properties);
            }

            return new BlendShape(mesh.GetBlendShapeName(shapeIndex), frames);
        }
        
        public static BlendShape GetBlendShape(this Mesh mesh, int shapeIndex, BlendShape source)
        {
            int frameCount = mesh.GetBlendShapeFrameCount(shapeIndex);
            if (source.Frames.Length != frameCount)
            {
                throw new Exception("The given source blend shape has a different frame count than the targeted blend shape from the mesh");
            }

            var blendShape = new BlendShape(mesh.GetBlendShapeName(shapeIndex), source);
            for (int frameIndex = 0; frameIndex < frameCount; ++frameIndex)
            {
                blendShape.Frames[frameIndex] = GetBlendShapeFrame(mesh, shapeIndex, frameIndex, blendShape.Frames[frameIndex]);
            }

            return blendShape;
        }

        public static BlendShapeFrame GetBlendShapeFrame(this Mesh mesh, int shapeIndex, int frameIndex, BlendShapeProperties properties = BlendShapeProperties.All)
        {
            var frame = new BlendShapeFrame(0.0f, mesh.vertexCount, properties);
            return GetBlendShapeFrame(mesh, shapeIndex, frameIndex, frame);
        }
        
        public static BlendShapeFrame GetBlendShapeFrame(this Mesh mesh, int shapeIndex, int frameIndex, BlendShapeFrame source)
        {
            if (source.VertexCount != mesh.vertexCount)
            {
                throw new Exception("The given source blend shape frame has a different vertex count than the mesh");
            }

            float weight = mesh.GetBlendShapeFrameWeight(shapeIndex, frameIndex);
            var frame = new BlendShapeFrame(weight, source);
            mesh.GetBlendShapeFrameVertices(shapeIndex, frameIndex, frame.DeltaVertices, frame.DeltaNormals, frame.DeltaTangents);
            
            return frame;
        }
        
        public static MeshBakeData BakeBlendShapes(this Mesh mesh, IEnumerable<BlendShapeState> shapeStates, BlendShapeProperties properties = BlendShapeProperties.All)
        {
            var data = new MeshBakeData(mesh, properties);
            var blendShapePool = new BlendShapePool(data.VertexCount, data.Properties);
            
            foreach (BlendShapeState shapeState in shapeStates)
            {
                BlendShape blendShape = blendShapePool.Get(mesh.GetBlendShapeFrameCount(shapeState.Index));
                blendShape = GetBlendShape(mesh, shapeState.Index, blendShape);
                blendShape.Bake(data, shapeState.Weight);
                blendShapePool.Release(blendShape);
            }
            
            return data;
        }
        
        public static async UniTask<MeshBakeData> BakeBlendShapesAsync(this Mesh mesh, IEnumerable<BlendShapeState> shapeStates, BlendShapeProperties properties = BlendShapeProperties.All)
        {
            var data = new MeshBakeData(mesh, properties);
            var blendShapePool = new BlendShapePool(data.VertexCount, data.Properties);
            
            foreach (BlendShapeState shapeState in shapeStates)
            {
                BlendShape blendShape = blendShapePool.Get(mesh.GetBlendShapeFrameCount(shapeState.Index));
                blendShape = GetBlendShape(mesh, shapeState.Index, blendShape);
                await blendShape.BakeAsync(data, shapeState.Weight);
                blendShapePool.Release(blendShape);
            }
            
            return data;
        }

        /// <summary>
        /// Translates all vertices and bindposes of the mesh by the given offset.
        /// </summary>
        public static void Translate(this Mesh mesh, Vector3 offset)
        {
            Vector3[] vertices = mesh.vertices;
            Matrix4x4[] bindposes = mesh.bindposes;
            Matrix4x4 translation = Matrix4x4.Translate(offset).inverse; // bindposes are inverted
            
            for (int i = 0; i < vertices.Length; ++i)
            {
                vertices[i] += offset;
            }

            for (int i = 0; i < bindposes.Length; ++i)
            {
                bindposes[i] *= translation;
            }

            mesh.vertices = vertices;
            mesh.bindposes = bindposes;
            mesh.RecalculateBounds();
        }
    }
}
