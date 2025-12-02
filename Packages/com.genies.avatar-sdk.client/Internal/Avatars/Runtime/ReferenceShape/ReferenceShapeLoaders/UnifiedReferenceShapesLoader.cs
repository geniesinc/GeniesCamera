using System;
using System.Collections.Generic;
using System.Diagnostics;
using Cysharp.Threading.Tasks;
using Unity.Collections;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Genies.Avatars
{
    public abstract class UnifiedReferenceShapesLoader : ReferenceShapesLoaderAsset
    {
        private static readonly UtilMeshName[] AllUtilMeshNames = Enum.GetValues(typeof(UtilMeshName)) as UtilMeshName[];

        public bool verbose;
        public bool lockDeforms = true;

        [Space(8), Tooltip("Single utility mesh that will be used for all utility meshes. Leave empty to use multiple utility meshes")]
        public Mesh singleUtilityMesh;
        [Tooltip("If empty, the name of the single utility mesh asset will be used instead")]
        public string singleUtilityMeshId;
        public List<string> unifiedDnaDeforms = new() { "female", "male", "gap" };
        public List<ReferenceUtilityMesh> referenceUtilityMeshes = new();

        protected abstract UniTask<IReferenceShape> InitializeShapeAsync(Mesh mesh, string id);

        public override async UniTask<Dictionary<string, IReferenceShape>> LoadShapesAsync()
        {
            Stopwatch stopwatch = verbose ? Stopwatch.StartNew() : null;
            var shapes = new Dictionary<string, IReferenceShape>();

            if (singleUtilityMesh)
            {
                string id = string.IsNullOrEmpty(singleUtilityMeshId) ? singleUtilityMesh.name : singleUtilityMeshId;
                IReferenceShape shape = await InitializeShapeAsync(singleUtilityMesh, id);
                AddDeforms(shape, unifiedDnaDeforms, singleUtilityMesh);
                foreach (UtilMeshName name in AllUtilMeshNames)
                {
                    shapes[name.ToString()] = shape;
                }

                LogInitializationTime(stopwatch);
                return shapes;
            }

            await UniTask.WhenAll(referenceUtilityMeshes.Select(InitializeReferenceUtilityMeshAsync));

            LogInitializationTime(stopwatch);
            return shapes;

            async UniTask InitializeReferenceUtilityMeshAsync(ReferenceUtilityMesh utilMesh)
            {
                string id = string.IsNullOrEmpty(utilMesh.meshId) ? singleUtilityMesh.name : utilMesh.meshId;
                IReferenceShape shape = await InitializeShapeAsync(utilMesh.mesh, id);
                AddDeforms(shape, utilMesh.unifiedDnaDeforms, utilMesh.mesh);
                foreach (UtilMeshName name in utilMesh.utilityMeshNames)
                {
                    shapes[name.ToString()] = shape;
                }
            }
        }

        private void AddDeforms(IReferenceShape shape, IEnumerable<string> unifiedDnaDeforms, Mesh mesh)
        {
            // initialize building dependencies
            using var builder = new MeshDeformBuilder(mesh, Allocator.TempJob);
            using var deformVertices = new NativeArray<Vector3>(mesh.vertexCount, Allocator.TempJob);

            foreach (string deform in unifiedDnaDeforms)
            {
                using MeshDeformationDescriptor descriptor = UnifiedDnaDeformsHelper.GetDeformationDescriptor(deform, mesh, Allocator.TempJob);
                builder.BuildDeform(descriptor, deformVertices);
                shape.AddDeform(deform, deformVertices);
            }

            if (lockDeforms)
            {
                shape.LockDeforms();
            }
        }

        private void LogInitializationTime(Stopwatch stopwatch)
        {
            if (!verbose)
            {
                return;
            }

            stopwatch.Stop();
            Debug.Log($"<color=orange>[{nameof(UnifiedReferenceShapesLoader)}]</color> Initialized reference meshes in <color=cyan>{stopwatch.Elapsed.TotalMilliseconds:0.00} ms</color>");
        }

        [Serializable]
        public struct ReferenceUtilityMesh
        {
            public Mesh mesh;
            public string meshId;
            public List<UtilMeshName> utilityMeshNames;
            public List<string> unifiedDnaDeforms;
        }
    }
}
