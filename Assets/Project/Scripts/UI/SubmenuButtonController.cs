using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class SubmenuButtonController : MonoBehaviour
{
    [SerializeField] private Button _button;
    [SerializeField] private RawImage _buttonImage;
    [SerializeField] private RawImage _buttonBgImage;

    public void Initialize(Texture buttonTexture, UnityAction[] buttonActions)
    {
        // Setup the button texture
        _buttonImage.texture = buttonTexture;

        // Setup listeners
        for (int i = 0; i < buttonActions.Length; i++)
        {
            int index = i; // Capture the current index
            _button.onClick.AddListener(() => buttonActions[index].Invoke());
        }
    }

    public void MakeBgTransparent()
    {
        _buttonBgImage.color = Color.clear;
    }

    private void OnDestroy()
    {
        _button.onClick.RemoveAllListeners();
    }
}
