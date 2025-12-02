using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Genies.Refs;
using Unity.Collections;
using UnityEngine;

namespace Genies.Avatars
{
    /// <summary>
    /// Controls the SubSpecies of a <see cref="NonUmaGenie"/> instance.
    /// </summary>
    public class SubSpeciesController : IDisposable
    {
        // dependencies
        private readonly EditableGenie _genie;
        private readonly SubSpeciesLoader _subSpeciesLoader;
        
        private string _currentSubSpeciesId;
        private Ref<SubSpeciesAsset> _defaultAsset;
        private Vector3[] _defaultShapePoints;
        
        public SubSpeciesController(EditableGenie genie, SubSpeciesLoader loader, string id)
        {
            _genie = genie;
            _subSpeciesLoader = loader;
            _currentSubSpeciesId = id;
            InitializeDefault().Forget();
        }
        
        /// <summary>
        /// Load asset from provided ID.
        /// If assetId is not provided, returns the default unified Genie GAP avatar.
        /// </summary>
        public async UniTask<Ref<SubSpeciesAsset>> LoadAssetAsync(string assetId)
        {
            if (string.IsNullOrEmpty(assetId))
            {
                return _defaultAsset;
            }

            Ref<SubSpeciesAsset> assetRef = await _subSpeciesLoader.LoadAsync(assetId, _genie.Lod);
            return assetRef;
        }
        
        /// <summary>
        /// Sets the SubSpecies of the Genie instance using the asset reference.
        /// </summary>
        public async UniTask EquipAssetAsync(Ref<SubSpeciesAsset> assetRef)
        {
            _currentSubSpeciesId = assetRef.Item.Id;
            await _genie.EquipSubSpeciesAsync(assetRef);
        }

        /// <summary>
        /// Creates new UniquePointsShape for the currently equipped SubSpecies, supporting the deformation from
        /// "default" (the Unified Genie proxy mesh) to that of the equipped avatar.
        /// </summary>
        public UniquePointsShape CreateReferenceShape()
        {
            Vector3[] deformPoints = GetEquippedDeformShapeArray();
            return GenerateReferenceShape(_currentSubSpeciesId, _defaultShapePoints, deformPoints);
        }

        /// <summary>
        /// Gets deform shape vertices from the UtilityMesh of the SubSpecies currently equipped on the Genie instance.
        /// </summary>
        public Vector3[] GetEquippedDeformShapeArray()
        {
            Mesh deformMesh = _genie.GetEquippedDeformMesh();
            return GetVertsFromTrianglesArray(deformMesh);
        }
        
        /// <summary>
        /// Gets Default GAP Avatar asset (GAP-run unified Genie) to use as fallback and reference shape for refitting.
        /// TODO: Currently hardcoded to load in a local GAPBodyContainer "avatar-unified-genie". Need to provide default asset from content.
        /// </summary>
        private async UniTask InitializeDefault()
        {
            _defaultAsset = await LoadAssetAsync("avatar-unified-genie");
            _defaultShapePoints = GetVertsFromTrianglesArray(_defaultAsset.Item.UtilityMesh);
        }
        
        private static UniquePointsShape GenerateReferenceShape(string id, Vector3[] referencePoints, Vector3[] deformPoints)
        {
            // Create PointsShape from source mesh (Unified Genie)
            UniquePointsShape uniquePointsShape = new UniquePointsShape(id);
            var points = new NativeArray<Vector3>(referencePoints, Allocator.Temp);
            uniquePointsShape.Initialize(points);

            // Add GAP Avatar points as deform
            var deformedPoints = new NativeArray<Vector3>(deformPoints, Allocator.Temp);
            uniquePointsShape.AddDeform(id, deformedPoints);
            
            // Dispose of native arrays
            points.Dispose();
            deformedPoints.Dispose();

            return uniquePointsShape;
        }
        
        private static Vector3[] GetVertsFromTrianglesArray(Mesh mesh)
        {
            // Because of mesh compression, the vertices from .glb files are not in order
            // The method uses the triangle array as a map to get the vertices in order
            var tris = mesh.triangles;
            var verts = mesh.vertices;
            var outVerts = new Vector3[tris.Length];
            for (int i = 0; i < tris.Length; i++)
            {
                outVerts[i] = verts[tris[i]];
            }

            return outVerts;
        }
        
        public void Dispose()
        {
            _defaultAsset.Dispose();
        }

    }
}
