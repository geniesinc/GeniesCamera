using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using VideoCreator;

public class RecordingManager : MonoBehaviour
{
    [SerializeField] BgController _bgController;
    [SerializeField] Button _recordButton;
    [SerializeField] Button[] _recordModeButtons;
    [SerializeField] RenderTexture _renderTexture;
    [SerializeField] AudioSource _audioSource;
    [SerializeField] GameObject _renderTextureViewerRoot;
    [SerializeField] RawImage _renderTextureViewer;
    [SerializeField] RawImage _recordButtonImage;
    [SerializeField] RawImage _fadeInImage;
    [SerializeField] RawImage _blinkingRecordLight;
    [SerializeField] RawImage _recordStopIcon;
    [SerializeField] TMPro.TextMeshProUGUI _videoModeText;
    [SerializeField] TMPro.TextMeshProUGUI _photoModeText;
    [SerializeField] RectTransform _recordModeLabels;

    public const int TARGET_FRAME_RATE_FOR_RECORDING = 30;
    // We are using a RecordingKit plugin that looks for a specific
    // camera in a sad hardcoded search way :<
    const string HARDCODED_CAM_NAME = "RecordingCamera";
    const string HARDCODED_CAM_TAG = "MainCamera";
    const string VIDEO_CODEC = "h264";
    // Use kAudioFormatProperty_AvailableEncodeSampleRates from
    // (<AudioToolbox/AudioFormat.h>) to enumerate available bitrates
    const System.Int32 AUDIO_BITRATE = 48_000; // 44_100 seems common
    const System.Int32 BITS_PER_FRAME = 1_000_000;
    private readonly long START_TIME_OFFSET = 6_000_000;
    const int AUDIO_CHANNEL_COUNT = 1;
    const int AUDIO_LENGTH_SEC = 600; // 10 minute allocation
    const bool DO_LOOP_AUDIO = false;
    const int VIDEO_MODE_UI_POSITION = -260;
    const int PHOTO_MODE_UI_POSITION = 260;

    public bool IsRecording { get; private set; } = false;
    private bool _doRecordAudio = false;
    private string _videoCachePath = "";
    private float _startTime = 0;
    private long _currAudioTimestamp = 0;
    private string _recordingCamNamePrev = "";
    private string _recordingCamTagPrev = "";
    private string _defaultMicName = null;
    private int _videoWidth, _videoHeight = 0;
    // For tinting the UI based on which mode is active
    private Color _niceRed = new Color(212f / 255, 3f / 255, 3f / 255, 1f);
    private Color _niceYellow = new Color(1, 207/255f, 0, 1f);
    private bool _isVideoMode = false;
    // The screenspace recording camera is off except for photo's.
    // The worldspace recording camera IS the worldspace camera.
    // So, one toggles on and off, and the other is always on.
    // This is due to the way we've setup the cameras to avoid double-lutting
    // the background images during recording.
    private bool _recordingCamera_prevEnabledState;
    
    private CameraManager _cameraManager;

    public void Initialize(CameraManager cameraManager)
    {
        _cameraManager = cameraManager;
        _defaultMicName = Microphone.devices[0];
        _audioSource.Stop();
        _videoCachePath = "file://" + Application.temporaryCachePath + "/tmp.mov";

        _recordButton.onClick.AddListener(PressRecordButton);
        for(int i=0; i < _recordModeButtons.Length; i++)
        {
            int index = i;
            _recordModeButtons[i].onClick.AddListener(ToggleRecordMode);
        }

        // Make sure UI is happy
        UpdateRecordModeView();

        // Learn about the mic
        /*int minFreq;
        int maxFreq;
        Microphone.GetDeviceCaps(Microphone.devices[0], out minFreq, out maxFreq);
        Debug.Log($"Microphone0 Stats. Count: {Microphone.devices.Length}, " +
            $"Name: {Microphone.devices[0]}, Min: {minFreq}, Max: {maxFreq}");*/
    }

    private void Update()
    {
        // Only works on device
        if (IsRecording && MediaCreator.IsRecording())
        {
            long time = (long)((Time.time - _startTime) * BITS_PER_FRAME) + START_TIME_OFFSET;

            //Debug.Log("Writing to video..." + time);
            MediaCreator.WriteVideo(_renderTexture, time);
        }
        // Works for editor and device
        if (IsRecording)
        {
            if (_isVideoMode)
            {
                // Blink record light
                Color currColor = _niceRed;
                currColor.a = Time.time % 1;
                _blinkingRecordLight.color = currColor;
            }
            else
            {
                // Don't show it for a photo
                _blinkingRecordLight.color = Color.clear;
            }
        }
    }

    void OnDestroy()
    {
        StopRec();
    }

    public void ToggleRecordMode()
    {
        // Update model
        _isVideoMode = !_isVideoMode;
        //  Update view
        UpdateRecordModeView();
    }

