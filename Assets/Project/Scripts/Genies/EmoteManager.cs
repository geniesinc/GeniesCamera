using System;
using System.Collections;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;


// This class manages Emote animation playbback.
public class EmoteManager : MonoBehaviour
{
    public event Action<string> OnCurrentEmoteStopped;

    const float BLEND_DURATION = 0.25f; // how long (seconds) to blend between anims
    const int ANIMATION_CONTROLLER_IDX = 0; // playble graph input 0
    const int EMOTE_A_IDX = 1; // playable graph input 1
    const int EMOTE_B_IDX = 2; // playable graph input 2
    // The source output index is always 0 for AnimationClips, as they are the only output.
    const int DEFAULT_OUTPUT_IDX = 0;

    private GenieController _targetGenie;

    private PlayableGraph _playableGraph;
    private bool _didInitPlayableGraph = false;
    private string _currEmoteId;
    private Coroutine _currBlendCoroutine;
    private Coroutine _blendBtwnEmotesCoroutine;

    public void Initialize(GenieController targetGenie)
    {
        _targetGenie = targetGenie;
    }

    private void OnDestroy()
    {
        // Stop getting warning messages about this!
        if (_playableGraph.IsValid())
        {
            _playableGraph.Destroy();
        }
    }

    public void LegacyWave()
    {
        PlayEmoteAnimClip(_targetGenie.LegacyWaveAnim);
    }

    public void LegacyVogue()
    {
        PlayEmoteAnimClip(_targetGenie.LegacyVogueAnim);
    }

    public void LegacyPeace()
    {
        PlayEmoteAnimClip(_targetGenie.LegacyPeaceAnim);
    }

    private void PlayEmoteAnimClip(AnimationClip clip)
    {
        // Initialize the playable graph if it hasn't been done yet
        if (!_didInitPlayableGraph)
        {
            InitializePlayableGraph();
        }

        // Stop any existing transition between emotes
        if (_currBlendCoroutine != null)
        {
            PauseEmote();
            _currBlendCoroutine = StartCoroutine(BlendFromEmoteToEmoteToAnimator(clip));
        }
        else
        {
            // Blend IN to the PlayableGraph (AnimationController -> PlayableGraph)
            _currBlendCoroutine = StartCoroutine(BlendFromAnimatorToEmoteToAnimator(clip));
        }
        _currEmoteId = clip.name;
    }

    private IEnumerator BlendFromEmoteToEmoteToAnimator(AnimationClip newEmoteClip)
    {
        // Get the mixer so we can jack this emote into it
        var currAnimationLayerMixer = (AnimationLayerMixerPlayable)_playableGraph.GetRootPlayable(0);

        // Figure out which Emote Slot has less influence, and use that one for
        // this new emote.
        float weightA = currAnimationLayerMixer.GetInputWeight(EMOTE_A_IDX);
        float weightB = currAnimationLayerMixer.GetInputWeight(EMOTE_B_IDX);
        int newEmoteIndex = weightA > weightB ? EMOTE_B_IDX : EMOTE_A_IDX;
        int otherEmoteIndex = newEmoteIndex == EMOTE_A_IDX ? EMOTE_B_IDX : EMOTE_A_IDX;

        // Disconnect the new emote slot if it's already connected
        if (currAnimationLayerMixer.GetInput(newEmoteIndex).IsValid())
        {
            currAnimationLayerMixer.DisconnectInput(newEmoteIndex);
        }

        // Create a new playable based on the clip to hook into the slot
        var newEmotePlayable = AnimationClipPlayable.Create(_playableGraph, newEmoteClip);

        // Ensure the new animation starts playing immediately from
        // frame 0 with appropriate settings
        newEmotePlayable.SetApplyFootIK(true); // don't sink into the ground, rather bend your knees!
        newEmotePlayable.SetTime(0);
        newEmotePlayable.Play();

        // Connect input and start with 0 weight
        currAnimationLayerMixer.ConnectInput(newEmoteIndex,
                                             newEmotePlayable,
                                             DEFAULT_OUTPUT_IDX);
        currAnimationLayerMixer.SetInputWeight(newEmoteIndex, 0f);

        float elapsedTime = 0f;
        while (elapsedTime < BLEND_DURATION)
        {
            // Slide TOWARDS the new emote from zero
            currAnimationLayerMixer.SetInputWeight(newEmoteIndex, elapsedTime / BLEND_DURATION);
            // Slide AWAY FROM the old emote from one
            currAnimationLayerMixer.SetInputWeight(otherEmoteIndex, 1 - (elapsedTime / BLEND_DURATION));

            elapsedTime += Time.deltaTime;
            yield return null;
        }
        // Final frame
        currAnimationLayerMixer.SetInputWeight(newEmoteIndex, 1);
        // Slide AWAY FROM the old emote from one
        currAnimationLayerMixer.SetInputWeight(otherEmoteIndex, 0);

        _blendBtwnEmotesCoroutine = StartCoroutine(WaitUntilEmotePlayedAndBlendBack(newEmotePlayable));
    }

