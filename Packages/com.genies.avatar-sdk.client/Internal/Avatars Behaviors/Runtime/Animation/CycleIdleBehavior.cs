using System;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Genies.Avatars.Behaviors
{
    /// <summary>
    /// A StateMachineBehaviour that automatically cycles between different idle animations based on configurable timing and rules.
    /// This behavior manages when to trigger different idle animation transitions and prevents or allows repetition of the same idle.
    /// </summary>
    public class CycleIdleBehavior : StateMachineBehaviour
    {
        [System.Obsolete("Use 'BaseIdleMinMaxCycles' instead.")]
        public Vector2 baseIdleMinMaxCycles
        {
            get => BaseIdleMinMaxCycles;
            set => BaseIdleMinMaxCycles = value;
        }

        [System.Obsolete("Use 'AllowRepeatIdles' instead.")]
        public bool allowRepeatIdles
        {
            get => AllowRepeatIdles;
            set => AllowRepeatIdles = value;
        }

        /// <summary>
        /// The minimum and maximum number of cycles before switching to a different idle animation.
        /// X component represents the minimum multiplier, Y component represents the maximum multiplier of the state length.
        /// </summary>
        public Vector2 BaseIdleMinMaxCycles = new Vector2(1f,3f);

        /// <summary>
        /// Whether the same idle animation can be repeated consecutively.
        /// If false, the same idle will not be chosen twice in a row.
        /// </summary>
        public bool AllowRepeatIdles = false;

        private float _enterBaseIdleTime = 0f;
        private float _baseIdleSwitchTime = 0f;
        private string _lastIdleTrigger = "";
        private bool _selectedNextIdle = false;

        /// <summary>
        /// Called when entering the idle animation state. Initializes timing for the next idle transition.
        /// </summary>
        /// <param name="animator">The Animator component this behavior is attached to.</param>
        /// <param name="stateInfo">Information about the current animation state.</param>
        /// <param name="layerIndex">The index of the animator layer containing this state.</param>
        public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) {
            _enterBaseIdleTime = Time.time;
            _baseIdleSwitchTime = Random.Range(stateInfo.length * BaseIdleMinMaxCycles.x, stateInfo.length * BaseIdleMinMaxCycles.y);
            _selectedNextIdle = false;
        }

        /// <summary>
        /// Called during each frame while in the idle animation state. Checks if it's time to transition to a new idle animation.
        /// Selects a random allowable animation transition based on camera focus state and repeat idle settings.
        /// </summary>
        /// <param name="animator">The Animator component this behavior is attached to.</param>
        /// <param name="stateInfo">Information about the current animation state.</param>
        /// <param name="layerIndex">The index of the animator layer containing this state.</param>
        public override void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) {
            if (!_selectedNextIdle && Time.time - _enterBaseIdleTime > _baseIdleSwitchTime) {
                var triggers = animator.parameters.Where(p => p.type == AnimatorControllerParameterType.Trigger).ToList();

                if (AllowRepeatIdles == false) {
                    var lastTriggerIndex = triggers.FindIndex(t=> string.Equals(t.name, _lastIdleTrigger));
                    if (lastTriggerIndex >= 0)
                    {
                        triggers.RemoveAt(lastTriggerIndex);
                    }
                }

                // Filter triggers by the allowed animation transition names, which are determined by the camera focus state
                string[] allowableAnimationTransitions = CameraFocusStateMachine.GetAllowableAnimationTransitions();
                triggers = triggers.Where(trigger => allowableAnimationTransitions.Contains(trigger.name)).ToList();
                if (triggers.Count == 0)
                {
                    _lastIdleTrigger = String.Empty;
                    return;
                }

                var nextTrigger = triggers[Random.Range(0, triggers.Count)];
                animator.SetTrigger(nextTrigger.name);
                _lastIdleTrigger = nextTrigger.name;
                _selectedNextIdle = true;
            }
        }
    }
}