    private void UpdateRecordModeView()
    {
        _videoModeText.color = _isVideoMode ? _niceYellow : Color.white;
        _photoModeText.color = !_isVideoMode ? _niceYellow : Color.white;
        _recordButtonImage.color = _isVideoMode ? _niceRed : Color.white;

        // Move UI
        _recordModeLabels.SetLeft(_isVideoMode ? VIDEO_MODE_UI_POSITION : PHOTO_MODE_UI_POSITION);
    }

    public void PressRecordButton()
    {
        StartCoroutine(AnimateRecordButtonScale());

        if (_isVideoMode) {
            OnUserPressRecordVideoButton();
        }
        else
        {
            OnUserPressTakePhotoButton();
        }
    }

    private void OnUserPressTakePhotoButton()
    {
        StartCoroutine(FadeFromBlackToClear());

        // We need a frame between setting the render texture
        // and asking to save image. Otherwise, it will just save
        // as a black frame :')
        StartCoroutine(TakePhotoPatiently());
    }

    private IEnumerator TakePhotoPatiently()
    {
        SetupRenderCamera();

        // HACK: This is necessary :(
        yield return null;
        yield return null;
        yield return null;

        Debug.Log($"Saving photo.");
        MediaSaver.SaveImage(_renderTexture, "jpeg");

        // HACK: ...still necessary!
        yield return null;
        yield return null;

        CleanupRenderCamera();
    }

    private void SetupRenderCamera()
    {
        InitializeRenderTexture();

        _recordingCamNamePrev = _cameraManager.RecordingCamera.name;
        _recordingCamTagPrev = _cameraManager.RecordingCamera.tag;
        _cameraManager.RecordingCamera.name = HARDCODED_CAM_NAME;
        _cameraManager.RecordingCamera.tag = HARDCODED_CAM_TAG;
        _cameraManager.RecordingCamera.targetTexture = _renderTexture;
        _renderTextureViewer.texture = _renderTexture;
        _renderTextureViewerRoot.SetActive(true);

        if(_renderTextureViewer.gameObject.layer != (int)Layers.IgnoreRaycast)
        {
            Debug.LogWarning("Video Recording Texture layer not set to IgnoreRaycast! This will break the UI >:|");
            _renderTextureViewer.gameObject.layer = (int)Layers.IgnoreRaycast;
        }

        _recordingCamera_prevEnabledState = _cameraManager.RecordingCamera.gameObject.activeInHierarchy;
        _cameraManager.RecordingCamera.gameObject.SetActive(true);

        if (_cameraManager.IsScreenspace)
        {
            _bgController.BgFullScreenCanvas.renderMode = RenderMode.ScreenSpaceCamera;
            _bgController.BgFullScreenCanvas.worldCamera = _cameraManager.RecordingCamera;
        }
    }

    private void CleanupRenderCamera()
    {
        _cameraManager.RecordingCamera.name = _recordingCamNamePrev;
        _cameraManager.RecordingCamera.tag = _recordingCamTagPrev;
        if (_cameraManager.RecordingCamera.targetTexture != null)
        {
            _cameraManager.RecordingCamera.targetTexture.Release();
        }
        _cameraManager.RecordingCamera.targetTexture = null;

        _renderTextureViewerRoot.SetActive(false);

        if (_cameraManager.IsScreenspace)
        {
            _bgController.BgFullScreenCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _cameraManager.RecordingCamera.gameObject.SetActive(_recordingCamera_prevEnabledState);
        }
    }

    private void OnUserPressRecordVideoButton()
    {
        // Check this...
        if (Application.targetFrameRate != TARGET_FRAME_RATE_FOR_RECORDING)
        {
            Debug.LogError($"Your targetFrameRate is {Application.targetFrameRate}. Video recording requires a target frame rate of {TARGET_FRAME_RATE_FOR_RECORDING}. Switching.");
            Application.targetFrameRate = TARGET_FRAME_RATE_FOR_RECORDING;
        }

        // The isRecording variable is currently out of date.
        // So if you're not recording, you're about to be!
        if (!IsRecording)
        {
            StartCoroutine(FadeFromBlackToClear());

            _recordStopIcon.enabled = true;

            // We need a frame between setting the render texture and
            // asking to record. Otherwise we get a black frame at the head
            // of our recording.
            StartCoroutine(TakeVideoPatiently());
        }
        else
        {
            // Will update isRecording
            StopRec();

            _recordStopIcon.enabled = false;

            CleanupRenderCamera();
        }
    }

    private IEnumerator TakeVideoPatiently()
    {
        SetupRenderCamera();

        yield return null;

        // Will update isRecording
        StartRecMovWithAudio();
    }

    private IEnumerator AnimateRecordButtonScale()
    {
        float minScale = 0.8f;
        _recordButtonImage.transform.localScale = Vector3.one * minScale;
        yield return null;

        float scaleTimer = 0;
        float scaleDuration = 0.25f;
        while (scaleTimer < scaleDuration)
        {
            scaleTimer += Time.deltaTime;
            float remappedRatio = Mathf.Lerp(0, 1-minScale, (scaleTimer / scaleDuration));
            _recordButtonImage.transform.localScale = Vector3.one * (minScale + remappedRatio);
            yield return null;
        }

        _recordButtonImage.transform.localScale = Vector3.one;
    }

