using System;
using System.Collections.Generic;
using GnWrappers;
using UnityEngine;
using Material = UnityEngine.Material;

namespace Genies.Naf
{
    /**
     * Base class for native mesh renderers, which can render native meshes with native materials.
     */
    public abstract class NativeMeshRenderer : MonoBehaviour
    {
        /**
         * The current materials set to the renderer (as NativeMaterial).
         */
        public IReadOnlyList<NativeMaterial> Materials => _readOnlyMaterials ??= _materials.AsReadOnly();
        
        public event Action UpdatedMesh;
        public event Action UpdatedMaterials;
        
        private Renderer _renderer;
        private readonly List<NativeMaterial> _materials = new();
        private IReadOnlyList<NativeMaterial> _readOnlyMaterials;
        
        protected void SetRenderer(Renderer renderer)
        {
            if (renderer == _renderer)
            {
                return;
            }

            if (_renderer)
            {
                _renderer.sharedMaterials = Array.Empty<Material>();
            }

            _renderer = null;
            _renderer = renderer;
            UpdateRenderer();
        }

        protected void TriggerUpdatedMesh()
        {
            UpdatedMesh?.Invoke();
        }
        
        protected virtual void OnDestroy()
        {
            UpdatedMesh = null;
            UpdatedMaterials = null;
            
            if (_renderer)
            {
                _renderer.sharedMaterials = Array.Empty<Material>();
            }

            _renderer = null;
            ClearMaterials();
        }
        
        public abstract void SetMesh(RuntimeMesh runtimeMesh);
        public abstract void ClearMesh();
        
        /**
         * Sets the RuntimeMesh attribute from the given entity, if any.
         */
        public virtual void SetMesh(Entity entity)
        {
            using RuntimeMesh runtimeMesh = RuntimeMesh.GetFrom(entity);
            SetMesh(runtimeMesh);
        }

        public virtual void SetMeshAndMaterials(RuntimeMesh runtimeMesh)
        {
            SetMesh(runtimeMesh);
            SetMaterials(runtimeMesh);
        }

        public virtual void SetMeshAndMaterials(Entity entity)
        {
            using RuntimeMesh runtimeMesh = RuntimeMesh.GetFrom(entity);
            SetMesh(runtimeMesh);
            SetMaterials(runtimeMesh);
        }

        /**
         * Sets the renderer materials to the ones found in the given RuntimeMesh primitives.
         */
        public void SetMaterials(RuntimeMesh runtimeMesh)
        {
            if (runtimeMesh.IsNull())
            {
                ClearMaterials();
                return;
            }
            
            runtimeMesh.UpdatePrimitiveMaterials(_materials);
            UpdateRenderer();
        }

        /**
         * Sets the renderer materials to the given native materials wrappers (it won't own or dispose them).
         */
        public void SetMaterials(IEnumerable<GnWrappers.Material> materials)
        {
            int i = 0;
            foreach (GnWrappers.Material material in materials)
            {
                if (_materials.Count > i)
                {
                    _materials[i].SetMaterial(material);
                }
                else
                {
                    _materials.Add(new NativeMaterial(material));
                }

                ++i;
            }
            
            int count = i;
            for (i = _materials.Count - 1; i >= count; --i)
            {
                _materials[i].Dispose();
                _materials.RemoveAt(i);
            }
            
            UpdateRenderer();
        }

        /**
         * Clears the current native materials from the renderer.
         */
        public void ClearMaterials()
        {
            if (_renderer)
            {
                _renderer.sharedMaterials = Array.Empty<Material>();
            }

            // dispose the native materials
            foreach (NativeMaterial material in _materials)
            {
                material.Dispose();
            }

            _materials.Clear();
        }
        
        private void UpdateRenderer()
        {
            if (_materials.Count == 0)
            {
                _renderer.sharedMaterials = Array.Empty<Material>();
                UpdatedMaterials?.Invoke();
                return;
            }
            
            var materials = new Material[_materials.Count];
            for (int i = 0; i < materials.Length; ++i)
            {
                materials[i] = _materials[i].Material;
            }

            _renderer.sharedMaterials = materials;
            UpdatedMaterials?.Invoke();
        }
    }
}