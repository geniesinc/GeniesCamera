using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using GnWrappers;
using UnityEngine;

using Object = UnityEngine.Object;

namespace Genies.Naf
{
    /**
     * Loads and manages a native skeleton in the Unity scene. It has a complex implementation since its very smart
     * about skeleton updates. It is much faster than just clearing and redoing the entire skeleton every time you set
     * a new one. It also helps users to rebuild the human avatar only when strictly necessary.
     */
    public sealed class NativeSkeleton
    {
        /**
         * Null or a manually set root for the generated skeleton hierarchy. It won't be part of the <see cref="Bones"/>
         * or <see cref="RootBones"/> lists, but it will be the parent of all root bones.
         */
        public Transform Root => _root.Transform;

        /**
         * The root bones of the skeleton, which are the bones without parents in the skeleton hierarchy.
         */
        public IReadOnlyList<Transform> RootBones { get; }

        /**
         * The bones of the skeleton, which includes all bones in the hierarchy. It matches one to one the bones in the
         * skeleton, so the index of each bone in this list corresponds to the index of the bone in the skeleton.
         */
        public IReadOnlyList<Transform> Bones { get; }

        /**
         * The current human description.
         */
        public HumanDescription? HumanDescription => _humanDescription;

        /**
         * The transform of the standard Unity "Hips" bone, if any.
         */
        public Transform HipsBone => _hipsBone;

        /**
         * The index of the standard Unity "Hips" bone within the current human description skeleton array. Returns -1
         * if no human description is set or not hips were found in the current one.
         */
        public int HipsBoneIndex => _hipsBoneIndex;

        /**
         * Triggered when any bones are added, removed, or they parent-child relationship changes in the skeleton. It is
         * not triggered when the bone transform changes (whether it comes from the native bone or a set human description).
         */
        public event Action SkeletonChanged;

        /**
         * Triggered when the <see cref="HumanDescription"/> is set/cleared, or if there are skeleton changes for any
         * bones mapped within the current human description. If the root bone is mapped within the human description,
         * changes to it will also trigger this event. Also, any changes to the hips position will trigger this event.
         */
        public event Action HumanChanged;

        // bones state
        private NativeBone                              _root;
        private readonly Dictionary<string, NativeBone> _nativeBonesByName;
        private readonly List<NativeBone>               _nativeBones;
        private readonly List<Transform>                _rootBones;
        private readonly List<Transform>                _bones;

        // human description state
        private HumanDescription?                _humanDescription;
        private readonly HashSet<string>         _humanBoneNames;
        private readonly Dictionary<string, int> _skeletonBoneIndicesByName;
        public Transform                         _hipsBone;
        private int                              _hipsBoneIndex;

        // helpers
        private readonly HashSet<string> _bonesToRemove;

        public NativeSkeleton()
        {
            _root = new NativeBone
            {
                Index               = -1,
                ParentIndex         = -1,
                IsHuman             = false,
                SkeletonBone        = null,
                Transform           = null,
                DefaultPose         = Matrix4x4.identity,
                DefaultLocalPose    = Matrix4x4.identity,
                Position            = Vector3.zero,
                Rotation            = Quaternion.identity,
                Scale               = Vector3.one,
                IsNew               = false,
            };

            _nativeBonesByName = new Dictionary<string, NativeBone>();
            _nativeBones       = new List<NativeBone>();
            _rootBones         = new List<Transform>();
            _bones             = new List<Transform>();
            RootBones          = _rootBones.AsReadOnly();
            Bones              = _bones.AsReadOnly();

            _humanDescription          = null;
            _humanBoneNames            = new HashSet<string>();
            _skeletonBoneIndicesByName = new Dictionary<string, int>();
            _hipsBoneIndex             = -1;

            _bonesToRemove = new HashSet<string>();
        }

        public void Set(RuntimeMesh runtimeMesh)
        {
            using Skeleton skeleton = runtimeMesh.Skeleton();
            Set(skeleton);
        }

