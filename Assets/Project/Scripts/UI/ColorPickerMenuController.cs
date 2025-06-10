using System;
using System.Collections;
using System.Collections.Generic;
using HSVPicker;
using UnityEngine;
using UnityEngine.Events;

public class ColorPickerMenuController : MonoBehaviour
{
    public ColorPicker colorPicker;

    public delegate void ColorPickerClosedEvent();
    public event ColorPickerClosedEvent OnColorPickerClosed;

    public void Initialize(Color startColor, Action<Color> onColorChanged)
    {
        // Convert Action<Color> to UnityAction<Color>
        UnityAction<Color> unityAction = new UnityAction<Color>(onColorChanged);
        colorPicker.onValueChanged.AddListener(unityAction);
        colorPicker.CurrentColor = startColor;
    }

    private void OnDestroy()
    {
        colorPicker.onValueChanged.RemoveAllListeners();
    }

    public void PressOkButton()
    {
        OnColorPickerClosed?.Invoke();
    }
}
