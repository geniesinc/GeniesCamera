using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Genies.Utilities
{
    public static class BlendShapeBaker
    {
        public static Mesh GenerateBakedMesh(SkinnedMeshRenderer renderer, Config config)
            => GenerateBakedMeshAsync(renderer, config, async: false).GetAwaiter().GetResult();

        public static UniTask<Mesh> GenerateBakedMeshAsync(SkinnedMeshRenderer renderer, Config config)
            => GenerateBakedMeshAsync(renderer, config, async: true);

        private static async UniTask<Mesh> GenerateBakedMeshAsync(SkinnedMeshRenderer renderer, Config config, bool async)
        {
            Mesh mesh = renderer.sharedMesh;
            if (!mesh)
            {
                throw new Exception("The given renderer must have a mesh in order to generate a baked mesh");
            }

            Mesh bakedMesh = Object.Instantiate(renderer.sharedMesh);

            try
            {
                await GenerateBakedMeshAsync(mesh, bakedMesh, renderer, config, async);
                return bakedMesh;
            }
            catch(Exception)
            {
                // if an exception occurs we want to destroy the baked mesh instance
                Object.Destroy(bakedMesh);
                throw;
            }
        }

        private static async UniTask GenerateBakedMeshAsync(Mesh mesh, Mesh bakedMesh, SkinnedMeshRenderer renderer, Config config, bool async)
        {
            // if there is no blend shapes do nothing
            if (bakedMesh.blendShapeCount == 0)
            {
                return;
            }

            ValidateConfig(config);
            var processedShapeIndices = new HashSet<int>();

            // clear all blend shapes from the baked mesh first, we will later re-add merged and included ones
            bakedMesh.ClearBlendShapes();

            // create a blend shape pool to optimize heap allocations for merges and includes
            var blendShapePool = new BlendShapePool(mesh.vertexCount);

            // merge and add blend shapes first
            if (config.ShapesToMerge is not null)
            {
                foreach (ShapeMerge shapeMerge in config.ShapesToMerge)
                {
                    CreateAndAddShapeMerge(shapeMerge, mesh, bakedMesh, blendShapePool, processedShapeIndices);
                }
            }

            // bake blend shapes
            if (config.ShapesToBake is not null)
            {
                await BakeBlendShapesAsync(renderer, mesh, bakedMesh, config.ShapesToBake, processedShapeIndices, async);
            }

            // add included blend shapes
            if (config.ShapesToInclude is null)
            {
                return;
            }

            foreach (int shapeIndex in config.ShapesToInclude)
            {
                // ignore if invalid index
                if (shapeIndex < 0 || shapeIndex >= mesh.blendShapeCount)
                {
                    continue;
                }

                // ignore if already baked or merged
                if (processedShapeIndices.Contains(shapeIndex))
                {
                    Debug.LogWarning($"[{nameof(BlendShapeBaker)}] blend shape {mesh.GetBlendShapeName(shapeIndex)} will not be included because it was has been merged or baked");
                    continue;
                }

                // get blend shape from the source mesh and add it back to the backed mesh
                int frameCount = mesh.GetBlendShapeFrameCount(shapeIndex);
                BlendShape blendShape = blendShapePool.Get(frameCount);
                blendShape = mesh.GetBlendShape(shapeIndex, blendShape);
                bakedMesh.AddBlendShape(blendShape);
                blendShapePool.Release(blendShape);
            }
        }

        private static void CreateAndAddShapeMerge(ShapeMerge shapeMerge, Mesh source, Mesh destination,
            BlendShapePool blendShapePool, HashSet<int> processedShapeIndices)
        {
            if (shapeMerge.Indices is null)
            {
                return;
            }

            int mergeIndex = -1;
            int currentShapeIndex = -1;
            if (!NextShapeIndex())
            {
                return;
            }

            // get the first blend shape frame count to get a blend shape instance from the pool
            int frameCount = source.GetBlendShapeFrameCount(currentShapeIndex);
            BlendShape blendShape = blendShapePool.Get(frameCount);

            // extract the blend shape data from the mesh and rename it with the shapeMerge name
            blendShape = source.GetBlendShape(currentShapeIndex, blendShape);
            blendShape = new BlendShape(shapeMerge.Name, blendShape);

            // merge the rest of the blend shapes (all of them must have the same frame count)
            while (NextShapeIndex())
            {
                // skip merging this current shape if it doesn't have the same frame count as the first one
                if (source.GetBlendShapeFrameCount(currentShapeIndex) != frameCount)
                {
                    Debug.LogWarning($"[{nameof(BlendShapeBaker)}] blend shape {source.GetBlendShapeName(currentShapeIndex)} will not be merged into {shapeMerge.Name} because it has a different frame count from the first");
                    continue;
                }

                // get a new blend shape from the pool, get the data from the mesh and merge it to the first blend shape
                BlendShape blendShapeToMerge = blendShapePool.Get(frameCount);
                blendShapeToMerge = source.GetBlendShape(currentShapeIndex, blendShapeToMerge);
                blendShape.MergeWith(blendShapeToMerge);
                blendShapePool.Release(blendShapeToMerge);
            }

            // add the merged blend shape to the destination mesh and release it to the pool
            destination.AddBlendShape(blendShape);
            blendShapePool.Release(blendShape);

            // forwards currentShapeIndex to the next valid index within the shape merge indices
            bool NextShapeIndex()
            {
                while (++mergeIndex < shapeMerge.Indices.Count)
                {
                    currentShapeIndex = shapeMerge.Indices[mergeIndex];
                    if (currentShapeIndex < 0 || currentShapeIndex >= source.blendShapeCount)
                    {
                        continue;
                    }

                    processedShapeIndices.Add(currentShapeIndex);
                    return true;
                }

                return false;
            }
        }

        private static async UniTask BakeBlendShapesAsync(SkinnedMeshRenderer renderer, Mesh source, Mesh destination, List<int> shapesToBake, HashSet<int> processedShapeIndices, bool async)
        {
            // get the shape states for shapes to bake that were not merged
            List<BlendShapeState> shapeStates = new List<BlendShapeState>(shapesToBake.Count);
            foreach (int shapeIndex in shapesToBake)
            {
                if (processedShapeIndices.Contains(shapeIndex))
                {
                    Debug.LogWarning($"[{nameof(BlendShapeBaker)}] blend shape {source.GetBlendShapeName(shapeIndex)} will not be baked because it was has been merged");
                    continue;
                }

                shapeStates.Add(renderer.GetBlendShapeState(shapeIndex));
            }

            // bake blend shapes into the destination mesh
            MeshBakeData bakeData = async ? await source.BakeBlendShapesAsync(shapeStates) : source.BakeBlendShapes(shapeStates);
            bakeData.ApplyTo(destination);
            processedShapeIndices.UnionWith(shapesToBake);
        }

        private static void ValidateConfig(Config config)
        {
            // remove duplicates from indices lists
            if (config.ShapesToBake is not null)
            {
                RemoveDuplicates(config.ShapesToBake);
            }

            if (config.ShapesToInclude is not null)
            {
                RemoveDuplicates(config.ShapesToBake);
            }

            if (config.ShapesToMerge is null)
            {
                return;
            }

            // throw if we find invalid or duplicated output names for the shapes to merge
            var nameSet = new HashSet<string>();
            foreach (ShapeMerge shapeMerge in config.ShapesToMerge)
            {
                if (string.IsNullOrEmpty(shapeMerge.Name))
                {
                    throw new Exception($"[{nameof(BlendShapeBaker)}] found null or empty blend shape merge output name");
                }

                if (nameSet.Contains(shapeMerge.Name))
                {
                    throw new Exception($"[{nameof(BlendShapeBaker)}] found duplicated blend shape merge output name: {shapeMerge.Name}");
                }

                nameSet.Add(shapeMerge.Name);
            }

            void RemoveDuplicates<T>(List<T> list)
            {
                for (int i = 0; i < list.Count; ++i)
                {
                    if (list.IndexOf(list[i]) < i)
                    {
                        list.RemoveAt(i--);
                    }
                }
            }
        }

        public struct Config
        {
            /// <summary>
            /// The blend shapes to merge. The given merge groups should have indices from blend shapes having the same
            /// number of frames.
            /// </summary>
            public List<ShapeMerge> ShapesToMerge;

            /// <summary>
            /// The blend shapes to bake (excluding those to merge).
            /// </summary>
            public List<int> ShapesToBake;

            /// <summary>
            /// The blend shapes to leave as they were on the original mesh (excluding those to merge or bake).
            /// Blend shapes not included here that are not set to merge or bake will be excluded from the final mesh.
            /// </summary>
            public List<int> ShapesToInclude;
        }

        public struct ShapeMerge
        {
            public string Name;
            public List<int> Indices;
        }
    }
}