        public void Set(Skeleton skeleton)
        {
            ulong skeletonSize = skeleton.Size();
            var   nativeBones  = new Bone[skeletonSize];

            try
            {
                for (uint i = 0; i < skeletonSize; i++)
                {
                    nativeBones[i] = skeleton.Bone(i);
                }

                Set(nativeBones);
            }
            finally
            {
                foreach (Bone bone in nativeBones)
                {
                    bone?.Dispose();
                }
            }
        }

        public void Set(IReadOnlyList<Bone> bones)
        {
            // initialize flags to track what changed
            bool skeletonChanged = false;
            bool humanChanged    = false;

            // sync the native bones collections (the list and the map) to the given bones
            SyncNativeBones(bones, ref skeletonChanged, ref humanChanged);

            // clear transform collections
            _rootBones.Clear();
            _bones.Clear();

            // sync each native bone individually. This will update the _rootBones and _bones collections
            for (int i = 0; i < bones.Count; ++i)
            {
                SyncNativeBone(bones[i], _nativeBones[i], ref skeletonChanged, ref humanChanged);
            }

            UpdateHipsBone();
            CalculateDefaultPoses();

            // trigger changed events
            if (skeletonChanged)
            {
                SkeletonChanged?.Invoke();
            }

            if (humanChanged)
            {
                HumanChanged?.Invoke();
            }
        }

        /**
         * Set the root transform for the skeleton. It can be null, in which case the root bones will have no parent.
         * Please note that since the root transform is always set by external actors, it is not a bone that is
         * "managed" by this class, so it won't ever be destroyed.
         */
        public void SetRoot(Transform root)
        {
            if (root == _root.Transform)
            {
                return;
            }

            // move the bones to the new root
            foreach (Transform rootBone in _rootBones)
            {
                rootBone.SetParent(root, worldPositionStays: false);
            }

            _root.Transform = root;
            UpdateHumanBone(_root);

            if (root)
            {
                _root.Position = root.localPosition;
                _root.Rotation = root.localRotation;
                _root.Scale    = root.localScale;

            }
            else
            {
                _root.Position = Vector3.zero;
                _root.Rotation = Quaternion.identity;
                _root.Scale    = Vector3.one;
            }

            _root.DefaultPose      = Matrix4x4.TRS(_root.Position, _root.Rotation, _root.Scale);
            _root.DefaultLocalPose = _root.DefaultPose.Value;

            // trigger changed events
            SkeletonChanged?.Invoke();
            if (_humanDescription.HasValue)
            {
                HumanChanged?.Invoke();
            }
        }

        public void SetHumanDescription(HumanDescription humanDescription)
        {
            // set the new human description and clear the human bones set
            _humanDescription = humanDescription;
            _humanBoneNames.Clear();
            _skeletonBoneIndicesByName.Clear();

            // find the bone name for the Hips bone
            string hipsBoneName = null;
            foreach (HumanBone bone in humanDescription.human)
            {
                if (bone.humanName != "Hips")
                {
                    continue;
                }

                hipsBoneName = bone.boneName;
                break;
            }

            // gather the name of all the human bones
            foreach (HumanBone bone in humanDescription.human)
            {
                _humanBoneNames.Add(bone.boneName);
            }

            // reset hips bone index. Iterate over all skeleton bones, registering their indices by name and, if the hips bone is found, record its index
            _hipsBoneIndex = -1;
            for (int i = 0; i < humanDescription.skeleton.Length; ++i)
            {
                SkeletonBone bone = humanDescription.skeleton[i];
                if (bone.name == hipsBoneName)
                {
                    _hipsBoneIndex = i;
                }

                _skeletonBoneIndicesByName.Add(bone.name, i);
            }

            // just in case the hips bone name was null and it mathed with a random null name skeleton bone, make sure the hips bone index is -1
            if (hipsBoneName == null)
            {
                _hipsBoneIndex = -1;
            }

            UpdateHipsBone();

            // update human bones on all bones, including the root
            UpdateHumanBone(_root);
            foreach (NativeBone bone in _nativeBones)
            {
                UpdateHumanBone(bone);
            }

            HumanChanged?.Invoke();
        }

