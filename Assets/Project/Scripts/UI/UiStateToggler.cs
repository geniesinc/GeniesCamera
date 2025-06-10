using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// if there is a button, disable it
// if there is a textmeshugui, tint it
// if there is a raw image, tint it
public class UiStateToggler : MonoBehaviour
{
    private Color disabledTint = new Color(1, 1, 1, 0.5f);

    private RawImage rawImage;
    private Image image;
    private TMPro.TextMeshProUGUI textMesh;
    private Button button;
    private Slider slider;

    private Color originalColor;
    private bool originalButtonState;

    private bool isEnabledCurr;

    void Awake()
    {
        rawImage = GetComponent<RawImage>();
        image = GetComponent<Image>();
        textMesh = GetComponent<TMPro.TextMeshProUGUI>();
        button = GetComponent<Button>();
        slider = GetComponent<Slider>();
        originalColor = Color.white;
        isEnabledCurr = true;
    }

    public void SetEnabledState(bool isEnabledNew)
    {
        // sanity check
        if(isEnabledCurr == isEnabledNew)
        {
            return;
        }

        // if you're currently enabled, store color
        if (isEnabledCurr && !isEnabledNew)
        {
            // cache button state
            if(button != null)
            {
                originalButtonState = button.enabled;
            }

            // cache color
            if (rawImage != null)
            {
                originalColor = rawImage.color;
            }
            else if (image != null)
            {
                originalColor = image.color;
            }
            else if (textMesh != null)
            {
                originalColor = textMesh.color;
            }
        }

        // manipulate colors
        if (rawImage != null) {
            rawImage.color = isEnabledNew ? originalColor : originalColor * disabledTint;
        }
        if (image != null)
        {
            image.color = isEnabledNew ? originalColor : originalColor * disabledTint;
        }
        if (textMesh != null)
        {
            textMesh.color = isEnabledNew ? originalColor : originalColor * disabledTint;
        }

        // manipulate interactable state
        if (button != null)
        {
            button.enabled = isEnabledNew && originalButtonState;
        }
        if (slider != null)
        {
            slider.enabled = isEnabledNew;
        }

        isEnabledCurr = isEnabledNew;
    }
}
