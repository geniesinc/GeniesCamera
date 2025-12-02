using System;
using System.Collections.Generic;
using GnWrappers;
using UnityEngine;
using UnityEngine.Rendering;

namespace Genies.Naf
{
    /**
     * <see cref="NativeMeshRenderer"/> implementation that uses a <see cref="SkinnedMeshRenderer"/>.
     */
    public sealed class SkinnedNativeMeshRenderer : NativeMeshRenderer
    {
        public SkinnedMeshRenderer Renderer => renderer;

        public event Action SkeletonChanged;
        public event Action HumanSkeletonChanged;
        public event Action BlendShapesChanged;
        public event Action HumanBoundsDirtied;

        // inspector
        [SerializeField] private new SkinnedMeshRenderer renderer;
        [SerializeField] private Transform skeletonRoot;
        [Tooltip("If enabled, the UnityEngine.Mesh instance will be reused when setting a new RuntimeMesh.")]
        public bool reuseMeshAsset = false;
        [Tooltip("Recalculates normals and/or tangents when missing from the native mesh.")]
        public bool recalculateMissingAttributes = true;
        [Tooltip("Scale factor for the auto-calculated renderer local bounds. Increase this value if you are experiencing unwanted culling since the calculated bounds will never be as precise as enabling update when offscreen on the renderer.")]
        public float rendererBoundsScale = 1.0f;
        public bool setBlendShapeWeightsOnSetMesh;

        private readonly NativeSkeleton   _skeleton    = new();
        private readonly List<BlendShape> _blendShapes = new();
        private readonly NativePoseBounds _humanBounds = new();

        private Mesh           _mesh;
        private NativeSkeleton _currentSkeleton;

        // dirty flags
        private bool _updatedMesh;
        private bool _skeletonChanged;
        private bool _humanChanged;
        private bool _blendShapesChanged;
        private bool _humanBoundsDirtied;

        private void Awake()
        {
            base.SetRenderer(renderer);

            _currentSkeleton = _skeleton;
            _skeleton.SetRoot(skeletonRoot ? skeletonRoot : transform);

            // subscribe to events
            _skeleton.SkeletonChanged       += () => _skeletonChanged = true;
            _skeleton.HumanChanged          += () => _humanChanged = true;
            _humanBounds.CalculationDirtied += () => _humanBoundsDirtied = true;
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            SkeletonChanged      = null;
            HumanSkeletonChanged = null;
            BlendShapesChanged   = null;
            HumanBoundsDirtied   = null;

            ClearMesh();

            if (_mesh)
            {
                Destroy(_mesh);
            }
        }

        public void SetRenderer(SkinnedMeshRenderer renderer, Transform skeletonRoot)
        {
            if (!renderer)
            {
                throw new NullReferenceException("Renderer is null");
            }

            // make sure to have the internal skeleton updated (the given root must not affect any overriden skeleton)
            _skeleton.SetRoot(skeletonRoot ? skeletonRoot : transform);

            if (renderer == this.renderer)
            {
                return;
            }

            // if there is a previous renderer, copy its state, if not, clear the new renderer until a new mesh is set
            if (this.renderer)
            {
                renderer.sharedMesh  = this.renderer.sharedMesh;
                renderer.bones       = this.renderer.bones;
                renderer.rootBone    = this.renderer.rootBone;
                renderer.localBounds = this.renderer.localBounds;
            }
            else
            {
                renderer.sharedMesh  = null;
                renderer.bones       = null;
                renderer.rootBone    = null;
                renderer.localBounds = default;
            }

            this.renderer = renderer;
            base.SetRenderer(renderer);
        }

        public override void SetMesh(RuntimeMesh runtimeMesh)
        {
            SetMesh(runtimeMesh, null, null);
        }

