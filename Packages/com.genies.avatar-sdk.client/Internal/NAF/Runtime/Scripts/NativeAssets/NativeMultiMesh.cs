using System;
using System.Collections.Generic;
using GnWrappers;

namespace Genies.Naf
{
    /**
     * This class provides a convenient way to deal with native multi-mesh entities. Native multi-mesh entities contain
     * multiple meshes (as child entities) that share the same skeleton. Each child entity must be named with a unique
     * ID so each mesh can be identified across multiple rebuilds. The result entity from the AssetBuilder is a
     * multi-mesh entity.
     */
    public sealed class NativeMultiMesh : IDisposable
    {
        public Entity              Entity   { get; private set; }
        public Skeleton            Skeleton { get; private set;}
        public IReadOnlyList<Mesh> Meshes   { get; }

        private readonly List<Mesh> _meshes;

        public NativeMultiMesh()
        {
            _meshes = new List<Mesh>();
            Meshes = _meshes.AsReadOnly();
        }

        public NativeMultiMesh(Entity entity) : this()
        {
            SetEntity(entity);
        }

        /**
         * Sets the current multi-mesh entity and refreshes. The given entity can be null if you want to clear all data.
         */
        public void SetEntity(Entity entity)
        {
            if (entity == Entity)
            {
                Refresh();
                return;
            }

            Entity?.Dispose();
            Entity = entity;
            Refresh();
        }

        /**
         * Refreshes the multi-mesh data for the current entity.
         */
        public void Refresh()
        {
            Skeleton?.Dispose();
            Skeleton = null;

            ClearMeshes();

            if (Entity is null || Entity.IsNull())
            {
                return;
            }

            using var sharedSkeleton = SharedSkeleton.GetFrom(Entity);
            if (!sharedSkeleton.IsNull())
            {
                Skeleton = sharedSkeleton.Skeleton();
            }

            using var node = EntityNode.GetFrom(Entity);
            if (node.IsNull())
            {
                return;
            }

            int childCount = node.ChildCount();
            for (int i = 0; i < childCount; ++i)
            {
                using EntityNode childNode = node.Child(i);
                if (childNode.IsNull())
                {
                    continue;
                }

                using Entity child = childNode.Owner();
                if (child.IsNull())
                {
                    continue;
                }

                var runtimeMesh = RuntimeMesh.GetFrom(child);
                if (runtimeMesh.IsNull())
                {
                    runtimeMesh.Dispose();
                    continue;
                }

                var mesh = new Mesh
                {
                    Id           = child.Name(),
                    RuntimeMesh  = runtimeMesh,
                    SharedJoints = sharedSkeleton.IsNull() ? null : sharedSkeleton.Joints(i),
                    Report       = RuntimeMeshCombinerReport.GetFrom(child)
                };

                _meshes.Add(mesh);
            }
        }

        public void Dispose()
        {
            Entity?.Dispose();
            Skeleton?.Dispose();
            Entity = null;
            Skeleton = null;

            ClearMeshes();
        }

        private void ClearMeshes()
        {
            foreach (Mesh mesh in _meshes)
            {
                mesh.RuntimeMesh?.Dispose();
                mesh.SharedJoints?.Dispose();
                mesh.Report?.Dispose();
            }

            _meshes.Clear();
        }

        public struct Mesh
        {
            public string                    Id;
            public RuntimeMesh               RuntimeMesh;
            public VectorSizeT               SharedJoints;
            public RuntimeMeshCombinerReport Report;
        }
    }
}
