using System.Collections.Generic;
using Genies.Utilities;
using Newtonsoft.Json.Linq;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Genies.Avatars
{
    /// <summary>
    /// Genie component that can modify the skeleton joints. Uses the <see cref="SkeletonModifierBehaviour"/> but wraps
    /// joint modifiers with a custom implementation that uses transform name/paths instead of direct references. We do
    /// this since a <see cref="IGenie"/> can rebuild its skeleton multiple times, so we need to make sure to keep the
    /// <see cref="SkeletonModifierBehaviour"/> updated with the correct joint references.
    /// </summary>
    [SerializableAs(typeof(GenieComponent), "skeleton-modifier")]
    public sealed class SkeletonModifier : GenieComponent
    {
        public override string Name => "Skeleton Modifier";
        
        public int ModifierCount => _modifiers.Count;
        public bool RestoreRemovedJoints
        {
            get => _behaviour.restoreRemovedJoints;
            set => _behaviour.restoreRemovedJoints = value;
        }
        
        private readonly List<GenieJointModifier>      _modifiers;
        private readonly Dictionary<string, Transform> _jointsByName;
        private SkeletonModifierBehaviour _behaviour;
        private Avatar _previousAvatar;
        private Avatar _generatedAvatar;
        
        public SkeletonModifier()
            : this(null) { }

        public SkeletonModifier(IEnumerable<GenieJointModifier> modifiers)
        {
            _modifiers = modifiers is null ? new List<GenieJointModifier>() : new List<GenieJointModifier>(modifiers);
            _jointsByName = new Dictionary<string, Transform>();
        }
        
        /// <summary>
        /// Forces a rebuild of the component. You should not need to call this if the <see cref="IGenie.RootRebuilt"/>
        /// event on the genie is being invoked as expected. If you are making any modifications to the skeleton root
        /// that doesn't trigger the RootRebuilt event then you may need to call this method.
        /// </summary>
        public void Rebuild()
        {
            RebuildAvatarIfRequired();
            
            // update joints by name dic
            _jointsByName.Clear();
            Genie.Root.transform.AddChildrenByName(_jointsByName);
            
            // remove all joint modifiers
            _behaviour.RemoveAllModifiers();
            
            // rebuild all modifiers
            foreach (GenieJointModifier modifier in _modifiers)
            {
                if (!TryGetJoint(modifier.TargetJoint, out Transform joint))
                {
                    continue;
                }

                modifier.RebuildWrappedModifier(joint);
                _behaviour.AddModifier(modifier.WrappedModifier);
            }
        }

        public override GenieComponent Copy()
        {
            if (_modifiers.Count == 0)
            {
                return new SkeletonModifier();
            }

            // copy all modifiers
            var modifiersCopy = new GenieJointModifier[_modifiers.Count];
            for (int i = 0; i < modifiersCopy.Length; ++i)
            {
                modifiersCopy[i] = new GenieJointModifier(_modifiers[i]);
            }

            return new SkeletonModifier(modifiersCopy);
        }

        public bool TryGetJoint(string nameOrPath, out Transform joint)
        {
            if (string.IsNullOrEmpty(nameOrPath))
            {
                joint = null;
                return false;
            }
            
            if (_jointsByName.TryGetValue(nameOrPath, out joint))
            {
                return true;
            }

            joint = Genie.Root.transform.Find(nameOrPath);
            return joint;
        }
        
        protected override bool TryInitialize()
        {
            // disallow multiple SkeletonModifier components on the same genie
            if (Genie.Components.TryGet<SkeletonModifier>(out _))
            {
                Debug.LogError($"This genie already has a {nameof(SkeletonModifier)} component");
                return false;
            }

            _behaviour = Genie.Animator.gameObject.AddComponent<SkeletonModifierBehaviour>();
            Genie.RootRebuilt += Rebuild;
            Rebuild();
            
            return true;
        }

        protected override void OnRemoved()
        {
            /**
             * Don't remove modifiers as that is part of the config of this component, we just need to restore the genie
             * state. By keeping the modifiers then if the component is added back to another genie the components will
             * be there.
             */
            
            Genie.RootRebuilt -= Rebuild;
            if (Genie.Animator && _previousAvatar)
            {
                Genie.Animator.avatar = _previousAvatar;
            }

            if (_behaviour)
            {
                Object.Destroy(_behaviour);
            }

            if (_generatedAvatar)
            {
                Object.Destroy(_generatedAvatar);
            }

            _jointsByName.Clear();
            _behaviour = null;
            _previousAvatar = null;
            _generatedAvatar = null;
        }

        private void RebuildAvatarIfRequired()
        {
            Avatar avatar = Genie.Animator.avatar;
            if (!avatar || !avatar.isValid || !avatar.isHuman)
            {
                return;
            }

            /**
             * We need the hasTranslationDoF option enabled so modifiers that modify position work as expected. If the
             * current Avatar asset has it disabled we have to build a new one.
             */
            HumanDescription humanDescription = avatar.humanDescription;
            if (humanDescription.hasTranslationDoF)
            {
                return;
            }

            _previousAvatar = avatar;
            if (_generatedAvatar)
            {
                Object.Destroy(_generatedAvatar);
            }

            // human description does not have translation DoF, so we have to rebuild the avatar
            humanDescription.hasTranslationDoF = true;
            _generatedAvatar = AvatarBuilder.BuildHumanAvatar(Genie.Root, humanDescription);
            Genie.Animator.avatar = _generatedAvatar;
        }

        public JToken Serialize()
        {
            return JToken.FromObject(_modifiers);
        }

        public static GenieComponent Deserialize(JToken token)
        {
            var modifiers = token.ToObject<List<GenieJointModifier>>();
            return new SkeletonModifier(modifiers);
        }
        
#region BehaviourWrapper
        public void AddModifier(GenieJointModifier modifier)
        {
            if (modifier is null || _behaviour.ContainsModifier(modifier.WrappedModifier))
            {
                return;
            }

            _modifiers.Add(modifier);
            
            if (!TryGetJoint(modifier.TargetJoint, out Transform joint))
            {
                return;
            }

            modifier.RebuildWrappedModifier(joint);
            _behaviour.AddModifier(modifier.WrappedModifier);
        }
        
        public bool RemoveModifier(int index)
        {
            if (index < 0 || index >= _modifiers.Count)
            {
                return false;
            }

            GenieJointModifier modifier = _modifiers[index];
            _modifiers.RemoveAt(index);
            _behaviour.RemoveModifier(modifier.WrappedModifier);
            modifier.RebuildWrappedModifier(null);
            return true;
        }

        public bool RemoveModifier(GenieJointModifier modifier)
        {
            return TryGetModifierIndex(modifier, out int index) && RemoveModifier(index);
        }
        
        public void AddModifiers(IEnumerable<GenieJointModifier> modifiers)
        {
            foreach (GenieJointModifier modifier in modifiers)
            {
                AddModifier(modifier);
            }
        }
        
        public void RemoveModifiers(IEnumerable<GenieJointModifier> modifiers)
        {
            foreach (GenieJointModifier modifier in modifiers)
            {
                RemoveModifier(modifier);
            }
        }
        
        public bool ContainsModifier(GenieJointModifier modifier)
        {
            return _behaviour.ContainsModifier(modifier.WrappedModifier);
        }

        public bool IsJointModified(Transform joint)
        {
            return _behaviour.IsJointModified(joint);
        }
        
        public bool IsJointModified(string jointNameOrPath)
        {
            return TryGetJoint(jointNameOrPath, out Transform joint) && _behaviour.IsJointModified(joint);
        }

        public bool TryGetModifierIndex(GenieJointModifier modifier, out int index)
        {
            index = _modifiers.IndexOf(modifier);
            return index >= 0;
        }

        public GenieJointModifier GetModifier(int index)
        {
            if (index >= 0 && index < _modifiers.Count)
            {
                return _modifiers[index];
            }

            return null;
        }

        public List<GenieJointModifier> GetAllModifiers()
        {
            var results = new List<GenieJointModifier>(_modifiers.Count);
            GetAllModifiers(results);
            return results;
        }

        public void GetAllModifiers(ICollection<GenieJointModifier> results)
        {
            foreach (GenieJointModifier modifier in _modifiers)
            {
                results.Add(modifier);
            }
        }

        public bool SetModifierIndex(GenieJointModifier modifier, int index)
        {
            if (modifier is null || index < 0 || index >= _modifiers.Count || !TryGetModifierIndex(modifier, out int currentIndex) || index == currentIndex)
            {
                return false;
            }

            // shift elements between currentIndex and index (only one for loop will actually run, depending on the dirction of the shift)
            for (int i = currentIndex; i < index; ++i)
            {
                _modifiers[i] = _modifiers[i + i];
            }

            for (int i = currentIndex; i > index; --i)
            {
                _modifiers[i] = _modifiers[i - i];
            }

            // set the modifier to its new index
            _modifiers[index] = modifier;

            // since the behaviour could have modifiers added by other scripts, make sure to set the index relative to an adjacent modifier
            if (index == 0)
            {
                _behaviour.TryGetModifierIndex(_modifiers[1].WrappedModifier, out int adjacentIndex);
                _behaviour.SetModifierIndex(modifier.WrappedModifier, adjacentIndex - 1);
            }
            else
            {
                _behaviour.TryGetModifierIndex(_modifiers[index - 1].WrappedModifier, out int adjacentIndex);
                _behaviour.SetModifierIndex(modifier.WrappedModifier, adjacentIndex + 1);
            }
            
            return true;
        }

        public void RemoveAllModifiers()
        {
            _modifiers.Clear();
            _behaviour.RemoveAllModifiers();
        }
#endregion
    }
}