    private IEnumerator FadeFromBlackToClear()
    {
        // Ensure RawImage is enabled initially
        _fadeInImage.color = Color.black;
        _fadeInImage.enabled = true;
        float fadeTimer = 0f;
        float fadeDuration = 0.5f;

        yield return null;

        // Check if the fade-out process is complete
        while (fadeTimer < fadeDuration)
        {
            fadeTimer += Time.deltaTime;
            _fadeInImage.color = Color.black * (1f - Mathf.Clamp01(fadeTimer / fadeDuration));
            yield return null;
        }

        // Disable the RawImage component after fading out
        _fadeInImage.enabled = false;
    }

    private void InitializeRenderTexture()
    {
        if (Screen.width > Screen.height)
        {
            _videoWidth = 1920;
            _videoHeight = Mathf.RoundToInt(1920 * (Screen.height / (float)Screen.width));
        }
        else
        {
            _videoHeight = 1920;
            _videoWidth = Mathf.RoundToInt(1920 * (Screen.width / (float)Screen.height));
        }

        if (_renderTexture.width != _videoWidth || _renderTexture.height != _videoHeight)
        {
            if (_cameraManager.RecordingCamera.targetTexture != null)
            {
                _cameraManager.RecordingCamera.targetTexture.Release();
            }

            _renderTexture = new RenderTexture(_videoWidth, _videoHeight, 24);
            _renderTexture.name = "RecordingOutputRT";
            _renderTexture.Create();
        }
    }

    private void OnAudioFilterRead(float[] data, int channels)
    {
        WriteAudio(data, channels);

        for (int i = 0; i < data.Length; i++)
        {
            data[i] = 0;
        }
    }

    private void WriteAudio(float[] data, int channels)
    {
        if (!IsRecording || !_doRecordAudio || !MediaCreator.IsRecording()) return;

        long time = (_currAudioTimestamp * BITS_PER_FRAME / AUDIO_BITRATE) + START_TIME_OFFSET;
        //Debug.Log($"write audio: {time}");

        MediaCreator.WriteAudio(data, time);

        _currAudioTimestamp += data.Length;
    }

    private void StartRecMovWithAudio()
    {
        Debug.Log("StartRecMovWithAudio");

        // Don't double-start
        if (IsRecording || MediaCreator.IsRecording())
        {
            return;
        }

        _audioSource.clip = Microphone.Start(_defaultMicName, loop: DO_LOOP_AUDIO, AUDIO_LENGTH_SEC, AUDIO_BITRATE);
        _audioSource.loop = DO_LOOP_AUDIO;

        // Get the position in samples of the recording.
        // If you pass a null or empty string for the device name then the default microphone will be used.
        // You can get a list of available microphone devices from the devices property.
        // You can use this to control latency.If you want a 30ms latency, poll GetPosition() until 30ms(in samples)
        // has gone and then start the audio.
        while (Microphone.GetPosition(_defaultMicName) < 0) { }

        MediaCreator.InitAsMovWithAudio(_videoCachePath, VIDEO_CODEC, _renderTexture.width, _renderTexture.height, AUDIO_CHANNEL_COUNT, AUDIO_BITRATE);
        MediaCreator.Start(START_TIME_OFFSET);

        _startTime = Time.time;

        _doRecordAudio = true;
        IsRecording = true;
        _currAudioTimestamp = 0;

        StartCoroutine(PlayAudioSourceAfterDelay());
    }

    // Without this, the audio was super garbled, it sounded like
    // a stutter on every frame, with noise in between. Very choppy.
    // https://forum.unity.com/threads/microphone-start-works-only-in-start-routine.907172/
    private IEnumerator PlayAudioSourceAfterDelay()
    {
        yield return new WaitForSeconds(0.1f);
        _audioSource.Play();
    }

    private void StartRecMovWithNoAudio()
    {
        // Don't double-start
        if (IsRecording || MediaCreator.IsRecording())
        {
            return;
        }

        MediaCreator.InitAsMovWithNoAudio(_videoCachePath, VIDEO_CODEC, _renderTexture.width, _renderTexture.height);
        MediaCreator.Start(START_TIME_OFFSET);

        _startTime = Time.time;

        _doRecordAudio = false;
        IsRecording = true;
    }

    private void StopRec()
    {
        if (!IsRecording || !_doRecordAudio || !MediaCreator.IsRecording())
        {
            return;
        }

        _audioSource.Stop();
        Microphone.End(Microphone.devices[0]);

        MediaCreator.FinishSync();

        Debug.Log($"Saving video: {_videoCachePath}");
        MediaSaver.SaveVideo(_videoCachePath);

        IsRecording = false;
    }
}
