using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace Genies.Sdk.Samples.AvatarStarter
{
    public class GeniesAnimationPlayer : MonoBehaviour
    {
        public event Action<string> OnCurrentAnimStopped;
        private string _currentAnimId;

        private Animator _animator;
        private GeniesAvatarController _avatarController;

        private PlayableGraph _playableGraph;
        private bool _playableGraphInitialized;

        private Coroutine _currBlendCoroutine;
        private Coroutine _blendBtwnAnimClipsCoroutine;

        private const float BlendDuration = 0.25f; // in seconds
        private const int AnimationControllerIndex = 0;
        private const int AnimIndexA = 1;
        private const int AnimIndexB = 2;
        // The source output index is always 0 for AnimationClips, as they are the only output.
        private const int DefaultOutputIndex = 0;

        // Character Controller will set locked state during running and jumping
        public bool Locked { get; private set; }
        public void SetLocked(bool locked)
        {
            Locked = locked;
        }
        // Character Controller will set locked state during running and jumping
        private bool _lockOverride;
        public void SetLockOverride(bool lockOverride)
        {
            _lockOverride = lockOverride;
        }
        private void Start()
        {
            _avatarController = GetComponent<GeniesAvatarController>();
            InitializePlayableGraph();
        }

        private void InitializePlayableGraph()
        {
            _animator = GetComponentInChildren<Animator>();

            // Cannot null-check the playable graph, so we do this instead.
            // Cannot put the init in start, because the Genie doesn't exist
            // at that time, and we need her animator component.
            if(_playableGraphInitialized)
            {
                _playableGraph.Destroy();
            }

            // Create the playable graph we use to make anims
            _playableGraph = PlayableGraph.Create();
            _playableGraph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);

            // Create a LayerMixer to blend 2 anim and 1 animation controller (3 total) inputs
            var animLayerMixer = AnimationLayerMixerPlayable.Create(_playableGraph, 3);
            // Connect the mixer to the AnimationPlayableOutput, a type of
            // ephemeral output used to hijack and play animations on an Animator.
            var playableOutput = AnimationPlayableOutput.Create(_playableGraph, "Animation", _animator);
            playableOutput.SetSourcePlayable(animLayerMixer);

            // Get the current time of the animation from the Animator
            AnimatorStateInfo currentStateInfo = _animator.GetCurrentAnimatorStateInfo(0);
            float animatorTime = currentStateInfo.normalizedTime % 1f;; // 0 to 1 range (normalized time)
            int currentStateHash = currentStateInfo.shortNameHash;

            // Create a placeholder for the Animator motion layer
            var animatorControllerPlayable = AnimatorControllerPlayable.Create(_playableGraph,
                                                                               _animator.runtimeAnimatorController);
            // Set the time and animator state in the PlayableGraph (AnimatorControllerPlayable)
            animatorControllerPlayable.SetTime(animatorTime * currentStateInfo.length); // Multiply by clip length to get absolute time
            animatorControllerPlayable.Play(currentStateHash, 0, currentStateInfo.normalizedTime);
            animatorControllerPlayable.SetSpeed(_animator.speed);

            animLayerMixer.ConnectInput(AnimationControllerIndex, animatorControllerPlayable, DefaultOutputIndex);
            // Start with 100% weight (AnimationController is active first)
            animLayerMixer.SetInputWeight(AnimationControllerIndex, 1f);

            _playableGraphInitialized = true;
        }

        public void PlayAnimClip(AnimationClip clip)
        {
            if (Locked && !_lockOverride)
            {
                return;
            }
            // Stop any existing transition between anims
            if (_currBlendCoroutine != null)
            {
                PauseAnim();
                _currBlendCoroutine = StartCoroutine(BlendFromAnimClipToAnimClipToAnimator(clip));
            }
            else
            {
                // Blend IN to the PlayableGraph (AnimationController -> PlayableGraph)
                _currBlendCoroutine = StartCoroutine(BlendFromAnimatorToAnimClipToAnimator(clip));
            }
            _currentAnimId = clip.name;
        }

        private IEnumerator BlendFromAnimClipToAnimClipToAnimator(AnimationClip newAnimClip)
        {
            // Get the mixer so we can inject this animation into it
            var currAnimationLayerMixer = (AnimationLayerMixerPlayable)_playableGraph.GetRootPlayable(0);

            // Figure out which Anim Slot has less influence, and use that one for
            // this new anim.
            float weightA = currAnimationLayerMixer.GetInputWeight(AnimIndexA);
            float weightB = currAnimationLayerMixer.GetInputWeight(AnimIndexB);
            int newAnimIndex = weightA > weightB ? AnimIndexB : AnimIndexA;
            int otherAnimIndex = newAnimIndex == AnimIndexA ? AnimIndexB : AnimIndexA;

            // Disconnect the new anim slot if it's already connected
            if(currAnimationLayerMixer.GetInput(newAnimIndex).IsValid())
            {
                currAnimationLayerMixer.DisconnectInput(newAnimIndex);
            }

            // Create a new playable based on the clip to hook into the slot
            var newAnimPlayable = AnimationClipPlayable.Create(_playableGraph, newAnimClip);

            // Ensure the new animation starts playing immediately from
            // frame 0 with appropriate settings
            newAnimPlayable.SetApplyFootIK(true); // don't sink into the ground, rather bend your knees!
            newAnimPlayable.SetTime(0);
            newAnimPlayable.Play();

            // Connect input and start with 0 weight
            currAnimationLayerMixer.ConnectInput(newAnimIndex,
                                                 newAnimPlayable,
                                                 DefaultOutputIndex);
            currAnimationLayerMixer.SetInputWeight(newAnimIndex, 0f);

            float elapsedTime = 0f;
            while (elapsedTime < BlendDuration)
            {
                // Slide TOWARDS the new anim from zero
                currAnimationLayerMixer.SetInputWeight(newAnimIndex, elapsedTime / BlendDuration);
                // Slide AWAY FROM the old anim from one
                currAnimationLayerMixer.SetInputWeight(otherAnimIndex, 1 - (elapsedTime / BlendDuration));

                elapsedTime += Time.deltaTime;
                yield return null;
            }
            // Final frame
            currAnimationLayerMixer.SetInputWeight(newAnimIndex, 1);
            // Slide AWAY FROM the old anim from one
            currAnimationLayerMixer.SetInputWeight(otherAnimIndex, 0);

            _blendBtwnAnimClipsCoroutine = StartCoroutine(WaitUntilAnimPlayedAndBlendBack(newAnimPlayable));
        }


        // avatarcontroller anim weight
        private IEnumerator BlendFromAnimatorToAnimClipToAnimator(AnimationClip animClip)
        {
            // Set root transform
            _animator.applyRootMotion = true;

            // Playable for the anim
            var animClipPlayable = AnimationClipPlayable.Create(_playableGraph, animClip);
            // Get the layer mixer
            var animLayerMixer = (AnimationLayerMixerPlayable)_playableGraph.GetRootPlayable(0);
            // Ensure we're not connecting to an occupied slot
            if (animLayerMixer.GetInput(AnimIndexA).IsValid())
            {
                animLayerMixer.DisconnectInput(AnimIndexA);
            }
            // Connect to input 1
            animLayerMixer.ConnectInput(inputIndex: AnimIndexA, animClipPlayable, sourceOutputIndex: DefaultOutputIndex);
            // Start with 0 weight (Animator is active first)
            animLayerMixer.SetInputWeight(inputIndex:AnimIndexA, weight:0f);
            // Ensure the new animation starts playing immediately from
            // frame 0 with appropriate settings
            animClipPlayable.SetApplyFootIK(true); // don't sink into the ground, rather bend your knees!
            animClipPlayable.SetTime(0f);
            animClipPlayable.Play();

            // Force refresh
            if (!_playableGraph.IsPlaying())
            {
                Debug.LogWarning("PlayableGraph was not playing. Restarting...");
                _playableGraph.Play();
            }
            _playableGraph.Evaluate();

            float elapsedTime = 0f;
            while (elapsedTime < BlendDuration)
            {
                animLayerMixer.SetInputWeight(AnimIndexA, elapsedTime / BlendDuration);

                elapsedTime += Time.deltaTime;
                yield return null;
            }
            // Ensure final blend values
            animLayerMixer.SetInputWeight(AnimIndexA, 1);

            _blendBtwnAnimClipsCoroutine = StartCoroutine(WaitUntilAnimPlayedAndBlendBack(animClipPlayable));
        }

        private IEnumerator WaitUntilAnimPlayedAndBlendBack(AnimationClipPlayable animClipPlayable)
        {
            // Wait until anim is done playing
            bool isPlayingAnim = true;
            while(isPlayingAnim)
            {
                isPlayingAnim = animClipPlayable.GetTime() <=
                                 animClipPlayable.GetAnimationClip().length;
                yield return null;
            }
            _blendBtwnAnimClipsCoroutine = StartCoroutine(BlendBackToAnimationController());
        }

        private IEnumerator BlendBackToAnimationController()
        {
            // Get the layer mixer
            var animLayerMixer = (AnimationLayerMixerPlayable)_playableGraph.GetRootPlayable(0);

            // Transition back to the Animation Controller
            float elapsedTime = 0f;
            while (elapsedTime < BlendDuration)
            {
                animLayerMixer.SetInputWeight(AnimIndexA, 1 - (elapsedTime / BlendDuration));

                elapsedTime += Time.deltaTime;
                yield return null;
            }

            // Ensure final blend values
            animLayerMixer.SetInputWeight(AnimIndexA, 0);

            // Cleanup when transitioning back to Animator
            HardStopAnim();
        }

        private void PauseAnim()
        {
            // Stop the anim blender if it's running
            if(_blendBtwnAnimClipsCoroutine != null)
            {
                StopCoroutine(_blendBtwnAnimClipsCoroutine);
                _blendBtwnAnimClipsCoroutine = null;
            }
            if(_currBlendCoroutine != null)
            {
                StopCoroutine(_currBlendCoroutine);
                _currBlendCoroutine = null;
            }

            // Reset root transform
            _avatarController.PropagateRootMotion();
            OnCurrentAnimStopped?.Invoke(_currentAnimId);
            _currentAnimId = "";
        }

        public void SoftStopAnim()
        {
            // FREEZE! (but don't reset the blend)
            PauseAnim();

            // Wind down
            _blendBtwnAnimClipsCoroutine = StartCoroutine(BlendBackToAnimationController());
        }

        public void HardStopAnim()
        {
            // Stop the Anim blender if it's running
            if(_blendBtwnAnimClipsCoroutine != null)
            {
                StopCoroutine(_blendBtwnAnimClipsCoroutine);
                _blendBtwnAnimClipsCoroutine = null;
            }
            if(_currBlendCoroutine != null)
            {
                StopCoroutine(_currBlendCoroutine);
                _currBlendCoroutine = null;
            }

            if (_playableGraphInitialized)
            {
                // Reset the blend of Anims (if it exists)
                var currLayerMixer = (AnimationLayerMixerPlayable)_playableGraph.GetRootPlayable(0);
                currLayerMixer.SetInputWeight(AnimIndexA, 0);
                currLayerMixer.SetInputWeight(AnimIndexB, 0);

                // Reset root transform
                _avatarController.PropagateRootMotion();
                _animator.applyRootMotion = false;

                // Tell anyone who cares
                OnCurrentAnimStopped?.Invoke(_currentAnimId);
                _currentAnimId = "";
            }
        }

        private void OnDestroy()
        {
            if(_playableGraphInitialized)
            {
                _playableGraph.Destroy();
            }
        }
    }
}
