using System;
using Cysharp.Threading.Tasks;
using Genies.Customization.Framework;
using Genies.UI.Widgets;
using Genies.Utilities;
using UnityEngine;

namespace Genies.Customization.MegaEditor
{
    /// <summary>
    /// Can be extended to create a custom Hsv color picker controller.
    /// </summary>
    public class HsvColorPickerCustomizationController : BaseCustomizationController, IColorPicker
    {
        private const string _colorPickerViewId = "hsv-color-picker";

        [SerializeField]
        [AssetPath.Attribute(typeof(HsvColorSpectrumPicker), AssetPath.PathType.Resources)]
        private string _colorPickerPrefab;
        private const float _colorPickerOffsetY = 55;

        public Color Color { get => _colorPicker.Color; set => _colorPicker.Color = value; }

        public event Action<Color> ColorUpdated
        {
            add => _colorPicker.ColorUpdated += value;
            remove => _colorPicker.ColorUpdated -= value;
        }

        private HsvColorSpectrumPicker _colorPicker;

        public override UniTask<bool> TryToInitialize(Customizer customizer)
        {
            var prefab = AssetPath.Load<HsvColorSpectrumPicker>(_colorPickerPrefab);
            _colorPicker = customizer.View.GetOrCreateViewInLayer(_colorPickerViewId, CustomizerViewLayer.CustomizationEditor, prefab);
            RectTransform _colorPickerRTranform = _colorPicker.GetRectTransform();
            Vector2 anchoredPos = _colorPickerRTranform.anchoredPosition;
            anchoredPos.y = _colorPickerOffsetY;
            _colorPickerRTranform.anchoredPosition = anchoredPos;
            _colorPicker.gameObject.SetActive(false);

            return UniTask.FromResult(true);
        }

        public override void StartCustomization()
        {
            _colorPicker.gameObject.SetActive(true);
        }

        public override void StopCustomization()
        {
            _colorPicker.gameObject.SetActive(false);
        }

        public override void Dispose()
        {
        }

        public void SetColorWithoutNotify(Color color)
        {
            _colorPicker.SetColorWithoutNotify(color);
        }
    }
}
