using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.XR.ARFoundation;

public class LightEstimationController : MonoBehaviour
{
    public delegate void LightEvent(Light l);
    public event LightEvent OnLightDataUpdated;

    [SerializeField] private TMPro.TextMeshProUGUI _debugText;

    /// <summary>
    /// The estimated brightness of the physical environment, if available.
    /// </summary>
    public float? Brightness { get; private set; }
    /// <summary>
    /// The estimated color temperature of the physical environment, if available.
    /// </summary>
    public float? ColorTemperature { get; private set; }
    /// <summary>
    /// The estimated color correction value of the physical environment, if available.
    /// </summary>
    public Color? ColorCorrection { get; private set; }
    /// <summary>
    /// The estimated direction of the main light of the physical environment, if available.
    /// </summary>
    public Vector3? MainLightDirection { get; private set; }
    /// <summary>
    /// The estimated color of the main light of the physical environment, if available.
    /// </summary>
    public Color? MainLightColor { get; private set; }
    /// <summary>
    /// The estimated intensity in lumens of main light of the physical environment, if available.
    /// </summary>
    public float? MainLightIntensityLumens { get; private set; }
    /// <summary>
    /// The estimated spherical harmonics coefficients of the physical environment, if available.
    /// </summary>
    public SphericalHarmonicsL2? SphericalHarmonics { get; private set; }

    private List<Light> _lights = new List<Light>();
    private string _debugString = "";
    private ARCameraManager _arCameraManager;
    private CameraManager _cameraManager;
    private bool _didInitialize = false;

    public void Initialize(CameraManager cameraManager)
    {
        _cameraManager = cameraManager;
        _arCameraManager = _cameraManager.ARCameraManager;

        _arCameraManager.frameReceived += UpdateLightData;
        _didInitialize = true;
    }

    public void EnableLightData(Light light)
    {
        if (!_lights.Contains(light))
        {
            _lights.Add(light);
        }
    }

    public void DisableLightData(Light light)
    {
        if (_lights.Contains(light))
        {
            _lights.Remove(light);
        }
    }

    void Update()
    {
        if (_didInitialize && _debugText != null && _debugText.enabled)
        {
            _debugText.text = _debugString;
        }
    }

    void OnDestroy()
    {
        if(_didInitialize)
        {
            _arCameraManager.frameReceived -= UpdateLightData;
        }
    }

    void UpdateLightData(ARCameraFrameEventArgs args)
    {
        /*if (arCameraManager.currentFacingDirection == CameraFacingDirection.User)
        {
            arCameraManager.requestedLightEstimation = LightEstimation.AmbientColor &
                                        LightEstimation.AmbientIntensity &
                                        LightEstimation.AmbientSphericalHarmonics &
                                        LightEstimation.MainLightDirection &
                                        LightEstimation.MainLightIntensity;
        }
        else
        {
            arCameraManager.requestedLightEstimation = LightEstimation.AmbientColor &
                                        LightEstimation.AmbientIntensity;
        }*/

        _debugString = $"data updated: {Time.time}\n";

        if (args.lightEstimation.averageBrightness.HasValue)
        {
            Brightness = args.lightEstimation.averageBrightness.Value;
            _debugString += $"averageBrightness: {Brightness.Value}\n";
        }
        else
        {
            _debugString += "averageBrightness: null\n";
        }

        if (args.lightEstimation.averageColorTemperature.HasValue)
        {
            ColorTemperature = args.lightEstimation.averageColorTemperature.Value;
            _debugString += $"colorTemperature: {ColorTemperature.Value}\n";
        }
        else
        {
            _debugString += $"colorTemperature: null\n";
        }

        if (args.lightEstimation.colorCorrection.HasValue)
        {
            ColorCorrection = args.lightEstimation.colorCorrection.Value;
            _debugString += $"colorCorrection: {ColorCorrection.Value}\n";
        }
        else
        {
            _debugString += $"colorCorrection: null\n";
        }

        if (args.lightEstimation.mainLightDirection.HasValue)
        {
            MainLightDirection = args.lightEstimation.mainLightDirection;
            _debugString += $"mainLightDirection: {MainLightDirection.Value}\n";
        }
        else
        {
            _debugString += $"mainLightDirection: null\n";
        }

        if (args.lightEstimation.mainLightColor.HasValue)
        {
            MainLightColor = args.lightEstimation.mainLightColor;
            _debugString += $"mainLightColor: {MainLightColor.Value}\n";

#if PLATFORM_ANDROID
            // ARCore needs to apply energy conservation term (1 / PI) and be placed in gamma
            m_Light.color = mainLightColor.Value / Mathf.PI;
            m_Light.color = m_Light.color.gamma;
            
            // ARCore returns color in HDR format (can be represented as FP16 and have values above 1.0)
            var camera = m_CameraManager.GetComponentInParent<Camera>();
            if (camera == null || !camera.allowHDR)
            {
                Debug.LogWarning($"HDR Rendering is not allowed.  Color values returned could be above the maximum representable value.");
            }
#endif
        }
        else
        {
            _debugString += $"mainLightColor: null\n";
        }

        if (args.lightEstimation.mainLightIntensityLumens.HasValue)
        {
            MainLightIntensityLumens = args.lightEstimation.mainLightIntensityLumens;
            Brightness = args.lightEstimation.averageMainLightBrightness.Value;
            _debugString += $"averageMainLightBrightness: {args.lightEstimation.averageMainLightBrightness.Value}\n";
        }
        else
        {
            _debugString += $"averageMainLightBrightness: null\n";
        }

        if (args.lightEstimation.ambientSphericalHarmonics.HasValue)
        {
            SphericalHarmonics = args.lightEstimation.ambientSphericalHarmonics;
            _debugString += $"sphericalHarmonics: {SphericalHarmonics.Value}\n";
        }
        else
        {
            _debugString += $"mainLightColor: null\n";
        }

        // Apply info to light
        for(int i=0; i < _lights.Count; i++)
        {
            _lights[i].intensity = Brightness != null ? Brightness.Value * 8f : 8f;
            _lights[i].colorTemperature = ColorTemperature != null ? ColorTemperature.Value : 6570f;
            _lights[i].color = ColorCorrection != null ? ColorCorrection.Value : Color.white;
            _lights[i].transform.rotation = MainLightDirection != null ?
                                                Quaternion.LookRotation(MainLightDirection.Value) :
                                                Quaternion.LookRotation(-_cameraManager.ActiveCamera.transform.forward);
            if (SphericalHarmonics != null)
            {
                RenderSettings.ambientMode = AmbientMode.Skybox;
                RenderSettings.ambientProbe = SphericalHarmonics.Value;
            }

            OnLightDataUpdated?.Invoke(_lights[i]);
        }
    }
}