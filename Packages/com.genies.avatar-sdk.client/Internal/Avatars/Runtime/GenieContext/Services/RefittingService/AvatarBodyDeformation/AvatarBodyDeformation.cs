using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using UnityEngine;
using MathNet.Numerics.LinearAlgebra;

namespace Genies.Avatars
{
    public sealed class AvatarBodyDeformation
    {
        private const string MaleBodyDeformKey = "male";
        private const string FemaleBodyDeformKey = "female";
        private const string SourceDeformKey = "source";

        // dependencies
        private readonly IUtilityVectorService _utilityVectorService;

        //persistence
        private readonly AvatarDeformationDataSource _dataSource;

        // computation caches
        private readonly Dictionary<UtilMeshName, SourceData> _precomputedSourceData = new Dictionary<UtilMeshName, SourceData>();

        // Radial Basis Function Interpolation
        private readonly RbfInterpolation _rbfInterpolation = new RbfInterpolation();

        // accounting
        private UniTaskCompletionSource _loadingOperation;

        public AvatarBodyDeformation(IUtilityVectorService utilityVectorService)
        {
            _utilityVectorService = utilityVectorService;

            _dataSource = new AvatarDeformationDataSource();
        }

        public bool AllSolvesReadyForDeformation(string deformKey)
        {
            return _dataSource != null && _dataSource.IsDeformationReady(deformKey);
        }

        public async UniTask WaitUntilBodyDeformVectorsLoadedAsync()
        {
            if (_loadingOperation is null)
            {
                await LoadBodyDeformVectorsAsync();
                return;
            }

            await _loadingOperation.Task;
        }

        public Task<Vector3[]> ComputeMeshRefitDeltasForDeformation(in Vector3[] targetPoints, string deformKey, UtilMeshName utilmeshKey)
        {
            DeformDriverData data         = _dataSource.GetDriverData(deformKey, utilmeshKey);
            Vector3[]        sourcePoints = _precomputedSourceData[utilmeshKey].uniquePoints;
            Matrix<float>    weightMatrix = data.WeightMatrix;
            return _rbfInterpolation.DeformTargetAsync(targetPoints, sourcePoints, weightMatrix);
        }

        public async UniTask LoadBodyDeformVectorsAsync()
        {
            if (_loadingOperation != null)
            {
                await _loadingOperation.Task;
                return;
            }

            _loadingOperation = new UniTaskCompletionSource();

            try
            {
                await InternalLoadBodyDeformVectorsAsync();
            }
            catch (AggregateException aggregateException)
            {
                foreach (Exception e in aggregateException.InnerExceptions)
                {
                    Debug.LogError(e.Message);
                }

                Debug.LogError(new AvatarBodyDeformException("Failed to load body deform vectors"));
            }
            catch (Exception exception)
            {
                Debug.LogError(exception);
            }

            _loadingOperation.TrySetResult();
        }

        private async Task InternalLoadBodyDeformVectorsAsync()
        {
            UtilityVector[] vectors = await UniTask.WhenAll(new[]
            {
                LoadUtilityVectorAsync(SourceDeformKey),
                LoadUtilityVectorAsync(MaleBodyDeformKey),
                LoadUtilityVectorAsync(FemaleBodyDeformKey),
            });

            // once all vectors have loaded (especially source), we can run the solves
            var completionTasks = ListPool<Task>.Get();
            foreach (UtilityVector vector in vectors)
            {
                PopulateDeformSolveTasks(vector, completionTasks);
            }

            // separate await-and-process result approach from 'Concurrency in C# Cookbook' allows the tasks to run
            // in parallel but have their results processed as soon as each individual task finishes.
            await Task.WhenAll(completionTasks);
            ListPool<Task>.Release(completionTasks);

            // after all tasks are done persist results
            _dataSource.PersistData();
        }

