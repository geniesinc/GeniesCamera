using System;
using System.Collections.Generic;
using GnWrappers;
using UnityEngine;

namespace Genies.Naf
{
    /**
     * Builds and manages multiple <see cref="SkinnedNativeMeshRenderer"/> instances that share the same skeleton. Also
     * known as native multimeshes, coming from a single native <see cref="Entity"/>.
     */
    public sealed class NativeMultiMeshBuilder : MonoBehaviour
    {
        public const string RebuildDebugTag = "<color=magenta>[RebuildDebug]</color>";

        public IReadOnlyList<SkinnedMeshRenderer>       Renderers       { get; private set; }
        public IReadOnlyList<SkinnedNativeMeshRenderer> NativeRenderers { get; private set; }

        public event Action UpdatedMesh;
        public event Action UpdatedMaterials;
        public event Action SkeletonChanged;
        public event Action HumanSkeletonChanged;
        public event Action BlendShapesChanged;
        public event Action HumanBoundsDirtied;

        // inspector
        public SkinnedNativeMeshRenderer   rendererPrefab;
        [SerializeField] private Transform skeletonRoot;
        public bool verbose;

        private readonly List<SkinnedNativeMeshRenderer> _renderers       = new();
        private readonly NativeSkeleton                  _skeleton        = new();
        private readonly NativeMultiMesh                 _multiMesh       = new();
        private readonly List<SkinnedMeshRenderer>       _unityRenderers  = new();
        private readonly NativePoseBounds                _humanBounds     = new();

        // dirty flags
        private bool _meshUpdated;
        private bool _materialsUpdated;
        private bool _skeletonChanged;
        private bool _humanChanged;
        private bool _blendShapesChanged;
        private bool _humanBoundsDirtied;

        private void Awake()
        {
            Renderers       = _unityRenderers.AsReadOnly();
            NativeRenderers = _renderers.AsReadOnly();

            _skeleton.SetRoot(skeletonRoot ? skeletonRoot : transform);
            _skeleton.SkeletonChanged += () => _skeletonChanged = true;
            _skeleton.HumanChanged    += () => _humanChanged = true;

            _humanBounds.CalculationDirtied += () => _humanBoundsDirtied = true;
        }

        private void OnDestroy()
        {
            UpdatedMesh          = null;
            UpdatedMaterials     = null;
            SkeletonChanged      = null;
            HumanSkeletonChanged = null;
            BlendShapesChanged   = null;
            HumanBoundsDirtied   = null;

            foreach (SkinnedNativeMeshRenderer renderer in _renderers)
            {
                Destroy(renderer.gameObject);
            }

            _renderers.Clear();
            _skeleton.Clear();
            _multiMesh.Dispose();
            _unityRenderers.Clear();
        }

        public void SetSkeletonRoot(Transform skeletonRoot)
        {
            _skeleton.SetRoot(skeletonRoot ? skeletonRoot : transform);
            this.skeletonRoot = skeletonRoot;
        }

        public void RebuildMesh(Entity entity, bool forced = true)
        {
            // update multi-mesh from the given entity
            _multiMesh.SetEntity(entity);

            if (verbose)
            {
                Debug.Log($"{RebuildDebugTag} Starting rebuild: {_multiMesh.Meshes.Count} meshes, forced={forced}");
            }

            BeginUpdates();
            {
                Bounds oldBounds = GetCombinedMeshBounds();

                // rebuild steps
                RebuildSkeleton();
                RebuildRenderers();
                RebuildRendererMeshes(forced);

                /**
                 * If the mesh bounds changed, invalidate the human bounds. This should be more than enough to detect if
                 * the human bounds should be invalidated, but there could be edge cases if the skeleton pose greatly
                 * differs from the mesh bindpose, or if there are non-human bones affecting the mesh bounds. This check
                 * may need to be revisited if those edge cases are found.
                 */
                if (oldBounds != GetCombinedMeshBounds())
                {
                    _humanBounds.InvalidateCalculation();
                }
            }
            EndUpdates();
        }

        public void Clear()
        {
            BeginUpdates();

            _meshUpdated = _renderers.Count > 0;

            foreach (SkinnedNativeMeshRenderer renderer in _renderers)
            {
                Destroy(renderer.gameObject);
            }

            _renderers.Clear();
            _unityRenderers.Clear();
            _skeleton.Clear(destroyImmediate: true);
            _humanBounds.InvalidateCalculation();

            EndUpdates();
        }