        public void ClearHumanDescription()
        {
            if (!_humanDescription.HasValue)
            {
                return;
            }

            _humanDescription = null;
            _humanBoneNames.Clear();
            _skeletonBoneIndicesByName.Clear();

            bool anyWasHuman = _root.IsHuman;

            _root.IsHuman      = false;
            _root.SkeletonBone = null;
            foreach (NativeBone nativeBone in _nativeBones)
            {
                if (nativeBone.IsHuman)
                {
                    anyWasHuman = true;
                }

                nativeBone.IsHuman      = false;
                nativeBone.SkeletonBone = null;
            }

            _hipsBoneIndex = -1;
            UpdateHipsBone();

            if (anyWasHuman)
            {
                HumanChanged?.Invoke();
            }
        }

        /**
         * Clears the skeleton except for the currently set root transform and human description. You can always set a
         * null root transform by calling SetRoot(null). Please note that since the root transform is always set by
         * external actors, it is not a bone that is "managed" by this class, so it won't ever be destroyed.
         */
        public void Clear(bool destroyImmediate = false)
        {
            if (_nativeBones.Count == 0)
            {
                return;
            }

            bool containedHumanBones = false;
            foreach (NativeBone bone in _nativeBones)
            {
                if (!bone.IsHuman)
                {
                    continue;
                }

                containedHumanBones = true;
                break;
            }

            foreach (Transform root in _rootBones)
            {
                if (!root)
                {
                    continue;
                }

                if (destroyImmediate)
                {
                    Object.DestroyImmediate(root.gameObject);
                }
                else
                {
                    Object.Destroy(root.gameObject);
                }
            }

            _nativeBonesByName.Clear();
            _nativeBones.Clear();
            _rootBones.Clear();
            _bones.Clear();

            SkeletonChanged?.Invoke();
            if (containedHumanBones)
            {
                HumanChanged?.Invoke();
            }
        }

        public Transform[] GetJoints(RuntimeMesh runtimeMesh)
        {
            using VectorSizeT nativeJoints = runtimeMesh.Joints();
            return GetJoints(nativeJoints);
        }

        public Transform[] GetJoints(VectorSizeT nativeJoints)
        {
            int jointsSize = nativeJoints.Count;
            var joints = new Transform[jointsSize];

            if (jointsSize > _bones.Count)
            {
                Debug.LogError($"Joints size ({jointsSize}) exceeds the number of bones ({_bones.Count}).");
                return Array.Empty<Transform>();
            }

            for (int i = 0; i < jointsSize; i++)
            {
                int boneIndex = (int)nativeJoints[i];
                if (boneIndex < 0 || boneIndex >= _bones.Count)
                {
                    Debug.LogError($"Joint index {boneIndex} is out of bounds for the bones list of size {_bones.Count}.");
                    return Array.Empty<Transform>();
                }

                joints[i] = _bones[boneIndex];
            }

            return joints;
        }

        /**
         * Applies the skeleton default pose and saves the current pose so you can restore it later with RestorePose().
         */
        public void ApplyDefaultPose()
        {
            if (_root.Transform)
            {
                _root.ApplyDefaultPose();
            }

            foreach (NativeBone bone in _nativeBones)
            {
                bone.ApplyDefaultPose();
            }
        }

        /**
         * Applies the human description skeleton pose (only if human description is set) and saves the current pose so
         * you can restore it later with RestorePose(). By default, any bones not registered in the human description
         * are ignored, but you can enable applyDefaultIfNotInHumanDescription if you want them to be set to their
         * default pose.
         */
        public void ApplyHumanPose(Vector3? hipsOffset = null, bool applyDefaultIfNotInHumanDescription = false)
        {
            if (_root.Transform)
            {
                _root.ApplyHumanPose(applyDefaultIfNotInHumanDescription);
            }

            foreach (NativeBone bone in _nativeBones)
            {
                bone.ApplyHumanPose(applyDefaultIfNotInHumanDescription);

                if (hipsOffset.HasValue && bone.Transform == HipsBone)
                {
                    bone.Transform.localPosition += hipsOffset.Value;
                }
            }
        }

        /**
         * Restores the last pose set before the last ApplyDefaultPose() call.
         */
        public void RestorePose()
        {
            if (_root.Transform)
            {
                _root.RestorePose();
            }

            foreach (NativeBone bone in _nativeBones)
            {
                bone.RestorePose();
            }
        }

