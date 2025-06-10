using System;
using System.Collections;
using System.Collections.Generic;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class SpatialMeshController : MonoBehaviour
{
    public event Action OnFoundFloor;

    public static bool IsMeshingSupported { get; private set; }
    [SerializeField] private ARPlaneManager _arPlaneManager;
    [SerializeField] private XROrigin _xrOrigin;
    [SerializeField] private GameObject _arPlanePrefab;
    // We need an infinite floor plane so that no matter where the sad lidarless reviewer
    // double-taps the screen, it will resolve and the genie will translate there.
    [SerializeField] private Transform _floorPlanePrefab;
    // Spatial Mesh Material to show or hide when moving Genie
    [SerializeField] private Material _spatialMeshMaterial;
    [SerializeField] private Material _genieShadowMaterial;
    // For raycasting against spatial mesh specifically
    [SerializeField] private ARRaycastManager _arRaycastManager;

    // Used to get a default floor height when placing Genies by GeniesManager
    public Vector3 InitialFoundFloorPoint { get; private set; } = Vector3.zero;
    

    public float CurrentShadowValue
    {
        get { return _genieShadowMaterial.GetFloat(MULTIPLIER_PROPERTY_NAME); }
    }
    public float MaxShadowValue { get { return MULTIPLIER_MAX_VALUE; } }

    private const float SPATIAL_MESH_OPACITY_MAX = 1f;
    private const string OPACITY_PROPERTY_NAME = "_Opacity";
    private const string MULTIPLIER_PROPERTY_NAME = "MultiplyStrength";
    private const float MULTIPLIER_MAX_VALUE = 1.25f;


    // Tracks status of finding the floor
    private bool _isLookingForFloor = true;

    private bool _isSpatialMeshVisible;
    private InputManager _inputManager;
    private CameraManager _cameraManager;
    private bool _didInitialize = false;

    public void Initialize(XROrigin xrOrigin, InputManager inputManager, CameraManager cameraManager)
    {
        _xrOrigin = xrOrigin;
        _cameraManager = cameraManager;
        _inputManager = inputManager;

        // Check if spatial meshing is supported
        var activeLoader = LoaderUtility.GetActiveLoader();
        IsMeshingSupported = activeLoader && activeLoader.GetLoadedSubsystem<XRMeshSubsystem>() != null;

        Debug.Log("Supports Spatial Meshing (has Lidar)?: " + IsMeshingSupported);

        if (!IsMeshingSupported)
        {
            _arPlaneManager.planePrefab = _arPlanePrefab;
            _arPlaneManager.planesChanged += OnPlanesChanged;
            _floorPlanePrefab = Instantiate(_floorPlanePrefab, Vector3.up * 100, Quaternion.identity);
        }

        _spatialMeshMaterial.SetFloat(OPACITY_PROPERTY_NAME, SPATIAL_MESH_OPACITY_MAX);

        _inputManager.OnTouchUp += HandleStopTouching;
        _inputManager.OnScale += HandleTwoFingerScale;
        _inputManager.OnTwoFingerDrag += HandleTwoFingerDrag;
        _inputManager.OnThreeFingerDrag += HandleThreeFingerDrag;

        _didInitialize = true;
    }

    private void Update()
    {
        // hunt for the floor
        if (_isLookingForFloor)
        {
            // Put Genie in the cutest place on the floor
            Vector2 screenPoint = CameraManager.GetIdealScreenPointForGeniePlacement();
            Vector3? floorPoint = GetFloorAtScreenPoint(screenPoint);

            if (floorPoint != null)
            {
                OnFoundFloor?.Invoke();
                InitialFoundFloorPoint = floorPoint.Value;
                _isLookingForFloor = false;
            }
        }
   
    }

    private void OnDestroy()
    {
        if (_didInitialize)
        {
            _inputManager.OnTouchUp -= HandleStopTouching;
            _inputManager.OnScale -= HandleTwoFingerScale;
            _inputManager.OnTwoFingerDrag -= HandleTwoFingerDrag;
            _inputManager.OnThreeFingerDrag -= HandleThreeFingerDrag;

            if (!IsMeshingSupported)
            {
                _arPlaneManager.planesChanged -= OnPlanesChanged;
            }
        }
    }

    public void SetTrackablesVisibility(bool isVisible)
    {
        _xrOrigin.TrackablesParent.gameObject.SetActive(isVisible);
    }

    // What floor surface is the user touching with their finger on screen?
    public Vector3? GetFloorAtScreenPoint(Vector2 touchPos)
    {
        Ray worldRay = _cameraManager.ActiveCamera.ScreenPointToRay(touchPos);
        RaycastHit hit;
        if (Physics.Raycast(worldRay,
                    out hit,
                    100f,
                    1 << (int)Layers.SpatialMesh) &&
            hit.normal.CanPlaceOnSurfaceWithNormal()
            )
        {
            return hit.point;
        }
        return null;
    }

    private void OnPlanesChanged(ARPlanesChangedEventArgs obj)
    {
        //Debug.Log($"OnPlanesChanged: {obj.added.Count}, {obj.updated.Count}, {obj.removed.Count}");
        CheckForLowestFloor(obj.added);
        CheckForLowestFloor(obj.updated);
    }

    private void CheckForLowestFloor(List<ARPlane> arPlanes)
    {
        // Ignore removed. We just need to pass Apple Compliance, lulz.
        for (int i = 0; i < arPlanes.Count; i++)
        {
            float yPos = arPlanes[i].transform.position.y;
            if (yPos < _floorPlanePrefab.position.y)
            {
                //Debug.Log($"Changing floor pos ({i+1}/{arPlanes.Count}): {yPos}");
                _floorPlanePrefab.transform.position = Vector3.up * yPos;
            }
        }
    }

    public void SetGenieShadowMultiplier(float normalizedValue)
    {
        _genieShadowMaterial.SetFloat(MULTIPLIER_PROPERTY_NAME, Mathf.Lerp(0, MULTIPLIER_MAX_VALUE, normalizedValue));
    }

    public void SetSpatialMeshVisibility(bool isVisible, float duration = 0.5f)
    {
        StartCoroutine(AnimateMeshOpacity(isVisible, duration));
    }

    private IEnumerator AnimateMeshOpacity(bool isVisible, float duration)
    {
        float startValue = _spatialMeshMaterial.GetFloat(OPACITY_PROPERTY_NAME);
        float endValue = isVisible ? SPATIAL_MESH_OPACITY_MAX : 0;

        float timer = 0;
        while (timer < duration)
        {
            _spatialMeshMaterial.SetFloat(OPACITY_PROPERTY_NAME,
                Mathf.Lerp(startValue, endValue, timer / duration));
            timer += Time.deltaTime;
            yield return null;
        }
        _spatialMeshMaterial.SetFloat(OPACITY_PROPERTY_NAME, endValue);

    }

    private void HandleMultiFingerTouch()
    {
        if (!_isSpatialMeshVisible)
        {
            SetSpatialMeshVisibility(true);
        }
        _isSpatialMeshVisible = true;
    }

    private void HandleThreeFingerDrag(Vector2 deltaPixels, Vector2 currScreenPoint)
    {
        HandleMultiFingerTouch();
    }
    private void HandleTwoFingerDrag(Vector2 deltaPixels, Vector2 currScreenPoint)
    {
        HandleMultiFingerTouch();
    }
    private void HandleTwoFingerScale(float deltaPixels, Vector2 pivotPointScreen)
    {
        HandleMultiFingerTouch();
    }
    private void HandleStopTouching(Vector2 lastTouchPos)
    {
        if (_isSpatialMeshVisible)
        {
            SetSpatialMeshVisibility(false);
        }
        _isSpatialMeshVisible = false;
    }
}
