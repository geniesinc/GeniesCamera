using System;
using GnWrappers;
using UnityEngine;

namespace Genies.Naf
{
    /**
     * <see cref="NativeMeshRenderer"/> implementation that uses a <see cref="MeshRenderer"/>.
     */
    public sealed class StaticNativeMeshRenderer : NativeMeshRenderer
    {
        public MeshRenderer Renderer => renderer;
        
        // inspector
        [SerializeField] private new MeshRenderer renderer;
        
        private MeshFilter _meshFilter;
        private Mesh _mesh;
        private bool _triggerUpdatedEvent = true;
        
        protected override void OnDestroy()
        {
            base.OnDestroy();
            ClearMesh();
        }
        
        public void SetRenderer(MeshRenderer renderer)
        {
            if (!renderer)
            {
                throw new NullReferenceException("Renderer is null");
            }

            if (renderer == this.renderer)
            {
                return;
            }

            _meshFilter = renderer.GetComponent<MeshFilter>();
            _meshFilter.sharedMesh = _mesh;
            
            this.renderer = renderer;
            base.SetRenderer(renderer);
        }

        public override void SetMesh(RuntimeMesh runtimeMesh)
        {
            _triggerUpdatedEvent = false;
            ClearMesh();
            _triggerUpdatedEvent = true;
            
            if (runtimeMesh.IsNull())
            {
                return;
            }

            _mesh = runtimeMesh.CreateMesh(ignoreSkinning: true);
            
            // update the mesh filter and renderer bounds
            _meshFilter.sharedMesh = _mesh;
            renderer.localBounds = _mesh.bounds;
            
            TriggerUpdatedMesh();
        }
        
        public override void ClearMesh()
        {
            if (_mesh)
            {
                Destroy(_mesh);
            }

            _mesh = null;
            _meshFilter ??= renderer.GetComponent<MeshFilter>();
            _meshFilter.sharedMesh = null;
            
            if (_triggerUpdatedEvent)
            {
                TriggerUpdatedMesh();
            }
        }
    }
}