        /**
         * Sets the mesh to the given <see cref="RuntimeMesh"/> but uses the given override skeleton instead of the
         * internal one. Skeleton events will not be fired in this case.
         */
        public void SetMesh(RuntimeMesh runtimeMesh, NativeSkeleton overrideSkeleton, VectorSizeT overrideJoints)
        {
            // validate skeleton override arguments
            if (overrideSkeleton is null != overrideJoints is null)
            {
                throw new ArgumentException("Either both or none of overrideSkeleton and overrideJoints must be provided");
            }

            BeginUpdates();

            // flag the mesh as updated and clear the mesh
            _updatedMesh = true;
            ClearMeshOnly();

            // if no runtime mesh is provided, clear the internal skeleton and exit
            if (runtimeMesh.IsNull())
            {
                _skeleton.Clear();
                EndUpdates();
                return;
            }

            Transform[] bones;

            // if no override skeleton is provided, use the internal skeleton
            if (overrideSkeleton is null)
            {
                _currentSkeleton = _skeleton;
                _skeleton.Set(runtimeMesh);
                renderer.bones = bones = _skeleton.GetJoints(runtimeMesh);
            }
            // else make sure the internal skeleton is cleared and use the override skeleton
            else
            {
                _skeleton.Clear();
                _currentSkeleton = overrideSkeleton;
                renderer.bones = bones = _currentSkeleton.GetJoints(overrideJoints);
            }

            // create/update the internal Unity mesh and set it to the renderer. It is important to do this after renderer.bones were set, otherwise you get complaints from the renderer
            if (_mesh)
            {
                runtimeMesh.ApplyToMesh(_mesh, ignoreSkinning: false, recalculateMissingAttributes, (MeshUpdateFlags)~0);
                string id = runtimeMesh.Id();
                _mesh.name = string.IsNullOrEmpty(id) ? "[Unnamed RuntimeMesh]" : id;
            }
            else
            {
                _mesh = runtimeMesh.CreateMesh(ignoreSkinning: false, recalculateMissingAttributes);
            }

            renderer.sharedMesh = _mesh;
            UpdateRendererBounds(bones);
            UpdateHumanBoundsSetup();
            SetBlendShapes(runtimeMesh);

            EndUpdates();
        }

        public void SetMorphTargetWeights(RuntimeMesh runtimeMesh)
        {
            uint morphTargetCount = runtimeMesh.TargetCount();
            if (!_mesh || _mesh.blendShapeCount != (int)morphTargetCount)
            {
                Debug.LogError("The blend shape count for the given runtime mesh doesn't match the current mesh");
                return;
            }

            if (renderer.sharedMesh != _mesh)
            {
                Debug.LogError("The current mesh is not set to the renderer");
                return;
            }

            for (uint i = 0; i < morphTargetCount; ++i)
            {
                using MorphTarget target = runtimeMesh.GetTarget(i);
                renderer.SetBlendShapeWeight((int)i, 100.0f * target.Weight());
            }
        }

        public override void ClearMesh()
        {
            BeginUpdates();

            // clear mesh and skeleton
            ClearMeshOnly();
            _skeleton.Clear();

            EndUpdates();
        }

        private void UpdateRendererBounds(Transform[] bones)
        {
            // no need to update bounds if the renderer is set to always update
            if (renderer.updateWhenOffscreen)
            {
                return;
            }

            // try to find a root bone
            var root = new NativeRootBone(_currentSkeleton, bones, _mesh.bindposes);
            if (!root.Bone)
            {
                // log an error if there were bones assigned but no valid root bone found
                if (bones.Length > 0)
                {
                    Debug.LogError($"Couldn't find a valid root bone for the native mesh renderer: {name}. ");
                }

                renderer.rootBone    = null;
                renderer.localBounds = _mesh.bounds;
                return;
            }

            // transform the mesh bounds to the root's local space
            Bounds bounds = _mesh.bounds;
            bounds.center = root.Bindpose.MultiplyPoint3x4(bounds.center);
            bounds.extents = root.Bindpose.MultiplyVector(bounds.extents * rendererBoundsScale);

            // update the renderer
            renderer.rootBone    = root.Bone;
            renderer.localBounds = bounds;
        }

        private void UpdateHumanBoundsSetup()
        {
            if (_humanBounds.Skeleton != _currentSkeleton || renderer.rootBone != _humanBounds.BoundsRoot)
            {
                _humanBounds.Setup(new[] { renderer }, _currentSkeleton, renderer.rootBone);
            }
        }

