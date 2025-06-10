using System;
using System.Collections;
using Genies.Components.Accounts;
using TMPro;
using UnityEngine;

public class LaunchSequenceController : MonoBehaviour
{
    public event Action OnFloorSearchComplete;
    public UserLoginController UserLoginController { get { return _userLoginController; } }

    [SerializeField] private TextMeshProUGUI _appVersionLabel;
    [SerializeField] private UserLoginController _userLoginController;
    [SerializeField] private TextMeshProUGUI _floorSearchText;
    [SerializeField] private Transform _floorSearchUiPivot;
    [SerializeField] private Transform _floorSearchImage;
    [SerializeField] private DoubleTapTip _doubleTapTip;

    private GeniesManager _geniesManager;
    private SpatialMeshController _spatialMeshController;
    private InputManager _inputManager;
    private bool _didInitialize = false;

    private float _floorSearchUI_minDisplayTime = 2.5f;
    private float _spatialMesh_minDisplayTime = 3f;

    private bool _isFloorSearchUIActive = false;
    private bool _didShowFloorSearchUI = false;
    private bool _didFindFloor = false;
    private bool _didShowDoubleTapTip = false;

    const float _floorSearchImageYawSpeed = 50f;
    const float _floorSearchImageYawRange = 80f;
    const float _floorSearchImagePitchSpeed = 20f;
    const float _floorSearchImagePitchRange = 20f;

    const string _floorSearchText_hasMeshing = "Please look around your space\nwith the Camera.";
    const string _floorSearchText_hasNoMeshing = "Please move the Camera gently while examining floors and walls from a distance.";

    public void Initialize(string appVersionString,
                           InputManager inputManager,
                           GeniesManager geniesManager,
                           SpatialMeshController spatialMeshController,
                           CameraManager cameraManager)
    {
        // Store references
        _geniesManager = geniesManager;
        _spatialMeshController = spatialMeshController;
        _inputManager = inputManager;

        // Various UI hookups
        _appVersionLabel.text = appVersionString;
        _floorSearchUiPivot.parent = cameraManager.XRCamera.transform; // xr camera's child
        _geniesManager.OnFirstGenieInitialized += HideSpatialMesh_AfterFirstGenieInitialized;
        _doubleTapTip.Initialize(cameraManager, inputManager);
        _floorSearchText.text = SpatialMeshController.IsMeshingSupported ?
                            _floorSearchText_hasMeshing : _floorSearchText_hasNoMeshing;

        // Register callbacks
        _userLoginController.OnLoginUiClosed += HandleLoginCompleted;
        _userLoginController.OnLoginStateAborted += HandleLoginAborted;

        // We only want to call this once, so remove to make sure!
        _spatialMeshController.OnFoundFloor += OnFoundFloor;
        _didInitialize = true;
    }

    private void OnDestroy()
    {
        if(_didInitialize)
        {
            _spatialMeshController.OnFoundFloor -= OnFoundFloor;
            _userLoginController.OnLoginUiClosed -= HandleLoginCompleted;
            _userLoginController.OnLoginStateAborted -= HandleLoginAborted;
            _geniesManager.OnFirstGenieInitialized -= HideSpatialMesh_AfterFirstGenieInitialized;      
        }
    }

    private void Update()
    {
        if (_isFloorSearchUIActive)
        {
            _floorSearchUI_minDisplayTime -= Time.deltaTime;
            AnimateFloorSearchImage();
        }
    }

    private void HandleLoginAborted()
    {
        _geniesManager.RemoveAndReplaceUserGenie();
    }

    private void HandleLoginCompleted()
    {
        if (!_didShowFloorSearchUI)
        {
            ShowFloorSearchUI();
        }
    }

    private void HideSpatialMesh()
    {
        _spatialMeshController.SetSpatialMeshVisibility(false, 4f);
    }

    private IEnumerator HideSpatialMesh_AfterWait()
    {
        // Look at the Genie standing on the Spatial Mesh for a few moments
        yield return new WaitForSeconds(_spatialMesh_minDisplayTime);

        // Keep showing the Spatial Mesh until you've found the floor, also
        while (!_didFindFloor)
        {
            yield return null;
        }

        // Show a tip if you haven't yet
        if (!_didShowDoubleTapTip && _doubleTapTip != null)
        {
            _doubleTapTip.ShowDoubleTapTip();
        }
        _didShowDoubleTapTip = true;

        // Turn off the Spatial Mesh visualizer
        HideSpatialMesh();
    }

    private void HideSpatialMesh_AfterFirstGenieInitialized()
    {
        StartCoroutine(HideSpatialMesh_AfterWait());
    }

    private void OnFoundFloor()
    {
        _didFindFloor = true;
        HideFloorSearchUI_AfterFloorFound();
    }

    private void ShowFloorSearchUI()
    {
        _floorSearchUiPivot.gameObject.SetActive(true);
        _floorSearchText.gameObject.SetActive(true);
        _isFloorSearchUIActive = true;

        if (_didFindFloor)
        {
            HideFloorSearchUI_AfterFloorFound();
        }

        _didShowFloorSearchUI = true;
    }

    private void HideFloorSearchUI()
    {
        if (_isFloorSearchUIActive)
        {
            OnFloorSearchComplete?.Invoke();
        }

        _floorSearchUiPivot.gameObject.SetActive(false);
        _floorSearchText.gameObject.SetActive(false);
        _isFloorSearchUIActive = false;
    }

    private IEnumerator HideFloorSearchUI_AfterWait(float t)
    {
        yield return new WaitForSeconds(t);
        HideFloorSearchUI();
    }

    public void HideFloorSearchUI_AfterFloorFound()
    {
        // Is it already hidden?
        if (!_isFloorSearchUIActive)
        {
            return;
        }

        // Did you show it long enough? Hide it soon.
        if (_floorSearchUI_minDisplayTime > 0)
        {
            StartCoroutine(HideFloorSearchUI_AfterWait(_floorSearchUI_minDisplayTime));
        }
        // Hide it immediately
        else
        {
            HideFloorSearchUI();
        }
    }

    private void AnimateFloorSearchImage()
    {
        float yaw = Mathf.PingPong(Time.time * _floorSearchImageYawSpeed,
                                   _floorSearchImageYawRange);
        yaw -= _floorSearchImageYawRange / 2f;

        // Negative because we want to pitch slgihtly upwards rather than slightly
        // downwards. This gives us a larger surface area to work with; meanwhile, 
        // holding the phone level is sufficient to find the floor so we are good there.
        float pitch = -Mathf.PingPong(Time.time * _floorSearchImagePitchSpeed,
                                      _floorSearchImagePitchRange);

        _floorSearchImage.localRotation = Quaternion.Euler(pitch, yaw, 0);
        _floorSearchUiPivot.localRotation = Quaternion.Euler(pitch * 0.1f, yaw * 0.1f, 0);
    }
}
