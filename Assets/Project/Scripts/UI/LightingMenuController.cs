using HSVPicker;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LightingMenuController : MonoBehaviour
{
    public ColorPicker colorPicker;
    public LightController lightController;

    public delegate void LightingMenuClosedEvent();
    public event LightingMenuClosedEvent OnLightingMenuClosed;

    /*public delegate void FloatEvent(float f);
    public event FloatEvent OnShadowDirectionChanged;*/

    [SerializeField] TextMeshProUGUI cameraButtonLabel;

    [SerializeField] Slider shadowDirectionSlider;
    [SerializeField] Slider shadowLengthSlider;

    [SerializeField] Slider brightnessSlider;
    [SerializeField] Slider shadowDarknessSlider;

    [SerializeField] UiStateToggler[] disableWithARKitLighting;

    void Start()
    {
        colorPicker.CurrentColor = lightController.CurrentColor;
        colorPicker.onValueChanged.AddListener(lightController.SetLightColorAndBrightness);
        shadowDirectionSlider.onValueChanged.AddListener(lightController.SetShadowDirection);
        shadowLengthSlider.onValueChanged.AddListener(lightController.SetShadowLength);
        shadowDarknessSlider.onValueChanged.AddListener(lightController.SetShadowDarkness);
        lightController.OnLightDataUpdatedByEstimation += ReceiveLightEsimation;

        lightController.SetLightEstimationActiveState(false);

        // Note, this will not work on Desktop!
        UpdateUiFromIsUsingARKitLighting();

        UpdateUiFromLight();
    }

    private void OnDestroy()
    {
        colorPicker.onValueChanged.RemoveListener(lightController.SetLightColorAndBrightness);
        shadowDirectionSlider.onValueChanged.RemoveListener(lightController.SetShadowDirection);
        shadowLengthSlider.onValueChanged.RemoveListener(lightController.SetShadowLength);
        shadowDarknessSlider.onValueChanged.RemoveListener(lightController.SetShadowDarkness);
        lightController.OnLightDataUpdatedByEstimation -= ReceiveLightEsimation;
    }

    public void PressOkButton()
    {
        OnLightingMenuClosed?.Invoke();
    }

    public void PressEstimateButton()
    {
        lightController.ToggleLightEstimationState();
        UpdateUiFromIsUsingARKitLighting();
    }

    void UpdateUiFromIsUsingARKitLighting()
    {
        for (int i = 0; i < disableWithARKitLighting.Length; i++)
        {
            bool isARKitOff = !lightController.IsUsingARKitLighting;
            disableWithARKitLighting[i].SetEnabledState(isARKitOff);
        }

        if (lightController.IsUsingARKitLighting)
        {
            cameraButtonLabel.text = "Don't Use\nCamera\nto Estimate";
        }
        else
        {
            cameraButtonLabel.text = "Use Camera\nto Estimate";
        }
    }

    void ReceiveLightEsimation(Light light)
    {
        if (!lightController.IsUsingARKitLighting)
        {
            return;
        }

        // Move the sliders to the new data points
        UpdateUiFromLight();
    }

    public void PressSpotlightButton()
    {
        lightController.ResetLighting();

        UpdateUiFromLight();
    }

    void UpdateUiFromLight()
    {
        // Set Saturation, Hue, Brightness
        colorPicker.CurrentColor = lightController.CurrentColor;

        brightnessSlider.SetValueWithoutNotify(lightController.CurrentBrightness);

        // Light direction and shadow length
        shadowLengthSlider.SetValueWithoutNotify(lightController.CurrentShadowLengthNormalized);
        shadowDirectionSlider.SetValueWithoutNotify(lightController.CurrentDirectionAngleNormalized);
        shadowDarknessSlider.SetValueWithoutNotify(lightController.CurrentShadowDarknessNormalized);
    }
}
