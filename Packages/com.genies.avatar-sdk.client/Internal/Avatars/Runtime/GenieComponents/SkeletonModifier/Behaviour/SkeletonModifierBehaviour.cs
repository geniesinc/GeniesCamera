using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Genies.Utilities;
using UnityEngine;
using UnityEngine.Animations.Rigging;
using UnityEngine.Assertions;

namespace Genies.Avatars
{
    /// <summary>
    /// Manages a list of joint modifiers for the skeleton of an <see cref="Animator"/> instance. Please note that
    /// translation modifiers may not work if working with a humanoid <see cref="Avatar"/> asset that has its
    /// hasTranslationDoF property disabled in the human description.
    /// </summary>
    [RequireComponent(typeof(Animator)), DisallowMultipleComponent]
    public sealed partial class SkeletonModifierBehaviour : MonoBehaviour
    {
        [Tooltip("Whether to restore joint transforms to their previous state when being removed")]
        public bool restoreRemovedJoints = true;
        [SerializeField] private List<JointModifier> modifiers = new();
        
        public int ModifierCount => modifiers.Count;
        
        // state
        private readonly List<JointJobData>                  _jobData          = new();
        private readonly List<JointJobData>                  _restoringJobData = new();
        private readonly HashSet<JointModifier>              _modifiersSet     = new(); // redundant data for fast access
        private readonly Dictionary<Transform, JointJobData> _jobDataByJoint   = new(); // redundant data for fast access
        private readonly HashSet<Transform>                  _jointsToRestore  = new(); // redundant data for fast access
        
        // rig constraint
        private Animator      _animator;
        private RigBuilder    _rigBuilder;
        private RigLayer      _rigLayer;
        private Rig           _rig;
        private RigConstraint _constraint;

        // Sometimes methods may be called on this behavior before awake is called...
        // If that happens we need to call awake manually and set this variable
        // so we dont call it twice (not that it'd be harmful from the looks of it, but still.)
        private bool _isInitialized = false;

        private void Awake()
        {
            Initialize();
        }

        private void Initialize()
        {
            if (_isInitialized)
            {
                return;
            }
            
            _animator = GetComponent<Animator>();

            // At this point, if the animator isn't there something bad has happened.
            Assert.IsTrue(_animator != null);
            
            RebuildDataFromModifiers();
            
            // setup RigConstraint
            _rigBuilder = GetComponent<RigBuilder>();
            if (_rigBuilder == null)
            {
                _rigBuilder = gameObject.AddComponent<RigBuilder>();
            }
            
            _rig = new GameObject("SkeletonModifierRig").AddComponent<Rig>();
            _rig.transform.SetParent(transform, worldPositionStays: false);
            _constraint = _rig.gameObject.AddComponent<RigConstraint>();
            _constraint.data.skeletonModifier = this;
            _rigLayer = new RigLayer(_rig);
            _rigBuilder.layers.Add(_rigLayer);
            _rigBuilder.Build();
            
            _isInitialized = true;
        }
        
        private void LateUpdate()
        {
            if (!_animator.enabled)
            {
                return;
            }

            // clear joints to restore at the end of each frame
            _restoringJobData.Clear();
            _jointsToRestore.Clear();

            float weight = _rig.weight * _constraint.weight;
            if (!_rigLayer.active || weight == 0.0f)
            {
                foreach (JointJobData data in _jobData)
                {
                    data.Joint.localScale = data.OriginalScale;
                }

                return;
            }
            
            /**
             * Evaluate the scale for job data with scale late update enabled. We do this because Unity's humanoid
             * animation system doesn't support non-uniform scaling. Setting the scale on the rig constraint job will
             * not work for human bones, so they should have scaleLateUpdate enabled.
             */
            foreach (JointJobData data in _jobData)
            {
                if (!data.RequiresScaleLateUpdate())
                {
                    continue;
                }

                Vector3 localScale = data.OriginalScale;
                foreach (JointModifier modifier in data.Modifiers)
                {
                    modifier.EvaluateScale(ref localScale);
                }

                data.Joint.localScale = Vector3.Lerp(data.OriginalScale, localScale, weight);
            }
        }

        private void OnDestroy()
        {
            Destroy().Forget();
        }

        public void AddModifier(JointModifier modifier)
        {
            if (modifier is null || _modifiersSet.Contains(modifier))
            {
                return;
            }

            modifiers.Add(modifier);
            _modifiersSet.Add(modifier);

            if (TryGetOrCreateJointJobData(modifier, out JointJobData data))
            {
                data.Modifiers.Add(modifier);
                data.RequiresScaleLateUpdate();
            }
        }
        