        private void SetBlendShapes(RuntimeMesh runtimeMesh)
        {
            int targetCount = (int)runtimeMesh.TargetCount();
            if (_mesh.blendShapeCount != targetCount)
            {
                throw new Exception("The blend shape count for the given runtime mesh doesn't match the current mesh");
            }

            for (int i = 0; i < targetCount; ++i)
            {
                using MorphTarget target = runtimeMesh.GetTarget((uint)i);
                var blendShape = new BlendShape { Name = target.Name(), Weight = target.Weight() };

                if (i < _blendShapes.Count)
                {
                    if (_blendShapes[i].Name != blendShape.Name)
                    {
                        _blendShapesChanged = true;
                    }

                    _blendShapes[i] = blendShape;
                }
                else
                {
                    _blendShapes.Add(blendShape);
                    _blendShapesChanged = true;
                }

                if (setBlendShapeWeightsOnSetMesh)
                {
                    renderer.SetBlendShapeWeight(i, 100.0f * blendShape.Weight);
                }
            }

            if (_blendShapes.Count <= targetCount)
            {
                return;
            }

            // remove any blend shapes that are not in the new mesh
            _blendShapes.RemoveRange(targetCount, _blendShapes.Count - targetCount);
            _blendShapesChanged = true;
        }

        private void ClearMeshOnly()
        {
            bool wasMeshClear = !_mesh || _mesh.vertexCount == 0;

            if (_mesh)
            {
                if (reuseMeshAsset)
                {
                    _mesh.Clear();
                }
                else
                {
                    Destroy(_mesh);
                    _mesh = null;
                }
            }

            // reset the renderer
            if (renderer)
            {
                renderer.sharedMesh  = null;
                renderer.rootBone    = null;
                renderer.bones       = null;
                renderer.localBounds = default;
            }

            if (!wasMeshClear)
            {
                _updatedMesh = true;
            }
        }

        private void BeginUpdates()
        {
            _updatedMesh        = false;
            _skeletonChanged    = false;
            _humanChanged       = false;
            _blendShapesChanged = false;
            _humanBoundsDirtied = false;
        }

        private void EndUpdates()
        {
            if (_updatedMesh)
            {
                _humanBounds.InvalidateCalculation();
                TriggerUpdatedMesh();
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

            _updatedMesh        = false;
            _skeletonChanged    = false;
            _humanChanged       = false;
            _humanBoundsDirtied = false;
            _blendShapesChanged = false;
        }

        private struct BlendShape
        {
            public string Name;
            public float  Weight;
        }

#region NativeSkeleton Wrappers
        public Transform                SkeletonRoot     => _currentSkeleton.Root;
        public IReadOnlyList<Transform> RootBones        => _currentSkeleton.RootBones;
        public IReadOnlyList<Transform> Bones            => _currentSkeleton.Bones;
        public HumanDescription?        HumanDescription => _currentSkeleton.HumanDescription;

        public void SetHumanDescription(HumanDescription humanDescription)
        {
            BeginUpdates();
            _currentSkeleton.SetHumanDescription(humanDescription);
            EndUpdates();
        }

        public void ClearHumanDescription()
        {
            BeginUpdates();
            _currentSkeleton.ClearHumanDescription();
            EndUpdates();
        }

        public void ApplyDefaultSkeletonPose()
            => _currentSkeleton.ApplyDefaultPose();

        public void ApplyHumanSkeletonPose(Vector3? hipsOffset = null, bool applyDefaultIfNotHuman = false)
            => _currentSkeleton.ApplyHumanPose(hipsOffset, applyDefaultIfNotHuman);

        public void RestoreSkeletonPose()
            => _currentSkeleton.RestorePose();

        public Avatar BuildHumanAvatar(GameObject go, Vector3? hipsOffset = null)
            => _currentSkeleton.BuildHumanAvatar(go, hipsOffset);
#endregion

#region NativePoseBounds Wrappers
        public bool     AreHumanBoundsValid => _humanBounds.IsCalculationValid;
        public Bounds?  HumanBounds         => _humanBounds.Bounds;
        public Vector3? GroundingHipsOffset => _humanBounds.GroundingHipsOffset;

        public void PerformHumanBoundsCalculation()
            => _humanBounds.PerformCalculation(applyHumanPose: true);

        public void InvalidateHumanBounds()
            => _humanBounds.InvalidateCalculation();
#endregion
    }
}
