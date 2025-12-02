using System.Collections;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Events;
using UnityEngine.Playables;

namespace Genies.Avatars.Behaviors
{
    /// <summary>
    /// Provides playback control for a single animation clip using Unity's Playable API.
    /// This class enables frame-by-frame control, normal playback, and event notifications for animation clips.
    /// </summary>
    public class SingleClipPlayable {
        private readonly byte _defaultAnimationFrameRate = 30;

        private MonoBehaviour _owner;

        /// <summary>
        /// The Animator component that will play the animation clip.
        /// </summary>
        public Animator TargetAnimator;

        /// <summary>
        /// The animation clip being controlled by this playable.
        /// </summary>
        public AnimationClip AnimationClip;

        /// <summary>
        /// The AnimationClipPlayable that handles the actual animation playback.
        /// </summary>
        public AnimationClipPlayable ClipPlayable;

        /// <summary>
        /// The PlayableGraph that manages the playable system for this animation.
        /// </summary>
        public PlayableGraph ClipGraph;

        /// <summary>
        /// Gets the total number of frames in the animation clip.
        /// </summary>
        public int FrameCount { get; }

        private float _frameStep;

        /// <summary>
        /// Event triggered for each frame during frame-by-frame playback. Provides the current frame number.
        /// </summary>
        public UnityAction<int> OnFrame;

        /// <summary>
        /// Event triggered when animation playback completes.
        /// </summary>
        public UnityAction OnComplete;

        private Coroutine _playingCoroutine;

        /// <summary>
        /// Initializes a new instance of the SingleClipPlayable class with the specified parameters.
        /// </summary>
        /// <param name="owner">The MonoBehaviour that owns this playable (used for coroutine management).</param>
        /// <param name="targetAnimator">The Animator component that will play the animation.</param>
        /// <param name="clip">The animation clip to be controlled.</param>
        public SingleClipPlayable(
            MonoBehaviour owner,
            Animator targetAnimator,
            AnimationClip clip) {

            this._owner = owner;
            this.TargetAnimator = targetAnimator;
            this.AnimationClip = clip;

            FrameCount = Mathf.RoundToInt(AnimationClip.length * AnimationClip.frameRate) + 1;
            _frameStep = 1f / (AnimationClip.frameRate == 0 ? _defaultAnimationFrameRate : AnimationClip.frameRate);
            ClipPlayable = AnimationPlayableUtilities.PlayClip(targetAnimator, clip, out ClipGraph);
            ClipGraph.Evaluate();
            ClipPlayable.Pause();
        }

        /// <summary>
        /// Resets the animation to the beginning (frame 0) and pauses playback.
        /// </summary>
        public void Reset() {
            ClipPlayable.SetTime(0);
            ClipPlayable.Pause();
            ClipGraph.Evaluate();
        }

        /// <summary>
        /// Pauses the animation playback and stops any running coroutines.
        /// </summary>
        public void Pause() {
            if (_playingCoroutine != null)
            {
                _owner.StopCoroutine(_playingCoroutine);
            }

            ClipPlayable.Pause();
        }

        /// <summary>
        /// Sets the animation to a specific frame number and pauses playback.
        /// </summary>
        /// <param name="frameNumber">The frame number to jump to (0-based).</param>
        public void SetFrame(int frameNumber) {
            Pause();
            ClipPlayable.SetTime(_frameStep * frameNumber);
        }

        /// <summary>
        /// Starts normal playback of the animation clip. The OnComplete event will be triggered when finished.
        /// </summary>
        public void PlayNormal() {
            _playingCoroutine = _owner.StartCoroutine(PlayingNormal());
        }

        /// <summary>
        /// Starts frame-by-frame playback of the animation clip. The OnFrame event will be triggered for each frame.
        /// </summary>
        public void PlayEveryFrame() {
            _playingCoroutine = _owner.StartCoroutine(PlayingEveryFrame());
        }

        /// <summary>
        /// Gets the current frame number of the animation playback.
        /// </summary>
        /// <returns>The current frame number (0-based).</returns>
        public int CurrentFrameNumber() {
            return Mathf.RoundToInt(((float) ClipPlayable.GetTime() / AnimationClip.length) * FrameCount);
        }

        private IEnumerator PlayingEveryFrame() {
            while (ClipPlayable.GetTime() <= AnimationClip.length) {
                NextFrame();
                yield return new WaitForEndOfFrame();
                OnFrame?.Invoke(CurrentFrameNumber());
                yield return null;
            }

            OnComplete?.Invoke();
        }

        /// <summary>
        /// Advances the animation to the next frame.
        /// </summary>
        public void NextFrame() {
            ClipPlayable.SetTime(ClipPlayable.GetTime() + _frameStep);
        }

        /// <summary>
        /// Moves the animation back to the previous frame.
        /// </summary>
        public void PreviousFrame() {
            ClipPlayable.SetTime(ClipPlayable.GetTime() - _frameStep);
        }

        private IEnumerator PlayingNormal() {
            while (!ClipPlayable.IsDone()) {
                yield return null;
            }

            OnComplete?.Invoke();
        }

        /// <summary>
        /// Destroys the PlayableGraph and ClipPlayable, cleaning up all resources.
        /// This should be called when the SingleClipPlayable is no longer needed to prevent memory leaks.
        /// </summary>
        public void Destroy() {
            ClipGraph.Destroy();
            ClipPlayable.Destroy();
        }
    }
}
