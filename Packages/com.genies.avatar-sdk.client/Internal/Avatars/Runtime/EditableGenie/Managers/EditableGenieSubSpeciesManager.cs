using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Unity.Collections;
using UnityEngine;

namespace Genies.Avatars
{
    internal sealed class EditableGenieSubSpeciesManager : IDisposable
    {
        public Mesh EquippedDeformMesh => _currentAsset.UtilityMesh;

        private readonly EditableGenie _genie;
        private readonly MeshBuilder _meshBuilder;

        // state
        private SubSpeciesAsset _currentAsset;
        private SubSpeciesAsset _defaultAsset;

        public EditableGenieSubSpeciesManager(EditableGenie genie, MeshBuilder meshBuilder)
        {
            _genie = genie;
            _meshBuilder = meshBuilder;
        }

        /// <summary>
        /// Will remove existing SubSpecies body assets and replace them with the new one provided.
        /// When used outside of initialization, this function should only be called in context of
        /// UnifiedGAPGenieController, so that clothing assets and refitting service are refreshed.
        /// </summary>
        public UniTask EquipSubSpecies(SubSpeciesAsset asset)
        {
            if (_currentAsset != null && asset.Id == _currentAsset.Id)
            {
                // The SubSpecies is the same, so the SubSpecies is already "applied"
                return UniTask.CompletedTask;
            }

            // dispose of existing asset
            _currentAsset?.Dispose();

            _currentAsset = asset;
            var baseMeshAssets = _currentAsset.MeshAssets;
            if (baseMeshAssets != null && baseMeshAssets.Count > 0)
            {
                // Clear MeshBuilder of all existing assets
                if (_meshBuilder.Assets.Count > 0)
                {
                    //_meshBuilder.Clear();
                    List<MeshAsset> assets = _meshBuilder.Assets.ToList();
                    _meshBuilder.Remove(assets);
                }

                // Add new assets from provided asset
                _meshBuilder.Add(baseMeshAssets);
            }
            else
            {
                Debug.LogError($"[{nameof(EditableGenieSubSpeciesManager)}] No base mesh assets were provided by GAPBodyAsset!");
            }

            return UniTask.CompletedTask;
        }

        public void Dispose()
        {
            _currentAsset?.Dispose();
            _defaultAsset?.Dispose();
        }
    }
}