        public bool RemoveModifier(int index)
        {
            if (index < 0 || index >= modifiers.Count)
            {
                return false;
            }

            JointModifier modifier = modifiers[index];
            modifiers.RemoveAt(index);
            _modifiersSet.Remove(modifier);
            
            if (!TryGetJointJobData(modifier, out JointJobData data))
            {
                return true;
            }

            // if restoring joints on removal then manually restore the scale now
            if (restoreRemovedJoints && data.Joint)
            {
                data.Joint.localScale = data.OriginalScale;
            }

            // remove the modifier from the joint job data
            data.Modifiers.Remove(modifier);
            if (data.Modifiers.Count > 0)
            {
                data.RequiresScaleLateUpdate();
                return true;
            }

            // if the joint doesn't have any modifiers left then also remove its data
            _jobDataByJoint.Remove(modifier.TargetJoint);
            int dataIndex = _jobData.FindIndex(item => item.Modifiers == data.Modifiers);
            if (dataIndex >= 0 && dataIndex < _jobData.Count)
            {
                _jobData.RemoveAt(dataIndex);
            }

            // add restore data if we should restore this joint
            if (!restoreRemovedJoints || !modifier.TargetJoint)
            {
                return true;
            }

            var restoreData = new JointJobData
            {
                Joint       = modifier.TargetJoint,
                JointHandle = data.JointHandle,
            };
            
            _restoringJobData.Add(restoreData);
            _jointsToRestore.Add(modifier.TargetJoint);
            return true;
        }

        public bool RemoveModifier(JointModifier modifier)
        {
            return TryGetModifierIndex(modifier, out int index) && RemoveModifier(index);
        }
        
        public void AddModifiers(IEnumerable<JointModifier> modifiers)
        {
            foreach (JointModifier modifier in modifiers)
            {
                AddModifier(modifier);
            }
        }
        
        public void RemoveModifiers(IEnumerable<JointModifier> modifiers)
        {
            foreach (JointModifier modifier in modifiers)
            {
                RemoveModifier(modifier);
            }
        }
        
        public bool ContainsModifier(JointModifier modifier)
        {
            return _modifiersSet.Contains(modifier);
        }

        public bool IsJointModified(Transform joint)
        {
            return joint && _jobDataByJoint.ContainsKey(joint);
        }

        public bool TryGetModifierIndex(JointModifier modifier, out int index)
        {
            if (!_modifiersSet.Contains(modifier))
            {
                index = -1;
                return false;
            }
            
            index = modifiers.IndexOf(modifier);
            return index >= 0;
        }

        public JointModifier GetModifier(int index)
        {
            if (index >= 0 && index < modifiers.Count)
            {
                return modifiers[index];
            }

            return null;
        }

        public List<JointModifier> GetAllModifiers()
        {
            var results = new List<JointModifier>(modifiers.Count);
            GetAllModifiers(results);
            return results;
        }

        public void GetAllModifiers(ICollection<JointModifier> results)
        {
            foreach (JointModifier modifier in modifiers)
            {
                results.Add(modifier);
            }
        }

        public bool SetModifierIndex(JointModifier modifier, int index)
        {
            if (modifier is null || index < 0 || index >= modifiers.Count || !TryGetModifierIndex(modifier, out int currentIndex) || index == currentIndex)
            {
                return false;
            }

            // shift elements between currentIndex and index (only one for loop will actually run, depending on the dirction of the shift)
            for (int i = currentIndex; i < index; ++i)
            {
                modifiers[i] = modifiers[i + i];
            }

            for (int i = currentIndex; i > index; --i)
            {
                modifiers[i] = modifiers[i - i];
            }

            // set the modifier to its new index
            modifiers[index] = modifier;
            
            // update order within the joint job data for the modifier
            if (TryGetJointJobData(modifier, out JointJobData data))
            {
                data.Modifiers.Sort((left, right) => modifiers.IndexOf(left).CompareTo(modifiers.IndexOf(right)));
            }

            return true;
        }

        [ContextMenu("Remove All Modifiers")]
        public void RemoveAllModifiers()
        {
            if (restoreRemovedJoints)
            {
                foreach (JointJobData data in _jobData)
                {
                    if (!data.Joint || _jointsToRestore.Contains(data.Joint))
                    {
                        continue;
                    }

                    var restoreData = new JointJobData
                    {
                        Joint       = data.Joint,
                        JointHandle = data.JointHandle,
                    };
            
                    _restoringJobData.Add(restoreData);
                    _jointsToRestore.Add(data.Joint);
                    data.Joint.localScale = data.OriginalScale;
                }
            }
            
            modifiers.Clear();
            _modifiersSet.Clear();
            _jobData.Clear();
            _jobDataByJoint.Clear();
        }
        
