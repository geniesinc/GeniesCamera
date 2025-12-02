using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UMA;
using UnityEngine;

namespace Genies.Avatars
{
    public sealed class RefittingService : IRefittingService
    {
        // state
        private readonly AvatarBodyDeformation _avatarBodyDeformation;
        private readonly Dictionary<(OutfitAsset asset, string bodyVariation), UniTaskCompletionSource> _refittingTasks;
        private readonly IUtilityVectorService _uVecService;

        public RefittingService(IUtilityVectorService utilityVectorService)
        {
            _uVecService = utilityVectorService;
            _avatarBodyDeformation = new AvatarBodyDeformation(_uVecService);
            _refittingTasks = new Dictionary<(OutfitAsset asset, string bodyVariation), UniTaskCompletionSource>();
        }

        public UniTask LoadAllVectorsAsync()
        {
            return _avatarBodyDeformation.LoadBodyDeformVectorsAsync();
        }
        
        public string GetBodyVariationBlendShapeName(string bodyVariation)
        {
            return $"refitting.{bodyVariation}";
        }

        public async UniTask AddBodyVariationBlendShapeAsync(OutfitAsset asset, string bodyVariation)
        {
            if (asset is null || string.IsNullOrEmpty(bodyVariation))
            {
                return;
            }

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
                await InternalAddBodyVariationBlendShapeAsync(asset, bodyVariation);
            }
            catch (Exception exception)
            {
                Debug.LogError($"[{nameof(RefittingService)}] something went wrong while trying to add the body variation blendshapes for {bodyVariation} to the asset {asset.Metadata.Id}\n{exception}");
            }
            finally
            {
                _refittingTasks.Remove(taskKey);
                completionSource.TrySetResult();
            }
        }

        public UniTask WaitUntilReadyAsync()
        {
            return _avatarBodyDeformation.WaitUntilBodyDeformVectorsLoadedAsync();
        }

        private async UniTask InternalAddBodyVariationBlendShapeAsync(OutfitAsset asset, string bodyVariation)
        {
            // try to get the util mesh from the asset
            UtilMeshName utilMesh = _uVecService.GetUtilityMeshFromAssetCategory(asset);
            if (utilMesh is UtilMeshName.none)
            {
                return;
            }

            // wait for all deform vectors to load
            await _avatarBodyDeformation.WaitUntilBodyDeformVectorsLoadedAsync();
            
            // check if this body deform vector exists
            if (!_avatarBodyDeformation.AllSolvesReadyForDeformation(bodyVariation))
            {
                Debug.LogError($"[{nameof(RefittingService)}] refitting solves not ready for util mesh {utilMesh.ToString()} and body variation {bodyVariation}");
                return;
            }
            
            // apply the refitting blend shape on all slots in parallel
            string bodyVariationBlendShapeName = GetBodyVariationBlendShapeName(bodyVariation);

            // compatibility with the new non-uma builder
            if (asset.GenieType == GenieTypeName.NonUma)
            {
                await UniTask.WhenAll(asset.MeshAssets.Select
                (
                    meshAsset => AddBodyVariationBlendShapeAsync(meshAsset, bodyVariation, bodyVariationBlendShapeName, utilMesh)
                ));
                
                return;
            }
            
            // old legacy UMA system
            await UniTask.WhenAll(asset.Slots.Select
            (
                slot => AddBodyVariationBlendShapeAsync(slot, bodyVariation, bodyVariationBlendShapeName, utilMesh)
            ));
        }
        
        private async UniTask AddBodyVariationBlendShapeAsync(MeshAsset asset, string bodyVariation, string blendShapeName, UtilMeshName utilMesh)
        {
            if (IsRefittingBlendshapeApplied(asset.BlendShapes, blendShapeName))
            {
                return;
            }

            UMABlendShape blendShape = await CreateBodyVariationBlendShapeAsync(asset.Vertices, bodyVariation, blendShapeName, utilMesh);

            // add the blend shape to the mesh data
            int count = asset.BlendShapes.Length;
            Array.Resize(ref asset.BlendShapes, count + 1);
            asset.BlendShapes[count] = blendShape;
        }

        private async UniTask AddBodyVariationBlendShapeAsync(SlotDataAsset slot, string bodyVariation, string blendShapeName, UtilMeshName utilMesh)
        {
            UMAMeshData meshData = slot.meshData;
            if (IsRefittingBlendshapeApplied(meshData.blendShapes, blendShapeName))
            {
                return;
            }

            UMABlendShape blendShape = await CreateBodyVariationBlendShapeAsync(meshData.vertices, bodyVariation, blendShapeName, utilMesh);

            // add the blend shape to the mesh data
            int count = meshData.blendShapes.Length;
            Array.Resize(ref meshData.blendShapes, count + 1);
            meshData.blendShapes[count] = blendShape;
        }
        
        private async UniTask<UMABlendShape> CreateBodyVariationBlendShapeAsync(Vector3[] vertices, string bodyVariation, string blendShapeName, UtilMeshName utilMesh)
        {
            // compute the refitting deltas
            Vector3[] deltaVertices = await _avatarBodyDeformation.ComputeMeshRefitDeltasForDeformation(vertices, bodyVariation, utilMesh);
            
            // build the blend frame
            var blendFrame = new UMABlendFrame();
            blendFrame.frameWeight = 100F;
            blendFrame.deltaVertices = deltaVertices;

            // build the blend shape
            UMABlendShape blendShape = new UMABlendShape();
            blendShape.shapeName = blendShapeName;
            blendShape.frames = new [] { blendFrame };
            
            return blendShape;
        }
        
        private static bool IsRefittingBlendshapeApplied(UMABlendShape[] blendShapes, string name)
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