        private void RebuildRenderers()
        {
            // map the current rendrers to their IDs
            var renderersById = new Dictionary<string, SkinnedNativeMeshRenderer>();
            foreach (SkinnedNativeMeshRenderer renderer in _renderers)
            {
                renderersById[renderer.name] = renderer;
            }

            // do a first pass, assigning existing renderers that matches the multi-mesh and filling null for missing ones
            _renderers.Clear();
            _unityRenderers.Clear();
            foreach (NativeMultiMesh.Mesh mesh in _multiMesh.Meshes)
            {
                if (!renderersById.TryGetValue(mesh.Id, out SkinnedNativeMeshRenderer existingRenderer))
                {
                    _renderers.Add(null);
                    _unityRenderers.Add(null);
                    continue;
                }

                _renderers.Add(existingRenderer);
                _unityRenderers.Add(existingRenderer.Renderer);
                renderersById.Remove(mesh.Id);
            }

            // gather all remaining renderers that are not used anymore
            var unusedRenderers = new Stack<SkinnedNativeMeshRenderer>();
            foreach (SkinnedNativeMeshRenderer renderer in renderersById.Values)
            {
                unusedRenderers.Push(renderer);
            }

            // do a second-pass, initializing missing renderers by reusing or creating new ones
            for (int i = 0; i < _multiMesh.Meshes.Count; ++i)
            {
                // check if the renderer is already assigned
                SkinnedNativeMeshRenderer renderer = _renderers[i];
                if (renderer is not null)
                {
                    continue;
                }

                // no existing renderer was found for this ID, so create or reuse one
                renderer = CreateOrReuseRenderer(_multiMesh.Meshes[i].Id, unusedRenderers);

                // update the lists
                _renderers[i]      = renderer;
                _unityRenderers[i] = renderer.Renderer;

                // flag that the mesh changed since we are adding a new renderer (or reusing one for a new mesh)
                _meshUpdated = true;
            }

            // destroy all unused renderers
            foreach (SkinnedNativeMeshRenderer renderer in unusedRenderers)
            {
                Destroy(renderer.gameObject);
                _meshUpdated = true;
            }
        }

        private void RebuildSkeleton()
        {
            // if no shared skeleton, clear previous and return
            if (_multiMesh.Skeleton is null)
            {
                _skeleton.Clear(destroyImmediate: true);
                return;
            }

            // create/update the skeleton
            _skeleton.Set(_multiMesh.Skeleton);
        }

        private void RebuildRendererMeshes(bool forced)
        {
            // get the skeleton that will be used for all renderers (null if not defined in the multi-mesh)
            NativeSkeleton skeleton = _multiMesh.Skeleton is null ? null : _skeleton;

            for (int i = 0; i < _renderers.Count; ++i)
            {
                SkinnedNativeMeshRenderer renderer = _renderers[i];
                NativeMultiMesh.Mesh      mesh     = _multiMesh.Meshes[i];

                // rebuild everything if a forced update or the mesh is dirty
                bool isMeshDirty = mesh.Report.IsNull() || mesh.Report.IsDirty();
                if (forced || isMeshDirty)
                {
                    // initialize skeleton overrides. If for some reason there is no skeleton or joints, we fall back to the mesh's own
                    VectorSizeT    overrideJoints   = skeleton is null ? null : mesh.SharedJoints;
                    NativeSkeleton overrideSkeleton = overrideJoints is null ? null : skeleton;

                    if (verbose)
                    {
                        Debug.Log($"{RebuildDebugTag} Rebuilding mesh {(overrideSkeleton is null ? "(no skeleton override)" : "")}: '{renderer.name}'");
                    }

                    renderer.SetMesh(mesh.RuntimeMesh, overrideSkeleton, overrideJoints);
                    renderer.SetMaterials(mesh.RuntimeMesh);
                    renderer.SetMorphTargetWeights(mesh.RuntimeMesh);
                    continue;
                }

                // set materials if dirty
                if (mesh.Report.AreMaterialsDirty())
                {
                    if (verbose)
                    {
                        Debug.Log($"{RebuildDebugTag} Setting materials: '{renderer.name}'");
                    }

                    renderer.SetMaterials(mesh.RuntimeMesh);
                }

                // set morph-target weights if dirty
                if (mesh.Report.AreMorphTargetWeightsDirty())
                {
                    if (verbose)
                    {
                        Debug.Log($"{RebuildDebugTag} Setting morph-target weights: '{renderer.name}'");
                    }

                    renderer.SetMorphTargetWeights(mesh.RuntimeMesh);
                }
            }
        }

        private Bounds GetCombinedMeshBounds()
        {
            var bounds = new Bounds(Vector3.zero, float.MinValue * Vector3.one);
            bool foundBounds = false;
            foreach (SkinnedNativeMeshRenderer renderer in _renderers)
            {
                if (renderer.Renderer.sharedMesh)
                {
                    bounds.Encapsulate(renderer.Renderer.sharedMesh.bounds);
                    foundBounds = true;
                }
            }

            if (foundBounds)
            {
                return bounds;
            }

            return default;
        }