        /**
         * Rebuilds everything assuming that the modifiers list had changes that were not performed by any of the public
         * methods. For example, this is called in OnValidate() when the dev makes changes directly on the editor.
         */
        private void RebuildDataFromModifiers()
        {
            // clear all modifiers from job data (but not joint handles, so we don't have to rebind them)
            foreach (JointJobData data in _jobData)
            {
                data.Modifiers.Clear();
            }

            // re-add all modifiers to the job datas and modifier set
            _modifiersSet.Clear();
            foreach (JointModifier modifier in modifiers)
            {
                _modifiersSet.Add(modifier);
                if (TryGetOrCreateJointJobData(modifier, out JointJobData data))
                {
                    data.Modifiers.Add(modifier);
                }
            }
            
            // refresh scaleLateUpdate setting and remove joint job data that doesn't have modifiers
            for (int i = 0; i < _jobData.Count; ++i)
            {
                JointJobData data = _jobData[i];
                data.RequiresScaleLateUpdate();
                
                if (data.Modifiers.Count > 0)
                {
                    continue;
                }

                _jobData.RemoveAt(i--);
                _jobDataByJoint.Remove(data.Joint);
                
                // add restore data if we should restore this joint
                if (!restoreRemovedJoints)
                {
                    continue;
                }

                var restoreData = new JointJobData
                {
                    Joint       = data.Joint,
                    JointHandle = data.JointHandle,
                };
            
                _restoringJobData.Add(restoreData);
                _jointsToRestore.Add(data.Joint);
            }
        }

        private bool TryGetOrCreateJointJobData(JointModifier modifier, out JointJobData data)
        {
            if (!modifier.TargetJoint)
            {
                data = default;
                return false;
            }

            if (!modifier.TargetJoint.IsChildOf(transform))
            {
                Debug.LogError($"[{nameof(SkeletonModifierBehaviour)}] invalid target joint for modifier. The target joint is not a child of the Animator hierarchy: {modifier.TargetJoint.GetPath()}");
                data = default;
                return false;
            }
            
            // data already exist, return it
            if (_jobDataByJoint.TryGetValue(modifier.TargetJoint, out data))
            {
                return true;
            }

            // Ensure the animator and other pieces are ready...
            Initialize();

            // data didn't exist, create it
            data = new JointJobData
            {
                Joint         = modifier.TargetJoint,
                JointHandle   = ReadWriteTransformHandle.Bind(_animator, modifier.TargetJoint),
                Modifiers     = new List<JointModifier>(),
                OriginalScale = modifier.TargetJoint.localScale,
            };
            
            _jobDataByJoint[modifier.TargetJoint] = data;
            _jobData.Add(data);
            
            // checkout if this joint was pending to be restored, in that case remove its restore data
            if (!_jointsToRestore.Contains(modifier.TargetJoint))
            {
                return true;
            }

            _jointsToRestore.Remove(modifier.TargetJoint);
            for (int i = 0; i < _restoringJobData.Count; ++i)
            {
                if (_restoringJobData[i].Joint == modifier.TargetJoint)
                {
                    _restoringJobData.RemoveAt(i--);
                }
            }
            
            return true;
        }

        private bool TryGetJointJobData(JointModifier modifier, out JointJobData data)
        {
            if (modifier.TargetJoint is not null)
            {
                return _jobDataByJoint.TryGetValue(modifier.TargetJoint, out data);
            }

            data = default;
            return false;
        }
        
        private async UniTaskVoid Destroy()
        {
            RemoveAllModifiers();
            
            // if the animator was not destroyed, and we have to restore removed joints then wait for one frame so joints are restored by the rig
            if (_animator && restoreRemovedJoints)
            {
                await UniTask.Yield(PlayerLoopTiming.LastPostLateUpdate);
            }

            _restoringJobData.Clear();
            _jointsToRestore.Clear();

            // remove rig layer and rebuild
            if (_rigBuilder)
            {
                _rigBuilder.layers.Remove(_rigLayer);
                _rigBuilder.Build();
            }
            
            // destroy rig
            if (_rig)
            {
                Destroy(_rig.gameObject);
            }

            _animator = null;
            _rigBuilder = null;
            _rigLayer = null;
            _rig = null;
            _constraint = null;
        }
        
        /**
         * Here is some editor hacks to fix some issues when editing joint modifiers from the inspector while playing.
         * Since JointModifier is a serializable class Unity will create new instances everytime you add or remove
         * joints on the inspector. The whole implementation of this component relies on referencing this modifiers
         * by instance, so this hacky code will keep a cache of instances based on their configuration, so they are
         * restored if new items are added/removed to the modifiers list.
         */
#if UNITY_EDITOR
        private void OnValidate()
        {
            if (!Application.isPlaying)
                return;
            
            if (!_animator)
                _animator = GetComponent<Animator>();
            
            // restore the scale on all joints
            foreach (JointJobData data in _jobData)
                data.Joint.localScale = data.OriginalScale;
            
            // rebuild rig constraint data
            RebuildDataFromModifiers();
        }
#endif
    }
}
