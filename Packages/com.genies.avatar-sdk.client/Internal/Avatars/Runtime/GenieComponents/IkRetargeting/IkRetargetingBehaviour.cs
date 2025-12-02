using System;
using System.Collections.Generic;
using Genies.Utilities;
using UnityEngine;
using UnityEngine.Events;

namespace Genies.Avatars
{
    /// <summary>
    /// <see cref="MonoBehaviour"/> implementation required to add IK retargeting support. It can be used as a standalone
    /// component when added to any GameObject with an <see cref="Animator"/>, as long as you provide a config asset
    /// and use an animator controller with IK retargeting support.
    /// </summary>
    [RequireComponent(typeof(Animator)), DisallowMultipleComponent]
    public sealed class IkRetargetingBehaviour : MonoBehaviour
    {
        [SerializeField]
        private IkrConfig config;
        [SerializeField, Tooltip("Invoked right after rebuilding")]
        private UnityEvent rebuilt = new();
        [SerializeField, Tooltip("Invoked right before retargeting is performed on each frame")]
        private UnityEvent retargeting = new();
        [Tooltip("If true, IK hints will be set to the transforms coming from the animation clip")]
        public bool setIkHints;
        [Tooltip("If enabled, the component will rebuild on each IK update, only when it is dirty")]
        public bool autoRebuild = true;

        public UnityEvent Rebuilt => rebuilt;
        public UnityEvent Retargeting => retargeting;
        
        public IkrConfig Config
        {
            get => config;
            set
            {
                if (config == value)
                {
                    return;
                }

                config = value;
                Rebuild();
            }
        }
        
        /// <summary>
        /// Subscribe to this event to post-process created goals on each rebuild (i.e.: you can add extra goals or targets).
        /// </summary>
        public event Action<List<IkrGoal>, AnimatorParameters> PostprocessGoals = delegate { };
        
        // state
        private readonly List<IkrGoal> _goals = new();
        private readonly HashSet<IIkrTarget> _targets = new();
        private readonly Dictionary<string, IIkrTarget> _targetsByKey = new();
        private Animator _animator;
        private AnimatorParameters _parameters;
        private RuntimeAnimatorController _currentController;
        private int _currentParametersHash;
        
        // seems that Unity forgot to make the component enabled toggle available when implementing OnAnimatorIK. So we just put this empty method so it becomes available
        private void OnEnable() { }
        
        private void Awake()
        {
            _animator = GetComponent<Animator>();
            _parameters = new AnimatorParameters(_animator, refresh: false);
            _currentParametersHash = _parameters.HashParameters();
        }

        public bool TryGetTarget(string key, out IIkrTarget target)
        {
            RebuildIfDirty();
            return _targetsByKey.TryGetValue(key, out target);
        }
        
        public bool TryGetFreeTargetTransform(string key, out Transform transform)
        {
            RebuildIfDirty();
            if (_targetsByKey.TryGetValue(key, out IIkrTarget target) && target.IsFree && target.Transform)
            {
                transform = target.Transform;
                return true;
            }
            
            transform = null;
            return false;
        }

        /// <summary>
        /// Forces a rebuild of the component.
        /// </summary>
        [ContextMenu("Rebuild")]
        public void Rebuild()
        {
            // refresh state and rebuild
            _parameters.Refresh();
            _currentController = _animator.runtimeAnimatorController;
            _currentParametersHash = _parameters.HashParameters();
            RebuildWithoutStateRefresh();
        }
        
        /// <summary>
        /// Rebuilds the component only if the animation parameters changed on the animator.
        /// </summary>
        [ContextMenu("Rebuild If Dirty")]
        public void RebuildIfDirty()
        {
            if (_animator.runtimeAnimatorController == _currentController)
            {
                return;
            }

            _currentController = _animator.runtimeAnimatorController;
            
            // if controller changed but parameters are the same don't rebuild
            _parameters.Refresh();
            int parametersHash = _parameters.HashParameters();
            if (parametersHash == _currentParametersHash)
            {
                return;
            }

            _currentParametersHash = parametersHash;
            
            RebuildWithoutStateRefresh();
        }
        
        private void RebuildWithoutStateRefresh()
        {
            // recreate IKR goals (pass the targets dictionary to try reusing as many target instances as we can)
            _goals.Clear();
            IkrGoal.CreateGoals(_animator, _parameters, config, _goals, _targetsByKey);
            
            // make sure to add new targets since they could be removed by the postprocessing event
            foreach (IkrGoal goal in _goals)
            {
                foreach (IIkrTarget target in goal.Targets)
                {
                    _targets.Add(target);
                }
            }

            // post process goals (anyone subscribed to this event can modify/add/remove goals)
            PostprocessGoals.Invoke(_goals, _parameters);
            
            // now that we have the final goals setup, remove current targets, so we can dispose remaining ones
            foreach (IkrGoal goal in _goals)
            {
                foreach (IIkrTarget target in goal.Targets)
                {
                    _targets.Remove(target);
                }
            }
            
            // dispose targets that are no longer used
            foreach (IIkrTarget target in _targets)
            {
                target.Dispose();
            }

            // register current targets
            _targets.Clear();
            _targetsByKey.Clear();
            foreach (IkrGoal goal in _goals)
            {
                foreach (IIkrTarget target in goal.Targets)
                {
                    _targets.Add(target);
                    _targetsByKey.Add(target.Key, target);
                }
            }
            
            _animator.Rebind();
            rebuilt.Invoke();
        }
        
        /**
         * This method is called only when the animator controllers has layers with IK pass enabled, so we don't have to
         * worry about performance when the avatar has animations without IK retargeting support.
         */
        private void OnAnimatorIK(int layerIndex)
        {
            if (autoRebuild)
            {
                RebuildIfDirty();
            }

            retargeting.Invoke();
            
            foreach (IkrGoal goal in _goals)
            {
                goal.OnAnimatorIK(setIkHints);
            }
        }

        private void OnDestroy()
        {
            foreach (IIkrTarget target in _targets)
            {
                target.Dispose();
            }

            _targets.Clear();
            _targetsByKey.Clear();
            _goals.Clear();
        }
    }
}
