using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class DoubleTapTip : MonoBehaviour
{
    [FormerlySerializedAs("circle1")]
    [SerializeField] RawImage _outerRingImage;
    [FormerlySerializedAs("circle2")]
    [SerializeField] RawImage _innerRingImage;
    [FormerlySerializedAs("tapFinger")]
    [SerializeField] RawImage _tapFingerImage;
    [FormerlySerializedAs("label")]
    [SerializeField] TextMeshProUGUI _label;

    private const string MESHING_SUPPORTED_TIP_TEXT = "Double-tap on any flat surface\nto place your Genie.";
    private const string MESHING_NOT_SUPPORTED_TIP_TEXT = "Double-tap the floor\nto teleport your Genie.";
    private const float MAX_SCALE_RING = 3f;
    private const float MIN_SCALE_FINGER = 0.75f;
    private const float SINGLE_TAP_ANIM_TIME = 0.2f;
    private const float RING_ANIM_TIME = 0.75f;
    private const float TIME_BETWEEN_REPLAYS = 2.5f;
    private const float FADE_IN_SPEED = 1f;
    private Color _clearWhite = new Color(1, 1, 1, 0);
    private float _fadeScalar = 0f;
    private float _currTime = 2.5f;
    private bool _isVisible = false;
    private bool _didInitialize = false;

    private CameraManager _cameraManager;
    private InputManager _inputManager;

    public void Initialize(CameraManager cameraManager, InputManager inputManager)
    {
        // InputManager and SpatialMeshController are initialized in
        // AppManager's Awake() function, so the values should be populated
        // by Start() of this class.
        _label.text = SpatialMeshController.IsMeshingSupported ?
                        MESHING_SUPPORTED_TIP_TEXT : MESHING_NOT_SUPPORTED_TIP_TEXT;

        _cameraManager = cameraManager;
        _inputManager = inputManager;

        _inputManager.OnDoubleTap += OnDoubleTappedFloor;
        _cameraManager.OnActiveCameraTypeChanged += OnScreenSpaceStateChanged;
        _didInitialize = true;
    }

    private void OnDestroy()
    {
        if (_didInitialize)
        {
            _inputManager.OnDoubleTap -= OnDoubleTappedFloor;
            _cameraManager.OnActiveCameraTypeChanged -= OnScreenSpaceStateChanged;
        }
    }

    public void ShowDoubleTapTip()
    {
        bool canBeVisible = !_cameraManager.IsScreenspace;
        SetVisible(canBeVisible);

        if (canBeVisible)
        {
            StartCoroutine(AnimateDoubleTap());
        }
    }

    private void OnScreenSpaceStateChanged(ActiveCameraType cameraTypeNew)
    {
        SetVisible(!_cameraManager.IsScreenspace);
    }

    private void SetVisible(bool isVisibleNew)
    {
        _outerRingImage.color = _clearWhite;
        _innerRingImage.color = _clearWhite;
        _tapFingerImage.color = _clearWhite;
        _label.color = _clearWhite;
        _fadeScalar = 0;

        _outerRingImage.gameObject.SetActive(isVisibleNew);
        _innerRingImage.gameObject.SetActive(isVisibleNew);
        _tapFingerImage.gameObject.SetActive(isVisibleNew);
        _label.gameObject.SetActive(isVisibleNew);

        _isVisible = isVisibleNew;
    }

    private void OnDoubleTappedFloor(Vector2 tapPoint)
    {
        Destroy(gameObject);
    }

    private void FixedUpdate()
    {
        if (!_isVisible)
        {
            return;
        }

        // Animate double tap
        if (_currTime < TIME_BETWEEN_REPLAYS)
        {
            _currTime += Time.deltaTime;
        }
        else
        {
            _currTime = 0f;
            StartCoroutine(AnimateDoubleTap());
        }

        // Fade in tooltip initially
        if(_fadeScalar < 1)
        {
            _fadeScalar += Time.deltaTime * FADE_IN_SPEED;
            // Don't exceed 1
            _fadeScalar = Mathf.Clamp01(_fadeScalar);

            // Set color anywhere it's missing
            Color fadeColor = Color.Lerp(_clearWhite, Color.white, _fadeScalar);
            _label.color = fadeColor;
            _tapFingerImage.color = fadeColor;
        }
    }

    private IEnumerator AnimateDoubleTap()
    {
        StartCoroutine(TapFinger());
        StartCoroutine(ScaleRing(_outerRingImage));
        yield return new WaitForSecondsRealtime(SINGLE_TAP_ANIM_TIME);
        StartCoroutine(TapFinger());
        StartCoroutine(ScaleRing(_innerRingImage));
        yield return new WaitForSecondsRealtime(RING_ANIM_TIME);
    }

    private IEnumerator TapFinger()
    {
        Vector3 depressedScale = Vector3.one * MIN_SCALE_FINGER;

        float timer = 0f;
        while (timer <= SINGLE_TAP_ANIM_TIME)
        {
            // Aniamte scale
            _tapFingerImage.transform.localScale = Vector3.Lerp(depressedScale,
                                                          Vector3.one,
                                                          timer / SINGLE_TAP_ANIM_TIME);
            // Keep track of time
            timer += Time.deltaTime;
            yield return null;
        }
        _tapFingerImage.transform.localScale = Vector3.one;
    }

    private IEnumerator ScaleRing(RawImage ringImage)
    {
        float timer = 0f;
        while (timer <= RING_ANIM_TIME)
        {
            // Aniamte scale
            ringImage.transform.localScale = Vector3.Lerp(Vector3.one,
                                                          Vector3.one * MAX_SCALE_RING,
                                                          timer / RING_ANIM_TIME);
            // Fade rings AND manage initial fade in
            ringImage.color = Color.Lerp(Color.white, _clearWhite, (timer / RING_ANIM_TIME) * _fadeScalar);
            timer += Time.deltaTime;
            yield return null;
        }
        ringImage.transform.localScale = Vector3.one * MAX_SCALE_RING;
        ringImage.color = _clearWhite * _fadeScalar;
    }
}
