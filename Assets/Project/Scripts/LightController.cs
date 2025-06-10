using UnityEngine;

public class LightController : MonoBehaviour
{
    public delegate void LightEvent(Light l);
    public event LightEvent OnLightDataUpdatedByEstimation;

    [SerializeField] private LightEstimationController _lightEstimation;
    [SerializeField] private Light _mainLight;

    public Color CurrentColor { get { return _mainLight.color; } }

    public float CurrentDirectionAngleNormalized { get {
            return _mainLight.transform.rotation.eulerAngles.y.GetPositiveEulerValue()/360f; } }

    public float CurrentShadowLengthNormalized { get {
            return (1-Mathf.Abs(_mainLight.transform.rotation.eulerAngles.x.GetSmallestEulerValue())/90f); } }

    public float CurrentShadowDarknessNormalized { get {
            return Mathf.InverseLerp(0,
                                    _spatialMeshController.MaxShadowValue, 
                                    _spatialMeshController.CurrentShadowValue);
        } }

    // The value can be between 0 and 8. This allows you to create over bright lights.
    public float CurrentBrightness { get { return _mainLight.intensity; } }

    public bool IsUsingARKitLighting { get; private set; } = true;

    // Class variables
    private float _origAmbientIntensity = 1;
    private float _origReflectionIntensity = 1;
    private GeniesManager _geniesManager;
    private CameraManager _cameraManager;
    private SpatialMeshController _spatialMeshController;    
    private bool _didInitialize = false;

    public void Initialize(GeniesManager geniesManager,
                            CameraManager cameraManager,
                            SpatialMeshController spatialMeshController)
    {
        _geniesManager = geniesManager;
        _cameraManager = cameraManager;
        _spatialMeshController = spatialMeshController;

        _lightEstimation.Initialize(_cameraManager);

        _origAmbientIntensity = RenderSettings.ambientIntensity;
        _origReflectionIntensity = RenderSettings.reflectionIntensity;

        // Listen to some actions to refresh the lighting
        _lightEstimation.OnLightDataUpdated += LightDataUpdatedByEstimation;
        _cameraManager.OnActiveCameraTypeChanged += ResetLighting;

        // Make sure you never get a bad lighting angle
        _geniesManager.OnCurrentGenieTeleported += ResetLightPose;

        _didInitialize = true;
    }

    public void SetShadowDarkness(float normalizedValue)
    {
        _spatialMeshController.SetGenieShadowMultiplier(normalizedValue);
    }

    private void LightDataUpdatedByEstimation(Light l)
    {
        OnLightDataUpdatedByEstimation?.Invoke(l);
    }

    private void OnDestroy()
    {
        if (_didInitialize)
        {
            _lightEstimation.OnLightDataUpdated -= LightDataUpdatedByEstimation;
            _cameraManager.OnActiveCameraTypeChanged -= ResetLighting;
            _geniesManager.OnCurrentGenieTeleported -= ResetLightPose;   
        }
    }

    public void ToggleLightEstimationState()
    {
        SetLightEstimationActiveState(!IsUsingARKitLighting);
    }

    public void SetLightEstimationActiveState(bool isActive)
    {
        if (isActive)
        {
            _lightEstimation.EnableLightData(_mainLight);
        }
        else
        {
            _lightEstimation.DisableLightData(_mainLight);
        }
        IsUsingARKitLighting = isActive;
    }

    public void SetShadowDirection(float normalizedValue)
    {
        _mainLight.transform.rotation = Quaternion.Euler(_mainLight.transform.rotation.eulerAngles.x,
                                                        normalizedValue * 360f,
                                                        0);
    }

    public void SetShadowLength(float normalizedValue)
    {
        _mainLight.transform.rotation = Quaternion.Euler((1 - normalizedValue) * 90f,
                                                        _mainLight.transform.rotation.eulerAngles.y,
                                                        0);
    }

    public void SetLightColorAndBrightness(Color userColor)
    {
        // H,S, & V are normalized values between 0 and 1
        float userColor_h, userColor_s, userColor_v;
        Color.RGBToHSV(userColor, out userColor_h, out userColor_s, out userColor_v);

        // Lights don't have "black" in their color, that's up
        // to the "intensity" (or Value, in our case)
        Color lightColor = Color.HSVToRGB(userColor_h, userColor_s, 1);
        SetLightColor(lightColor);
        SetLightIntensity(userColor_v);
    }

    public void SetLightColor(Color newColor)
    {
        _mainLight.color = newColor;
    }

    public void SetLightIntensity(float newIntensityNormalized)
    {
        _mainLight.intensity = newIntensityNormalized;

        // Set the intensity of ambient lighting (Environment Lighting)
        // This is currently breaking lighting in Unity 2022.3.32f1...
        //RenderSettings.ambientIntensity = _origAmbientIntensity * newIntensityNormalized;
        RenderSettings.ambientSkyColor = _mainLight.color;

        // Set the intensity of reflection lighting (Environment Reflections)
        RenderSettings.reflectionIntensity = _origReflectionIntensity * newIntensityNormalized;
    }

    public void ResetLightPose()
    {
        // This is not necessary but is nice for debugging
        _mainLight.transform.position = _cameraManager.ActiveCamera.transform.position;

        // Get user look vector
        Vector3 userLookDir = _cameraManager.ActiveCamera.transform.forward;
        userLookDir = Vector3.ProjectOnPlane(userLookDir, Vector3.up);
        // Zero out any roll
        Quaternion userLookYaw = Quaternion.LookRotation(userLookDir, Vector3.up);
        userLookYaw = userLookYaw.ExtractYaw();
        // Pitch of -30
        _mainLight.transform.rotation = Quaternion.Euler(30f, userLookYaw.eulerAngles.y, 0f);
    }

    // This bool argument is to accommodate a callback we are subscribed to
    public void ResetLighting(ActiveCameraType _=ActiveCameraType.XRCamera)
    {
        ResetLightPose();
        SetLightColor(Color.white);
        SetLightIntensity(1);
    }
}
