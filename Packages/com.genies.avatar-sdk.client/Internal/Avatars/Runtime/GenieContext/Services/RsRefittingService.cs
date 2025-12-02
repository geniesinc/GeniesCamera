using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UMA;
using Unity.Collections;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Genies.Avatars
{
    /// <summary>
    /// Specific <see cref="IRefittingService"/> implementation that uses <see cref="IReferenceShape"/> implementations.
    /// RS stands for reference shape.
    /// </summary>
    public sealed class RsRefittingService : IRefittingService, IDisposable
    {
        public string BlendShapePrefix = "refitting.";
        
        /// <summary>
        /// Enable this so deformation transfers are calculated in a background thread.
        /// </summary>
        public bool RunAsync;
        
        // dependencies
        private readonly IReferenceShapesLoader _shapesLoader;
        private readonly IReferenceShape _referenceShape;
        
        // state
        private readonly Dictionary<string, IReferenceShape> _shapes;
        private readonly Dictionary<(OutfitAsset asset, string bodyVariation), UniTaskCompletionSource> _refittingTasks;
        private UniTaskCompletionSource _readyCompletionSource;

        public RsRefittingService(IReferenceShapesLoader shapesLoader)
        {
            _shapesLoader   = shapesLoader;
            _shapes         = new Dictionary<string, IReferenceShape>();
            _refittingTasks = new Dictionary<(OutfitAsset asset, string bodyVariation), UniTaskCompletionSource>();
        }
        
        public RsRefittingService(IReferenceShape shape)
        {
            _shapesLoader = null;
            _referenceShape = shape;
            _shapes         = new Dictionary<string, IReferenceShape>();
            _refittingTasks = new Dictionary<(OutfitAsset asset, string bodyVariation), UniTaskCompletionSource>();
        }
        
        public async UniTask LoadAllVectorsAsync()
        {
            if (_readyCompletionSource is not null)
            {
                await _readyCompletionSource.Task;
                return;
            }
            
            _readyCompletionSource = new UniTaskCompletionSource();
            
            if (_shapesLoader is not null)
            {
                Dictionary<string, IReferenceShape> shapes = await _shapesLoader.LoadShapesAsync();
                foreach ((string id, IReferenceShape reference) in shapes)
                {
                    Debug.Log($"Shape ID {id} with reference shape {reference.Id}");
                    _shapes.Add(id, reference);
                }
            } else if (_referenceShape is not null)
            {
                foreach (UtilMeshName value in Enum.GetValues(typeof(UtilMeshName)))
                {
                    if (value != UtilMeshName.none)
                    {
                        _shapes.Add(value.ToString(), _referenceShape);
                    }
                }
            }
            
            _readyCompletionSource.TrySetResult();
        }
        
        public UniTask WaitUntilReadyAsync()
        {
            return LoadAllVectorsAsync();
        }

        public string GetBodyVariationBlendShapeName(string bodyVariation)
        {
            return BlendShapePrefix + bodyVariation;
        }
        
        public async UniTask AddBodyVariationBlendShapeAsync(OutfitAsset asset, string bodyVariation)
        {
            // TODO: update outfit asset with correct bindpose for GAP avatar

            if (asset is null || string.IsNullOrEmpty(bodyVariation))
            {
                return;
            }

            await WaitUntilReadyAsync();

            // if a refitting asset for this asset and this body variation is already taking place then await for it and return
            var taskKey = (asset, bodyVariation);
            if (_refittingTasks.TryGetValue(taskKey, out UniTaskCompletionSource completionSource))
            {
                await completionSource.Task;
                return;
            }
            
            _refittingTasks[taskKey] = completionSource = new UniTaskCompletionSource();

            try
            {
                if (RunAsync)
                {
                    await UniTask.SwitchToThreadPool();
                    AddDeformBlendShape(asset, bodyVariation);
                    await UniTask.SwitchToMainThread();
                }
                else
                {
                    AddDeformBlendShape(asset, bodyVariation);
                }
            }
            catch (Exception exception)
            {
                Debug.LogError($"[{nameof(RsRefittingService)}] something went wrong while trying to add the body variation blendshapes for {bodyVariation} to the asset {asset.Metadata.Id}\n{exception}");
            }
            finally
            {
                _refittingTasks.Remove(taskKey);
                completionSource.TrySetResult();
            }
        }
        
        public void AddDeformShape(string deformId, NativeArray<Vector3> deformPoints)
        {
            if (string.IsNullOrEmpty(deformId) || deformPoints.Length == 0)
            {
                return;
            }

            _referenceShape.AddDeform(deformId, deformPoints);
        }

        
        public void Dispose()
        {
            _referenceShape?.Dispose();

            foreach (IReferenceShape reference in _shapes.Values)
            {
                reference?.Dispose();
            }

            _shapes.Clear();
            _refittingTasks.Clear();
        }

        private async UniTaskVoid DisposeOnMainThead()
        {
            await UniTask.SwitchToMainThread();
            Dispose();
        }
        
        ~RsRefittingService()
        {
            DisposeOnMainThead().Forget();
        }

        private void AddDeformBlendShape(OutfitAsset asset, string deformId)
        {
            // try to get the util mesh name for this asset
            if (asset.Metadata.Subcategory == null
                || !UtilityMeshConverter.UtilMeshFromAssetCategory.TryGetValue(asset.Metadata.Subcategory, out UtilMeshName utilMesh)
                || utilMesh is UtilMeshName.none)
            {
                return;
            }
            
            // check that the reference shape for this util mesh exists and that it contains the deform ID
            if (!_shapes.TryGetValue(utilMesh.ToString(), out IReferenceShape shape))
            {
                throw new Exception($"No reference shape found for util mesh: {utilMesh.ToString()}");
            }

            if (!shape.ContainsDeform(deformId))
            {
                throw new Exception($"Reference shape for util mesh {utilMesh.ToString()} doesn't contain the deform ID: {deformId}");
            }

            // compatibility with the new non-uma builder
            if (asset.GenieType == GenieTypeName.NonUma)
            {
                foreach (MeshAsset meshAsset in asset.MeshAssets)
                {
                    AddDeformBlendShape(meshAsset, deformId, shape);
                }
            }
            else
            {
                // old legacy UMA system
                foreach (SlotDataAsset slot in asset.Slots)
                {
                    AddDeformBlendShape(slot, deformId, shape);
                }
            }
        }
        
        private void AddDeformBlendShape(MeshAsset asset, string deformId, IReferenceShape shape)
        {
            string blendShapeName = GetBodyVariationBlendShapeName(deformId);
            if (ContainsBlendShape(asset.BlendShapes, blendShapeName))
            {
                return;
            }

            UMABlendShape blendShape = CreateDeformBlendShape(asset.Id, asset.Vertices, deformId, shape);
            blendShape.shapeName = blendShapeName;
            AddBlendShape(ref asset.BlendShapes, blendShape);
        }

        private void AddDeformBlendShape(SlotDataAsset slot, string deformId, IReferenceShape shape)
        {
            string blendShapeName = GetBodyVariationBlendShapeName(deformId);
            UMAMeshData meshData = slot.meshData;
            if (ContainsBlendShape(meshData.blendShapes, blendShapeName))
            {
                return;
            }

            UMABlendShape blendShape = CreateDeformBlendShape(slot.name, meshData.vertices, deformId, shape);
            blendShape.shapeName = blendShapeName;
            AddBlendShape(ref meshData.blendShapes, blendShape);
        }
        
        private UMABlendShape CreateDeformBlendShape(string targetId, Vector3[] targetVertices, string deformId, IReferenceShape shape)
        {
            var blendFrame = new UMABlendFrame
            {
                frameWeight = 100F,
                deltaVertices = shape.TransferDeformAsDeltas(deformId, targetVertices, targetId),
            };

            return new UMABlendShape
            {
                frames = new [] { blendFrame }
            };
        }

        private static void AddBlendShape(ref UMABlendShape[] blendShapes, UMABlendShape blendShape)
        {
            Array.Resize(ref blendShapes, blendShapes.Length + 1);
            blendShapes[^1] = blendShape;
        }
        
        private static bool ContainsBlendShape(UMABlendShape[] blendShapes, string name)
        {
            for (int i = 0; i < blendShapes.Length; ++i)
            {
                if (blendShapes[i].shapeName == name)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