        /**
         * Builds an <see cref="Avatar"/> asset out of the current human description, optionally allowing to offset the
         * Hips bone (for grounding). If no human description is set, returns null.
         */
        public Avatar BuildHumanAvatar(GameObject go, Vector3? hipsOffset = null)
        {
            if (!_humanDescription.HasValue || !_hipsBone)
            {
                return null;
            }

            // get the human description and remove any non-existing bones from its skeleton
            HumanDescription humanDescription = _humanDescription.Value;
            var skeleton = new List<SkeletonBone>(humanDescription.skeleton.Length);
            for (int i = 0; i < humanDescription.skeleton.Length; ++i)
            {
                SkeletonBone bone = humanDescription.skeleton[i];
                if (!ContainsBone(bone.name))
                {
                    continue;
                }

                // if we find the hips bone and a hips offset was provided, apply it
                if (hipsOffset.HasValue && i == _hipsBoneIndex)
                {
                    bone.position += hipsOffset.Value;
                }

                skeleton.Add(bone);
            }

            humanDescription.skeleton = skeleton.ToArray();

            return AvatarBuilder.BuildHumanAvatar(go, humanDescription);
        }

        public bool ContainsBone(string name)
        {
            return _root.Transform && _root.Transform.name == name || _nativeBonesByName.ContainsKey(name);
        }

        public bool TryGetIndex(string boneName, out int index)
        {
            if (TryGetNativeBone(boneName, out NativeBone nativeBone))
            {
                index = nativeBone.Index;
                return true;
            }

            index = -1;
            return false;
        }

        public bool TryGetIsHuman(string boneName, out bool isHuman)
        {
            if (TryGetNativeBone(boneName, out NativeBone nativeBone))
            {
                isHuman = nativeBone.IsHuman;
                return true;
            }

            isHuman = false;
            return false;
        }

        public bool TryGetTransform(string boneName, out Transform transform)
        {
            if (TryGetNativeBone(boneName, out NativeBone nativeBone))
            {
                transform = nativeBone.Transform;
                return true;
            }

            transform = null;
            return false;
        }

        public bool TryGetDefaultLocalPose(string boneName, out Matrix4x4 defaultLocalPose)
        {
            if (TryGetNativeBone(boneName, out NativeBone nativeBone))
            {
                defaultLocalPose = nativeBone.DefaultLocalPose;
                return true;
            }

            defaultLocalPose = Matrix4x4.identity;
            return false;
        }

        public bool TryGetDefaultPose(string boneName, out Matrix4x4 defaultPose)
        {
            if (TryGetNativeBone(boneName, out NativeBone nativeBone) && nativeBone.DefaultPose.HasValue)
            {
                defaultPose = nativeBone.DefaultPose.Value;
                return true;
            }

            defaultPose = Matrix4x4.identity;
            return false;
        }

        public bool TryGetSkeletonBone(string boneName, out SkeletonBone skeletonBone)
        {
            if (TryGetNativeBone(boneName, out NativeBone nativeBone) && nativeBone.SkeletonBone.HasValue)
            {
                skeletonBone = nativeBone.SkeletonBone.Value;
                return true;
            }

            skeletonBone = default;
            return false;
        }

        public bool TryGetBone(string boneName, out int index, out bool isHuman, out Transform transform)
        {
            if (TryGetNativeBone(boneName, out NativeBone nativeBone))
            {
                index     = nativeBone.Index;
                isHuman   = nativeBone.IsHuman;
                transform = nativeBone.Transform;
                return true;
            }

            index     = -1;
            isHuman   = false;
            transform = null;
            return false;
        }

        // special TryGet method for native bones that includes the root
        private bool TryGetNativeBone(string boneName, out NativeBone bone)
        {
            if (_root.Transform && _root.Transform.name == boneName)
            {
                bone = _root;
                return true;
            }

            return _nativeBonesByName.TryGetValue(boneName, out bone);
        }

        private void UpdateHumanBone(NativeBone bone)
        {
            if (!bone.Transform || !_humanDescription.HasValue)
            {
                bone.IsHuman      = false;
                bone.SkeletonBone = null;
                return;
            }

            string boneName = bone.Transform.name;
            bone.IsHuman = _humanBoneNames.Contains(boneName);

            if (_skeletonBoneIndicesByName.TryGetValue(boneName, out int humanBoneIndex))
            {
                bone.SkeletonBone = _humanDescription.Value.skeleton[humanBoneIndex];
            }
            else
            {
                bone.SkeletonBone = null;
            }
        }

