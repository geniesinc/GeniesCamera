using System;
using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Genies.UI.Animations
{
    /// <summary>
    /// Easing types for animations
    /// </summary>
    public enum Ease
    {
        Linear,
        InSine,
        OutSine,
        InOutSine,
        InQuad,
        OutQuad,
        InOutQuad,
        InCubic,
        OutCubic
    }

    /// <summary>
    /// Base UI animation interface
    /// </summary>
    public interface IUIAnimation
    {
        bool IsActive { get; }
        void Terminate();
    }

    /// <summary>
    /// Animator class that controls individual animations
    /// </summary>
    public class UIAnimator : IUIAnimation, System.Collections.IEnumerator
    {
        internal Coroutine Coroutine { get; set; }
        internal Ease Ease { get; set; }
        internal bool UseUnscaledTime { get; set; }
        internal float Delay { get; set; }
        internal bool HasStarted { get; set; }

        private MonoBehaviour _host;
        private bool _isTerminated;
        private bool _isCompleted;
        public bool IsActive => !_isTerminated && !_isCompleted;

        public event Action Completed;
        public event Action Terminated;

        private event Action CompletedOneShot;
        private event Action TerminatedOneShot;

        internal UIAnimator(MonoBehaviour host, Coroutine coroutine, Ease ease, bool useUnscaledTime)
        {
            _host = host;
            Coroutine = coroutine;
            _isTerminated = false;
            _isCompleted = false;
            Ease = ease;
            UseUnscaledTime = useUnscaledTime;
            Delay = 0f;
        }

        public UIAnimator SetEase(Ease ease)
        {
            Ease = ease;
            _customCurve = null;
            return this;
        }

        internal AnimationCurve _customCurve;

        public UIAnimator SetEase(AnimationCurve curve)
        {
            _customCurve = curve;
            Ease = Ease.Linear; // Will be overridden by curve
            return this;
        }

        public UIAnimator SetUpdate(bool useUnscaledTime)
        {
            UseUnscaledTime = useUnscaledTime;
            return this;
        }

        public UIAnimator SetDelay(float delay)
        {
            Delay = delay;
            return this;
        }

        public UIAnimator OnCompletedOneShot(Action callback)
        {
            CompletedOneShot += callback;
            return this;
        }

        public UIAnimator OnTerminatedOneShot(Action callback)
        {
            TerminatedOneShot += callback;
            return this;
        }

        /// <summary>
        /// Start the animation - explicit start required (no auto-play)
        /// </summary>
        public UIAnimator Start()
        {
            if (!HasStarted && Coroutine == null && _host != null)
            {
                HasStarted = true;
                // Animation is already started by CreateAnimation, but this allows re-starting if needed
            }
            return this;
        }

        /// <summary>
        /// Play() - Alias for Start() for compatibility
        /// </summary>
        public UIAnimator Play() => Start();

        public void Terminate()
        {
            if (_isTerminated || _host == null)
            {
                TerminatedOneShot?.Invoke();
                TerminatedOneShot = null;

                return;
            }

            _isTerminated = true;
            if (Coroutine != null)
            {
                _host.StopCoroutine(Coroutine);
            }

            GeniesUIAnimation.UnregisterAnimation(_host, this);

            Terminated?.Invoke();

            TerminatedOneShot?.Invoke();
            TerminatedOneShot = null;
        }

        internal void Complete()
        {
            _isCompleted = true;
            GeniesUIAnimation.UnregisterAnimation(_host, this);

            Completed?.Invoke();

            CompletedOneShot?.Invoke();
            CompletedOneShot = null;
        }

        public async UniTask AsyncWaitForCompletion()
        {
            while (IsActive && !_isTerminated && !_isCompleted)
            {
                await UniTask.Yield();
            }
        }

        public IEnumerator WaitForCompletion()
        {
            while (IsActive && !_isTerminated && !_isCompleted)
            {
                yield return null;
            }
        }

        // IEnumerator implementation for UniTask compatibility
        public object Current => null;

        public bool MoveNext()
        {
            return IsActive && !_isTerminated && !_isCompleted;
        }

        public void Reset()
        {
            // Not needed for Unity coroutines
        }

        // Make Animator implement IEnumerator for UniTask compatibility
        public IEnumerator GetEnumerator()
        {
            return WaitForCompletion();
        }
    }


    /// <summary>
    /// AnimationGroup - Groups multiple animations together (replaces DOTween's Sequence)
    /// More explicit naming: Add() for sequential, AddParallel() for parallel execution
    /// </summary>
    public class AnimationGroup : UIAnimator
    {
        private class AnimationStep
        {
            public List<UIAnimator> Animations = new List<UIAnimator>();
        }

        private List<AnimationStep> _steps = new List<AnimationStep>();
        private MonoBehaviour _groupHost;

        internal AnimationGroup(MonoBehaviour host) : base(host, null, Ease.Linear, false)
        {
            _groupHost = host;
            HasStarted = false;
        }

        /// <summary>
        /// Add animation to run sequentially after previous animations complete
        /// </summary>
        public AnimationGroup Add(UIAnimator animator)
        {
            var step = new AnimationStep();
            step.Animations.Add(animator);
            _steps.Add(step);
            return this;
        }

        /// <summary>
        /// Add animation to run in parallel with the last added animation
        /// </summary>
        public AnimationGroup AddParallel(UIAnimator animator)
        {
            if (_steps.Count == 0)
            {
                _steps.Add(new AnimationStep());
            }
            _steps[_steps.Count - 1].Animations.Add(animator);
            return this;
        }

        /// <summary>
        /// Start the animation group - explicit start required
        /// </summary>
        public new AnimationGroup Start()
        {
            if (!HasStarted && _groupHost != null)
            {
                HasStarted = true;
                var coroutine = _groupHost.StartCoroutine(ExecuteGroup());
                Coroutine = coroutine;
            }
            return this;
        }

        private IEnumerator ExecuteGroup()
        {
            // Execute each step sequentially
            foreach (var step in _steps)
            {
                // Start all animations in this step (they run in parallel)
                foreach (var animation in step.Animations)
                {
                    animation.Start();
                }

                // Wait for all animations in this step to complete
                foreach (var animation in step.Animations)
                {
                    yield return animation.WaitForCompletion();
                }
            }

            Complete();
        }

        public new IEnumerator WaitForCompletion()
        {
            while (IsActive)
            {
                yield return null;
            }
        }
    }

    /// <summary>
    /// Sequence - Alias for AnimationGroup (for compatibility)
    /// </summary>
    public class Sequence : AnimationGroup
    {
        internal Sequence(MonoBehaviour host) : base(host) { }
    }

    /// <summary>
    /// Generic animator core for type compatibility
    /// </summary>
    public class UIAnimatorCore<T1, T2, TPlugOptions> : UIAnimator
    {
        internal UIAnimatorCore(MonoBehaviour host, Coroutine coroutine, Ease ease, bool useUnscaledTime)
            : base(host, coroutine, ease, useUnscaledTime)
        {
        }

        // Inherit IEnumerator support from base Animator class
        // Override to ensure proper type compatibility
        public new IEnumerator GetEnumerator()
        {
            return WaitForCompletion();
        }
    }

    /// <summary>
    /// Spring-based animator for physics-based animations
    /// Much more natural and organic than easing curves
    /// </summary>
    public class SpringUIAnimator : UIAnimator
    {
        private SpringPhysics.SpringConfig _springConfig;

        internal SpringUIAnimator(MonoBehaviour host, SpringPhysics.SpringConfig config)
            : base(host, null, Ease.Linear, false)
        {
            _springConfig = config;
        }

        public SpringUIAnimator WithStiffness(float stiffness)
        {
            _springConfig.Stiffness = stiffness;
            return this;
        }

        public SpringUIAnimator WithDamping(float damping)
        {
            _springConfig.Damping = damping;
            return this;
        }

        public SpringUIAnimator WithMass(float mass)
        {
            _springConfig.Mass = mass;
            return this;
        }

        internal SpringPhysics.SpringConfig GetConfig() => _springConfig;
    }

    /// <summary>
    /// Core animator system
    /// </summary>
    public static class GeniesUIAnimation
    {
        private static Dictionary<MonoBehaviour, List<UIAnimator>> _activeAnimations = new Dictionary<MonoBehaviour, List<UIAnimator>>();

        internal static void RegisterAnimation(MonoBehaviour host, UIAnimator uiAnimator)
        {
            if (!_activeAnimations.ContainsKey(host))
            {
                _activeAnimations[host] = new List<UIAnimator>();
            }
            _activeAnimations[host].Add(uiAnimator);
        }

        internal static void UnregisterAnimation(MonoBehaviour host, UIAnimator uiAnimator)
        {
            if (_activeAnimations.ContainsKey(host))
            {
                _activeAnimations[host].Remove(uiAnimator);
                if (_activeAnimations[host].Count == 0)
                {
                    _activeAnimations.Remove(host);
                }
            }
        }

        public static bool IsAnimating(Transform target)
        {
            var component = target.GetComponent<MonoBehaviour>();
            if (component != null && _activeAnimations.ContainsKey(component))
            {
                var animations = _activeAnimations[component];
                animations.RemoveAll(t => !t.IsActive);
                return animations.Count > 0;
            }
            return false;
        }

        public static void TerminateAnimations(MonoBehaviour host)
        {
            if (_activeAnimations.ContainsKey(host))
            {
                var animations = new List<UIAnimator>(_activeAnimations[host]);
                foreach (var anim in animations)
                {
                    anim.Terminate();
                }
            }
        }

        /// <summary>
        /// Create a new AnimationGroup for chaining animations
        /// </summary>
        public static AnimationGroup CreateGroup(Component host)
        {
            var manager = UIAnimationManager.Instance;
            return new AnimationGroup(manager);
        }

        /// <summary>
        /// Sequence() - Alias for CreateGroup() for compatibility
        /// </summary>
        public static Sequence Sequence(Component host)
        {
            return (Sequence)CreateGroup(host);
        }

        internal static UIAnimator CreateAnimation(MonoBehaviour host, float duration, Action<float> updateAction, Ease ease, bool useUnscaledTime, bool autoStart = true)
        {
            // Use manager to run coroutines instead of the target host
            // This allows animations to run even when the target GameObject is inactive
            var manager = Animations.UIAnimationManager.Instance;

            var animator = new UIAnimator(manager, null, ease, useUnscaledTime);
            RegisterAnimation(host, animator);

            // Start the coroutine immediately (for backward compatibility)
            // New code should use explicit Start() pattern
            if (autoStart)
            {
                var coroutine = manager.StartCoroutine(AnimationCoroutine(animator, duration, updateAction));
                animator.Coroutine = coroutine;
                animator.HasStarted = true;
            }

            return animator;
        }

        /// <summary>
        /// Create animation with configuration object (preferred method)
        /// </summary>
        internal static UIAnimator CreateAnimation(MonoBehaviour host, float duration, Action<float> updateAction, AnimationSettings settings)
        {
            var manager = Animations.UIAnimationManager.Instance;

            // Determine easing - custom curve takes precedence
            Ease ease = settings.CustomCurve != null ? Ease.Linear : settings.Easing;

            var animator = new UIAnimator(manager, null, ease, settings.UseUnscaledTime);

            // Apply settings
            animator.Delay = settings.Delay;
            if (settings.CustomCurve != null)
            {
                animator._customCurve = settings.CustomCurve;
            }

            if (settings.OnComplete != null)
            {
                animator.Completed += settings.OnComplete;
            }

            RegisterAnimation(host, animator);

            // Start if auto-start is enabled
            if (settings.AutoStart)
            {
                var coroutine = manager.StartCoroutine(AnimationCoroutine(animator, duration, updateAction));
                animator.Coroutine = coroutine;
                animator.HasStarted = true;
            }

            return animator;
        }

        internal static UIAnimatorCore<T1, T2, TPlugOptions> CreateAnimationCore<T1, T2, TPlugOptions>(MonoBehaviour host, float duration, Action<float> updateAction, Ease ease, bool useUnscaledTime, bool autoStart = true)
        {
            // Use manager to run coroutines instead of the target host
            // This allows animations to run even when the target GameObject is inactive
            var manager = Animations.UIAnimationManager.Instance;

            var animator = new UIAnimatorCore<T1, T2, TPlugOptions>(manager, null, ease, useUnscaledTime);
            RegisterAnimation(host, animator);

            // Start the coroutine immediately (for backward compatibility)
            if (autoStart)
            {
                var coroutine = manager.StartCoroutine(AnimationCoroutine(animator, duration, updateAction));
                animator.Coroutine = coroutine;
                animator.HasStarted = true;
            }

            return animator;
        }

        /// <summary>
        /// Create animation core with configuration object (preferred method)
        /// </summary>
        internal static UIAnimatorCore<T1, T2, TPlugOptions> CreateAnimationCore<T1, T2, TPlugOptions>(MonoBehaviour host, float duration, Action<float> updateAction, AnimationSettings settings)
        {
            var manager = Animations.UIAnimationManager.Instance;

            // Determine easing - custom curve takes precedence
            Ease ease = settings.CustomCurve != null ? Ease.Linear : settings.Easing;

            var animator = new UIAnimatorCore<T1, T2, TPlugOptions>(manager, null, ease, settings.UseUnscaledTime);

            // Apply settings
            animator.Delay = settings.Delay;
            if (settings.CustomCurve != null)
            {
                animator._customCurve = settings.CustomCurve;
            }
            if (settings.OnComplete != null)
            {
                animator.Completed += settings.OnComplete;
            }

            RegisterAnimation(host, animator);

            // Start if auto-start is enabled
            if (settings.AutoStart)
            {
                var coroutine = manager.StartCoroutine(AnimationCoroutine(animator, duration, updateAction));
                animator.Coroutine = coroutine;
                animator.HasStarted = true;
            }

            return animator;
        }

        /// <summary>
        /// Create a spring-based animation (physics-based, no fixed duration)
        /// </summary>
        internal static SpringUIAnimator CreateSpringAnimation(MonoBehaviour host, System.Func<bool> updateAction, SpringPhysics.SpringConfig config, bool useUnscaledTime, bool autoStart = true)
        {
            var manager = Animations.UIAnimationManager.Instance;

            var animator = new SpringUIAnimator(manager, config);
            animator.UseUnscaledTime = useUnscaledTime;
            RegisterAnimation(host, animator);

            // Start the spring coroutine (for backward compatibility)
            if (autoStart)
            {
                var coroutine = manager.StartCoroutine(SpringCoroutine(animator, updateAction));
                animator.Coroutine = coroutine;
                animator.HasStarted = true;
            }

            return animator;
        }

        /// <summary>
        /// Coroutine for spring-based animations
        /// Runs until spring settles at equilibrium
        /// </summary>
        internal static IEnumerator SpringCoroutine(SpringUIAnimator uiAnimator, System.Func<bool> updateAction)
        {
            // Handle delay
            if (uiAnimator.Delay > 0)
            {
                float delayElapsed = 0f;
                while (delayElapsed < uiAnimator.Delay)
                {
                    delayElapsed += uiAnimator.UseUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
                    yield return null;
                }
            }

            // Run spring simulation until settled
            while (true)
            {
                float deltaTime = uiAnimator.UseUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;

                // Update action returns true when spring has settled
                bool settled = updateAction();

                if (settled)
                {
                    break;
                }

                yield return null;
            }

            uiAnimator.Complete();
        }

        public static float Evaluate(Ease ease, float t)
        {
            switch (ease)
            {
                case Ease.Linear:
                    return t;

                case Ease.InSine:
                    return 1f - Mathf.Cos(t * Mathf.PI * 0.5f);

                case Ease.OutSine:
                    return Mathf.Sin(t * Mathf.PI * 0.5f);

                case Ease.InOutSine:
                    return -(Mathf.Cos(Mathf.PI * t) - 1f) * 0.5f;

                case Ease.InQuad:
                    return t * t;

                case Ease.OutQuad:
                    return 1f - (1f - t) * (1f - t);

                case Ease.InOutQuad:
                    return t < 0.5f ? 2f * t * t : 1f - Mathf.Pow(-2f * t + 2f, 2f) * 0.5f;

                case Ease.InCubic:
                    return t * t * t;

                case Ease.OutCubic:
                    return 1f - Mathf.Pow(1f - t, 3f);

                default:
                    return t;
            }
        }

        internal static IEnumerator AnimationCoroutine(UIAnimator uiAnimator, float duration, Action<float> updateAction)
        {
            // Handle delay
            if (uiAnimator.Delay > 0)
            {
                float delayElapsed = 0f;
                while (delayElapsed < uiAnimator.Delay)
                {
                    delayElapsed += uiAnimator.UseUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
                    yield return null;
                }
            }

            float elapsed = 0f;

            while (elapsed < duration)
            {
                float t = elapsed / duration;
                float easedT;

                if (uiAnimator._customCurve != null)
                {
                    easedT = uiAnimator._customCurve.Evaluate(t);
                }
                else
                {
                    easedT = Evaluate(uiAnimator.Ease, t);
                }

                updateAction(easedT);

                elapsed += uiAnimator.UseUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
                yield return null;
            }

            // Ensure final value
            updateAction(1f);
            uiAnimator.Complete();
        }
    }

    /// <summary>
    /// AnimateVirtual static methods for virtual value animations
    /// </summary>
    public static class AnimateVirtual
    {
        public static float EasedValue(float from, float to, float t, Ease ease)
        {
            float easedT = GeniesUIAnimation.Evaluate(ease, t);
            return Mathf.Lerp(from, to, easedT);
        }

        public static UIAnimator Float(float from, float to, float duration, Action<float> onUpdate, AnimationSettings settings = default)
        {
            // Use manager directly - no need for dummy host
            var manager = Animations.UIAnimationManager.Instance;

            if (settings.IsDefault())
            {
                settings = AnimationSettings.Default;
            }

            return GeniesUIAnimation.CreateAnimation(
                manager,
                duration,
                t => onUpdate(Mathf.Lerp(from, to, t)),
                settings
            );
        }

        public static UIAnimator Vector2(Vector2 from, Vector2 to, float duration, Action<Vector2> onUpdate, AnimationSettings settings = default)
        {
            // Use manager directly - no need for dummy host
            var manager = Animations.UIAnimationManager.Instance;

            if (settings.IsDefault())
            {
                settings = AnimationSettings.Default;
            }

            return GeniesUIAnimation.CreateAnimation(
                manager,
                duration,
                t => onUpdate(UnityEngine.Vector2.Lerp(from, to, t)),
                settings
            );
        }
    }
}


