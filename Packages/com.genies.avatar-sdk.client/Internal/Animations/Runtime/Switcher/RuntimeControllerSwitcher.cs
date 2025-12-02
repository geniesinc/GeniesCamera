using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Genies.Animations.Model;
using Genies.CrashReporting;
using Genies.Refs;
using UnityEngine;

namespace Genies.Animations
{
    /// <summary>
    /// Implementation of IAnimationSwitcher that switches between RuntimeAnimatorControllers to create animation montages.
    /// This class manages the creation and switching of animator override controllers to play animations in sequence.
    /// </summary>
    public class RuntimeControllerSwitcher : IAnimationSwitcher
    {
        private readonly AnimatorSwitcherComponents _components;

        private CancellationTokenSource _cancellationTokenSource;
        private Ref<List<AnimationClip>> _animationClipRefs;
        private Ref<List<RuntimeAnimatorController>> _overrideControllersRefs;

        // This buffer guarantees that our animation will not loop before starting the second one
        private const float AnimationMontageBuffer = .01f;

        /// <summary>
        /// Gets the total duration of the animation montage in seconds.
        /// </summary>
        public float MontageTime { get; private set; }
        
        /// <summary>
        /// Gets the frame rate of the animation clips in the montage.
        /// </summary>
        public float FrameRate { get; private set; }

        /// <summary>
        /// Initializes a new instance of the RuntimeControllerSwitcher class.
        /// </summary>
        /// <param name="components">The animator switcher components containing required animators and controllers.</param>
        public RuntimeControllerSwitcher(AnimatorSwitcherComponents components)
        {
            _components = components;
        }

        /// <inheritdoc cref='IAnimationSwitcher.Init'/>
        /// <summary>
        /// When initializing the method will dispose the current animator controllers along with their animation clips &
        /// generate new ones that come from the caller.
        /// </summary>
        public void Init(Ref<AnimationMontage> montage)
        {
            _overrideControllersRefs.Dispose();

            _animationClipRefs = montage.Item.GenieAnimations;

            var overrideControllers = new List<RuntimeAnimatorController>();
            MontageTime = 0;

            for (var i = 0; i < _animationClipRefs.Item.Count; i++)
            {
                var clip = _animationClipRefs.Item[i];
                if (i == 0)
                {
                    FrameRate = clip.frameRate;
                }

                MontageTime += (clip.length - AnimationMontageBuffer);

                var overrideController = CreateAnimatorController(clip);
                overrideControllers.Add(overrideController);
            }

            _overrideControllersRefs = CreateRef.FromDependentResource(overrideControllers, montage);
        }

        /// <inheritdoc cref='IAnimationSwitcher.Play'/>
        public void Play()
        {
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource = new CancellationTokenSource();

            if (_overrideControllersRefs.IsAlive && _overrideControllersRefs.Item != null && _overrideControllersRefs.Item.Count != 0)
            {
                LoopAnims(_cancellationTokenSource.Token);
            }
            else
            {
                CrashReporter.Log("Override Controllers Refs is empty.");
            }
        }

        /// <summary>
        /// This method will be a long running thread that loops trough the animator controllers.
        /// </summary>
        /// <param name="cancellationToken">Token used to interrupt the loop</param>
        private async void LoopAnims(CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }

            var umaAnimator = _components.UmaAnimator;

            try
            {
                var index = 0;

                while (!cancellationToken.IsCancellationRequested)
                {
                    if (!umaAnimator.runtimeAnimatorController.Equals(_overrideControllersRefs.Item[index]))
                    {
                        umaAnimator.runtimeAnimatorController = _overrideControllersRefs.Item[index];
                    }

                    umaAnimator.PlayInFixedTime("Idle", 0, 0);
                    umaAnimator.Update(0);
                    _components.OnAnimationLoopStarted.Invoke();

                    var seconds = umaAnimator.GetCurrentAnimatorClipInfo(0)[0].clip.length - AnimationMontageBuffer;
                    var milli = (int) (seconds * 1000);

                    await UniTask.Delay(milli, cancellationToken: cancellationToken).SuppressCancellationThrow();

                    if (index >= _overrideControllersRefs.Item.Count - 1)
                    {
                        index = 0;
                    }
                    else
                    {
                        index++;
                    }
                }
            }
            catch (Exception exception)
            {
                CrashReporter.Log(exception.Message);
            }
        }

        /// <inheritdoc cref='IAnimationSwitcher.Stop'/>
        public void Stop()
        {
            _cancellationTokenSource?.Cancel();
        }

        /// <inheritdoc cref='IAnimationSwitcher.Reset'/>
        public void Reset()
        {
            Play();
        }

        /// <summary>
        /// We should assume that the animator controller should only have the same number of clip overrides that match
        /// the original number of clip animations.
        /// </summary>
        /// <param name="newClip"></param>
        private AnimatorOverrideController CreateAnimatorController(AnimationClip newClip)
        {
            var newOverrideController = new AnimatorOverrideController(_components.OverrideAnimatorController);
            var clipOverrides = new AnimationClipOverrides(newOverrideController.overridesCount);

            newOverrideController.GetOverrides(clipOverrides);

            clipOverrides[newOverrideController.animationClips[0].name] = newClip;

            newOverrideController.ApplyOverrides(clipOverrides);

            return newOverrideController;
        }
    }

    /// <summary>
    /// Creates an override collection of animation clips that can be passed
    /// to the <see cref="AnimatorOverrideController"/> via the ApplyOverrides method.
    /// This internal class provides name-based indexing for animation clip overrides.
    /// </summary>
    /// <remarks>
    /// https://docs.unity3d.com/ScriptReference/AnimatorOverrideController.html
    /// </remarks>
    internal class AnimationClipOverrides : List<KeyValuePair<AnimationClip, AnimationClip>>
    {
        /// <summary>
        /// Initializes a new instance of the AnimationClipOverrides class with the specified capacity.
        /// </summary>
        /// <param name="capacity">The initial capacity of the collection.</param>
        public AnimationClipOverrides(int capacity) : base(capacity)
        {
        }

        /// <summary>
        /// Gets or sets an animation clip override by the original clip's name.
        /// This indexer provides convenient access to override clips using string names.
        /// </summary>
        /// <param name="name">The name of the original animation clip to override.</param>
        /// <returns>The replacement animation clip, or null if not found.</returns>
        public AnimationClip this[string name]
        {
            get { return this.Find(x => x.Key.name.Equals(name)).Value; }
            set
            {
                var index = this.FindIndex(x => x.Key.name.Equals(name));
                if (index != -1)
                {
                    this[index] = new KeyValuePair<AnimationClip, AnimationClip>(this[index].Key, value);
                }
            }
        }
    }
}