    private void InitializePlayableGraph()
    {
        if(_targetGenie == null || _targetGenie.Animator == null)
        {
            Debug.LogError("EmoteManager requires a GenieController with an Animator set. App State flow issue?");
            return;
        }

        // Create the playable graph we use to make emotes
        _playableGraph = PlayableGraph.Create();
        _playableGraph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);

        // Create a LayerMixer to blend 2 emote and 1 animation controller (3 total) inputs
        var animLayerMixer = AnimationLayerMixerPlayable.Create(_playableGraph, 3);
        // Connect the mixer to the AnimationPlayableOutput, a type of
        // ephemeral output used to hijack and play animations on an Animator.
        var playableOutput = AnimationPlayableOutput.Create(_playableGraph, "Animation", _targetGenie.Animator);
        playableOutput.SetSourcePlayable(animLayerMixer);

        // Get the current time of the animation from the Animator
        AnimatorStateInfo currentStateInfo = _targetGenie.Animator.GetCurrentAnimatorStateInfo(0);
        float animatorTime = currentStateInfo.normalizedTime % 1f; ; // 0 to 1 range (normalized time)
        int currentStateHash = currentStateInfo.shortNameHash;

        // Create a placeholder for the Animator motion layer
        var animatorControllerPlayable = AnimatorControllerPlayable.Create(_playableGraph,
                                                                           _targetGenie.Animator.runtimeAnimatorController);
        // Set the time and animator state in the PlayableGraph (AnimatorControllerPlayable)
        animatorControllerPlayable.SetTime(animatorTime * currentStateInfo.length); // Multiply by clip length to get absolute time
        animatorControllerPlayable.Play(currentStateHash, 0, currentStateInfo.normalizedTime);
        animatorControllerPlayable.SetSpeed(_targetGenie.Animator.speed);

        animLayerMixer.ConnectInput(ANIMATION_CONTROLLER_IDX, animatorControllerPlayable, DEFAULT_OUTPUT_IDX);
        // Start with 100% weight (AnimationController is active first)
        animLayerMixer.SetInputWeight(ANIMATION_CONTROLLER_IDX, 1f);

        // Play the graph
        _playableGraph.Play();

        _didInitPlayableGraph = true;