        private SkinnedNativeMeshRenderer CreateOrReuseRenderer(string id, Stack<SkinnedNativeMeshRenderer> unusedRenderers)
        {
            if (unusedRenderers.TryPop(out SkinnedNativeMeshRenderer renderer))
            {
                if (verbose)
                {
                    Debug.Log($"{RebuildDebugTag} Reusing renderer '{renderer.name}' for '{id}'");
                }

                renderer.name = id;

                return renderer;
            }

            if (verbose)
            {
                Debug.Log($"{RebuildDebugTag} Creating new renderer for '{id}'");
            }

            // create and initialize the renderer
            renderer = Instantiate(rendererPrefab, transform);
            renderer.name = id;
            renderer.SetRenderer(renderer.Renderer, skeletonRoot); // set our skeleton root

            // subscribe to events
            renderer.UpdatedMesh        += () => _meshUpdated = true;
            renderer.UpdatedMaterials   += () => _materialsUpdated = true;
            renderer.BlendShapesChanged += () => _blendShapesChanged = true;

            return renderer;
        }

        private void BeginUpdates()
        {
            _meshUpdated        = false;
            _materialsUpdated   = false;
            _skeletonChanged    = false;
            _humanChanged       = false;
            _blendShapesChanged = false;
            _humanBoundsDirtied = false;
        }

        private void EndUpdates()
        {
            if (verbose)
            {
                Debug.Log($"{RebuildDebugTag} Update end report:\nmeshUpdated: {_meshUpdated}\nmaterialsUpdated: {_materialsUpdated}\nskeletonChanged: {_skeletonChanged}\nhumanChanged: {_humanChanged}\nblendShapesChanged: {_blendShapesChanged}\nhumanBoundsDirtied: {_humanBoundsDirtied}");
            }

            if (_meshUpdated)
            {
                UpdatedMesh?.Invoke();
            }

            if (_materialsUpdated)
            {
                UpdatedMaterials?.Invoke();
            }

            if (_skeletonChanged)
            {
                SkeletonChanged?.Invoke();
            }

            if (_humanChanged)
            {
                _humanBounds.InvalidateCalculation();
                HumanSkeletonChanged?.Invoke();
            }

            if (_blendShapesChanged)
            {
                BlendShapesChanged?.Invoke();
            }

            if (_humanBoundsDirtied)
            {
                HumanBoundsDirtied?.Invoke();
            }

            _meshUpdated        = false;
            _materialsUpdated   = false;
            _skeletonChanged    = false;
            _humanChanged       = false;
            _blendShapesChanged = false;
            _humanBoundsDirtied = false;
        }

#region NativeSkeleton Wrappers
        public Transform                SkeletonRoot     => _skeleton.Root;
        public IReadOnlyList<Transform> RootBones        => _skeleton.RootBones;
        public IReadOnlyList<Transform> Bones            => _skeleton.Bones;
        public HumanDescription?        HumanDescription => _skeleton.HumanDescription;

        public void SetHumanDescription(HumanDescription humanDescription)
        {
            BeginUpdates();
            _skeleton.SetHumanDescription(humanDescription);
            EndUpdates();
        }

        public void ClearHumanDescription()
        {
            BeginUpdates();
            _skeleton.ClearHumanDescription();
            EndUpdates();
        }

        public void ApplyDefaultSkeletonPose()
            => _skeleton.ApplyDefaultPose();

        public void ApplyHumanSkeletonPose(Vector3? hipsOffset = null, bool applyDefaultIfNotHuman = false)
            => _skeleton.ApplyHumanPose(hipsOffset, applyDefaultIfNotHuman);

        public void RestoreSkeletonPose()
            => _skeleton.RestorePose();

        public Avatar BuildHumanAvatar(GameObject go, Vector3? hipsOffset = null)
        {
            if (verbose)
            {
                Debug.Log($"{RebuildDebugTag} Building human avatar...");
            }

            return _skeleton.BuildHumanAvatar(go, hipsOffset);
        }
#endregion

#region NativePoseBounds Wrappers
        public bool     AreHumanBoundsValid => _humanBounds.IsCalculationValid;
        public Bounds?  HumanBounds         => _humanBounds.Bounds;
        public Vector3? GroundingHipsOffset => _humanBounds.GroundingHipsOffset;

        public void PerformHumanBoundsCalculation()
        {
            if (verbose)
            {
                Debug.Log($"{RebuildDebugTag} Performing human bounds calculation...");
            }

            // make sure the setup is updated with the latest renderers and perform calculation
            _humanBounds.Setup(_unityRenderers, _skeleton, _skeleton.HipsBone);
            _humanBounds.PerformCalculation(applyHumanPose: true);
        }

        public void InvalidateHumanBounds()
            => _humanBounds.InvalidateCalculation();
#endregion
    }
}