        private void UpdateHipsBone()
        {
            if (_humanDescription.HasValue && _hipsBoneIndex >= 0 && _hipsBoneIndex < _humanDescription.Value.skeleton.Length)
            {
                string nativeHipsName = _humanDescription.Value.skeleton[_hipsBoneIndex].name;
                if (_nativeBonesByName.TryGetValue(nativeHipsName, out NativeBone bone))
                {
                    _hipsBone = bone.Transform;
                }

                return;
            }

            _hipsBone = null;
        }

        private void SyncNativeBones(IReadOnlyList<Bone> bones, ref bool skeletonChanged, ref bool humanChanged)
        {
            _bonesToRemove.Clear();
            _bonesToRemove.UnionWith(_nativeBonesByName.Keys);
            _nativeBones.Clear();

            /**
             * - Update the _nativeBones list to match the bone wrappers.
             * - Create and add new bones to the _nativeBonesByName.
             * - Remove current bones from _bonesToRemove.
             */
            for (int i = 0; i < bones.Count; ++i)
            {
                // get the native bone wrapper and its name. Also remove it from bones to remove
                Bone bone = bones[i];
                string name = bone.Name();
                _bonesToRemove.Remove(name);

                // reuse bones that already exist (and its transform has not been destroyed by external actors)
                if (_nativeBonesByName.TryGetValue(name, out NativeBone nativeBone) && nativeBone.Transform)
                {
                    // ensure IsNew is false (it should be already but just in case)
                    nativeBone.IsNew = false;
                }
                else
                {
                    // create a new native bone, add it to the dictionary and the list
                    nativeBone = new NativeBone
                    {
                        Transform = new GameObject(name).transform,
                        IsNew     = true,
                    };

                    _nativeBonesByName[name] = nativeBone;
                }

                // update index and default pose
                nativeBone.Index    = i;
                nativeBone.Position = Marshal.PtrToStructure<Vector3>(bone.Position());
                nativeBone.Rotation = Marshal.PtrToStructure<Quaternion>(bone.Rotation());
                nativeBone.Scale    = Marshal.PtrToStructure<Vector3>(bone.Scale());

                // check if the default pose changed (only for reused bones)
                var defaultLocalPose = Matrix4x4.TRS(nativeBone.Position, nativeBone.Rotation, nativeBone.Scale);
                nativeBone.DefaultPoseChanged = !nativeBone.IsNew && nativeBone.DefaultLocalPose != defaultLocalPose;

                // update pose matrices
                nativeBone.DefaultPose      = null; // will be calculated later as soon as parent indices are updated
                nativeBone.DefaultLocalPose = defaultLocalPose;

                // add to native bones list
                _nativeBones.Add(nativeBone);

                if (!nativeBone.IsNew)
                {
                    continue;
                }

                // if the bone is new, update its human bone and flags
                UpdateHumanBone(nativeBone);
                skeletonChanged = true;
                humanChanged |= nativeBone.IsHuman;
            }

            // remove bones that no longer exist
            foreach (string name in _bonesToRemove)
            {
                // remove from map and destroy the GameObject
                _nativeBonesByName.Remove(name, out NativeBone nativeBone);
                if (nativeBone.Transform)
                {
                    // before destroying the bone, we need to recreate all its children bones that still exist (if any)
                    RecreateChildrenBonesBeforeDestroy(nativeBone.Transform);
                    // it's very important to use immediate destruction here, otherwise we can get weird issues when rebuilding the human Avatar
                    Object.DestroyImmediate(nativeBone.Transform.gameObject);
                }

                // update flags
                skeletonChanged = true;
                humanChanged |= nativeBone.IsHuman;
            }

            _bonesToRemove.Clear();
            return;

            void RecreateChildrenBonesBeforeDestroy(Transform transform)
            {
                foreach (Transform child in transform)
                {
                    RecreateChildrenBonesBeforeDestroy(child);
                }

                // since the transform is going to be destroyed, we need to recreate it under a new GameObject
                if (_nativeBonesByName.TryGetValue(transform.name, out NativeBone nativeBone))
                {
                    nativeBone.Transform = new GameObject(transform.name).transform;
                    nativeBone.IsNew     = true;
                }
            }
        }