        // Wuddup, user?
        LegacyPeace();
    }

    private IEnumerator BlendFromAnimatorToEmoteToAnimator(AnimationClip emoteClip)
    {
        // Set root transform
        _targetGenie.Animator.applyRootMotion = true;

        // Playable for the emote
        var emoteClipPlayable = AnimationClipPlayable.Create(_playableGraph, emoteClip);
        // Get the layer mixer
        var animLayerMixer = (AnimationLayerMixerPlayable)_playableGraph.GetRootPlayable(0);
        // Ensure we're not connecting to an occupied slot
        if (animLayerMixer.GetInput(EMOTE_A_IDX).IsValid())
        {
            animLayerMixer.DisconnectInput(EMOTE_A_IDX);
        }
        // Connect to input 1
        animLayerMixer.ConnectInput(inputIndex: EMOTE_A_IDX, emoteClipPlayable, sourceOutputIndex: DEFAULT_OUTPUT_IDX);
        // Start with 0 weight (Animator is active first)
        animLayerMixer.SetInputWeight(inputIndex: EMOTE_A_IDX, weight: 0f);
        // Ensure the new animation starts playing immediately from
        // frame 0 with appropriate settings
        emoteClipPlayable.SetApplyFootIK(true); // don't sink into the ground, rather bend your knees!
        emoteClipPlayable.SetTime(0f);
        emoteClipPlayable.Play();

        // Force refresh
        if (!_playableGraph.IsPlaying())
        {
            Debug.LogWarning("PlayableGraph was not playing. Restarting...");
            _playableGraph.Play();
        }
        _playableGraph.Evaluate();

        float elapsedTime = 0f;
        while (elapsedTime < BLEND_DURATION)
        {
            animLayerMixer.SetInputWeight(EMOTE_A_IDX, elapsedTime / BLEND_DURATION);
            // The Genie has user input to consider. During Emoting, we discard the
            // user input over the spine, so let's ease into that as well.
            _targetGenie.EmoteBlendWeight = elapsedTime / BLEND_DURATION;

            elapsedTime += Time.deltaTime;
            yield return null;
        }
        // Ensure final blend values
        animLayerMixer.SetInputWeight(EMOTE_A_IDX, 1);
        _targetGenie.EmoteBlendWeight = 1;

        _blendBtwnEmotesCoroutine = StartCoroutine(WaitUntilEmotePlayedAndBlendBack(emoteClipPlayable));
    }

    private IEnumerator WaitUntilEmotePlayedAndBlendBack(AnimationClipPlayable emoteClipPlayable)
    {
        // Wait until emote is done playing
        bool isPlayingEmote = true;
        while (isPlayingEmote)
        {
            isPlayingEmote = emoteClipPlayable.GetTime() <=
                             emoteClipPlayable.GetAnimationClip().length;
            yield return null;
        }

        _blendBtwnEmotesCoroutine = StartCoroutine(BlendBackToAnimationController());
    }

    private IEnumerator BlendBackToAnimationController()
    {
        // Get the layer mixer
        var animLayerMixer = (AnimationLayerMixerPlayable)_playableGraph.GetRootPlayable(0);

        // Transition back to the Animation Controller
        float elapsedTime = 0f;
        while (elapsedTime < BLEND_DURATION)
        {
            animLayerMixer.SetInputWeight(EMOTE_A_IDX, 1 - (elapsedTime / BLEND_DURATION));
            // The Genie has user input to consider. During Emoting, we discard the
            // user input over the spine, so let's ease into that as well.
            _targetGenie.EmoteBlendWeight = 1 - (elapsedTime / BLEND_DURATION);

            elapsedTime += Time.deltaTime;
            yield return null;
        }
        // Ensure final blend values
        animLayerMixer.SetInputWeight(EMOTE_A_IDX, 0);
        _targetGenie.EmoteBlendWeight = 0;

        // Cleanup when transitioning back to Animator
        HardStopEmote();
    }

    private void PauseEmote()
    {
        // Stop the emote blender if it's running
        if (_blendBtwnEmotesCoroutine != null)
        {
            StopCoroutine(_blendBtwnEmotesCoroutine);
            _blendBtwnEmotesCoroutine = null;
        }
        if (_currBlendCoroutine != null)
        {
            StopCoroutine(_currBlendCoroutine);
            _currBlendCoroutine = null;
        }

        // Reset root transform
        _targetGenie.PropagateRootMotion();
        OnCurrentEmoteStopped?.Invoke(_currEmoteId);
        _currEmoteId = "";
    }

    public void SoftStopEmote()
    {
        // FREEZE! (but don't reset the blend)
        PauseEmote();

        // Get the current animation clip playable object        
        /*var animLayerMixer = (AnimationLayerMixerPlayable)playableGraph.GetRootPlayable(0);
        int activeEmoteIndex = animLayerMixer.GetInputWeight(emoteIndexA) >
                               animLayerMixer.GetInputWeight(emoteIndexB) 
                                    ? emoteIndexA 
                                    : emoteIndexB;
        var currPlayable = (AnimationClipPlayable)animLayerMixer.GetInput(activeEmoteIndex);*/

        // Wind down
        _blendBtwnEmotesCoroutine = StartCoroutine(BlendBackToAnimationController());
    }

    private void HardStopEmote()
    {
        // Stop the emote blender if it's running
        if (_blendBtwnEmotesCoroutine != null)
        {
            StopCoroutine(_blendBtwnEmotesCoroutine);
            _blendBtwnEmotesCoroutine = null;
        }
        if (_currBlendCoroutine != null)
        {
            StopCoroutine(_currBlendCoroutine);
            _currBlendCoroutine = null;
        }

        // Reset the blend of emotes (if it exists)
        var currLayerMixer = (AnimationLayerMixerPlayable)_playableGraph.GetRootPlayable(0);
        currLayerMixer.SetInputWeight(EMOTE_A_IDX, 0);
        currLayerMixer.SetInputWeight(EMOTE_B_IDX, 0);

        // Reset root transform
        _targetGenie.PropagateRootMotion();
        _targetGenie.Animator.applyRootMotion = false;

        // Tell anyone who cares
        OnCurrentEmoteStopped?.Invoke(_currEmoteId);
        _currEmoteId = "";
    }

}