        private UniTask<UtilityVector> LoadUtilityVectorAsync(string vectorId)
        {
            return UniTask.FromResult<UtilityVector>(null);
            // Unreachable code - commented out for now
            /*
            UtilityVector vector = await _utilityVectorService.LoadAsync(vectorId);
            if (vector is null)
            {
                Debug.LogError($"[{nameof(AvatarBodyDeformation)}] couldn't load utility vector for ID {vectorId}");
                return null;
            }

            string deformKey = vector.Name;
            bool pointsUpdated = false;

            foreach (UtilMesh utilMesh in vector.UtilMeshes)
            {
                UtilMeshName utilMeshName = utilMesh.Name;
                if (utilMesh.Regions.Count < 1)
                {
                    Debug.LogError($"[{nameof(AvatarBodyDeformation)}] did not find any regions under {utilMeshName.ToString()}");
                    continue;
                }

                if (deformKey.Equals(SourceDeformKey))
                {
                    // deformKey = 'source' has the undeformed reference points
                    // compute source distance matrix which will be used along with deform points for weight matrix solve
                    Vector3[] uniqueSourcePoints = utilMesh.Regions[0].UniquePoints;
                    _rbfInterpolation.ComputeDistanceMatrix(uniqueSourcePoints, uniqueSourcePoints, out Matrix<float> srcDistMatrix);
                    _precomputedSourceData[utilMeshName] = new SourceData(uniqueSourcePoints, srcDistMatrix);
                }
                else if (!_dataSource.HasLatestDriverData(vector, utilMeshName))
                {
                    _dataSource.SetDriverData(deformKey, utilMeshName, new DeformDriverData(utilMesh.Regions[0].UniquePoints));
                    pointsUpdated = true;
                }
            }

            if (pointsUpdated) {
                _dataSource.SetVersion(deformKey, vector.Version);
                _dataSource.MarkDeformKeyProcessed(deformKey, false);
            }

            return vector;
            */
        }

        private void PopulateDeformSolveTasks(UtilityVector vector, List<Task> completionTasks)
        {
            if (vector is null)
            {
                return;
            }

            string deformKey = vector.Name;

            // Source doesn't need processing
            if (deformKey.Equals(SourceDeformKey))
            {
                return;
            }

            //No need to reprocess the deformation since the data is already persisted, unless we're forcing a reload
            if (_dataSource.IsDeformationReady(deformKey) && _dataSource.IsVersionCurrent(vector))
            {
                return;
            }

            var utilMeshSolveTasks = ListPool<Task>.Get();

            foreach (var um in vector.UtilMeshes)
            {
                var utilmeshKey = um.Name;

                if (!_dataSource.HasDeformData(deformKey))
                {
                    Debug.LogError($"Did not find data for '{deformKey}'");
                    continue;
                }

                if (!_dataSource.HasLatestDriverData(vector, utilmeshKey))
                {
                    Debug.LogError($"Did not find data for '{deformKey}' utility mesh {utilmeshKey.ToString()}");
                    continue;
                }

                if (!_precomputedSourceData.ContainsKey(utilmeshKey))
                {
                    Debug.LogError($"Precomputed source data for {utilmeshKey.ToString()} not found, can't run RBF Interpolation without it.");
                    return;
                }

                if (_precomputedSourceData[utilmeshKey].srcDistanceMatrix == null)
                {
                    Debug.LogError($"Precomputed source distance matrix for {utilmeshKey.ToString()} is null, can't run RBF Interpolation without it");
                    return;
                }

                /*
                    srcPts: precomputedSourceData[utilmeshKey].uniquePoints
                    defPts: cachedDriverData[deformKey][utilmeshKey].uniquePoints
                    srcDistMat: precomputedSourceData[utilmeshKey].srcDistanceMatrix
                    => out weightMatrix:  cachedDriverData[deformKey][utilmeshKey].weightMatrix
                    launch rbfsolve
                */

                var solveKey = $"{deformKey}_{utilmeshKey}";
                var data     = _dataSource.GetDriverData(deformKey, utilmeshKey);
                Task<Matrix<float>> solveTask = _rbfInterpolation.CalcRBFDriverDataAsync(
                                                                                         solveKey,
                                                                                         _precomputedSourceData[utilmeshKey].uniquePoints,
                                                                                         data.uniquePoints,
                                                                                         _precomputedSourceData[utilmeshKey].srcDistanceMatrix
                                                                                        );

                //Add task
                utilMeshSolveTasks.Add(AwaitAndProcessSolveTask(deformKey, utilmeshKey, solveTask));
            }

            //Solve all tasks then mark key completed
            async Task SolveAndComplete()
            {
                //Wait for solves
                await Task.WhenAll(utilMeshSolveTasks);

                //Mark deform solves complete
                MarkDeformSolveCompleted(deformKey);

                //Release pool
                ListPool<Task>.Release(utilMeshSolveTasks);
            }

            //Add to awaited tasks
            completionTasks.Add(SolveAndComplete());
        }

        private async Task AwaitAndProcessSolveTask(string deformKey, UtilMeshName utilmeshKey, Task<Matrix<float>> weightMatrixTask)
        {
            var result = await weightMatrixTask; // wait for $$$ solve to happen

            DeformDriverData data = _dataSource.GetDriverData(deformKey, utilmeshKey);
            data.WeightMatrix = result;
            _dataSource.SetDriverData(deformKey, utilmeshKey, data);

        }

        private void MarkDeformSolveCompleted(string deformKey)
        {
            //Mark completed
            _dataSource.MarkDeformKeyProcessed(deformKey, true);
            Debug.Log($"All utility meshes solved for '{deformKey}'");
        }
    }
}