        private void CalculateDefaultPoses()
        {
            foreach (NativeBone bone in _nativeBones)
            {
                CalculateDefaultPose(bone);
            }
        }

        private void CalculateDefaultPose(NativeBone bone)
        {
            // already calculated
            if (bone.DefaultPose.HasValue)
            {
                return;
            }

            // if no parent, the default pose equals the local one
            if (bone.ParentIndex < 0)
            {
                bone.DefaultPose = bone.DefaultLocalPose;
                return;
            }

            // get the parent and ensure its bindpose its calculated (recursively)
            NativeBone parent = _nativeBones[bone.ParentIndex];
            CalculateDefaultPose(parent);

            // calculate the bindpose using the parent's bindpose and our default local pose
            bone.DefaultPose = parent.DefaultPose.Value * bone.DefaultLocalPose;
        }

        private void SyncNativeBone(Bone bone, NativeBone nativeBone, ref bool skeletonChanged, ref bool humanChanged)
        {
            Transform transform = nativeBone.Transform;
            Transform parent    = _root.Transform;

            _bones.Add(transform);

            // if the bone wrapper declares a parent, get it by index. Otherwise, the parent is our root
            if (bone.HasParent() && !bone.IsParentExternal())
            {
                nativeBone.ParentIndex = (int)bone.ParentIndex();
                parent = _nativeBones[nativeBone.ParentIndex].Transform;
            }
            else
            {
                nativeBone.ParentIndex = -1;
                _rootBones.Add(transform);
            }

            // the parent changed, so update the entire transform, update the flags and return
            if (parent != transform.parent)
            {
                transform.SetParent(parent, worldPositionStays: false);
                nativeBone.ApplyDefaultPose();
                nativeBone.IsNew = false;
                nativeBone.DefaultPoseChanged = false;

                skeletonChanged = true;
                humanChanged |= nativeBone.IsHuman;
                return;
            }

            // apply the default pose if the native bone is new
            if (nativeBone.IsNew || nativeBone.DefaultPoseChanged)
            {
                nativeBone.ApplyDefaultPose();
                nativeBone.IsNew = false;
                nativeBone.DefaultPoseChanged = false;
            }
        }

        private sealed class NativeBone
        {
            public int           Index;
            public int           ParentIndex;
            public bool          IsHuman;
            public SkeletonBone? SkeletonBone;
            public Transform     Transform;
            public Matrix4x4?    DefaultPose;
            public Matrix4x4     DefaultLocalPose;
            public Vector3       Position;
            public Quaternion    Rotation;
            public Vector3       Scale;
            public bool          IsNew;
            public bool          DefaultPoseChanged;

            private Vector3    _prevPosition;
            private Quaternion _prevRotation;
            private Vector3    _prevScale;

            public void ApplyDefaultPose()
            {
                _prevPosition = Transform.localPosition;
                _prevRotation = Transform.localRotation;
                _prevScale    = Transform.localScale;

                Transform.localPosition = Position;
                Transform.localRotation = Rotation;
                Transform.localScale    = Scale;
            }

            public void ApplyHumanPose(bool applyDefaultIfNotInHumanDescription = false)
            {
                if (!SkeletonBone.HasValue)
                {
                    if (applyDefaultIfNotInHumanDescription)
                    {
                        ApplyDefaultPose();
                    }

                    return;
                }

                _prevPosition = Transform.localPosition;
                _prevRotation = Transform.localRotation;
                _prevScale    = Transform.localScale;

                Transform.localPosition = SkeletonBone.Value.position;
                Transform.localRotation = SkeletonBone.Value.rotation;
                Transform.localScale    = SkeletonBone.Value.scale;
            }

            public void RestorePose()
            {
                Transform.localPosition = _prevPosition;
                Transform.localRotation = _prevRotation;
                Transform.localScale    = _prevScale;
            }
        }
    }
}
