using System;
using Genies.UI.Widgets;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


namespace Genies.Looks.Customization.UI
{
    public class PasteColorPopup : OkCancelPopupWidget
    {
        [SerializeField]
        private TMP_InputField _inputField;
        [SerializeField]
        private Button _copyButton;

        private BehaviorSetting _behaviorSetting;

        public void Initialize(BehaviorSetting behaviorSetting)
        {
            _behaviorSetting = behaviorSetting;
            ProcessInputFromColor(_behaviorSetting.InitializedColor);
            UpdateColor(_behaviorSetting.InitializedColor);
        }
        protected override void OnShown()
        {
            base.OnShown();
            _inputField.onValueChanged.AddListener(ProcessColorFromInput);
            _copyButton.onClick.AddListener(OnCopyAction);
        }

        protected override void OnHidden()
        {
            _inputField.onValueChanged.RemoveListener(ProcessColorFromInput);
            _copyButton.onClick.RemoveListener(OnCopyAction);
            base.OnHidden();
        }

        protected override void Ok()
        {
            OnApplyAction();
            base.Ok();
        }

        private void OnCopyAction()
        {
           _behaviorSetting.CopyAction.Invoke(_inputField.textComponent.color);
        }

        private void OnApplyAction()
        {
           _behaviorSetting.ApplyAction.Invoke(_inputField.textComponent.color);
        }
        private void ProcessColorFromInput(string input)
        {
            var validColor = ColorUtility.TryParseHtmlString("#" + input, out Color color);
            UpdateColor(validColor && color.a > 0 ? color :  Color.white);
        }

        private void ProcessInputFromColor(Color color)
        {
            var colorText = ColorUtility.ToHtmlStringRGBA(color);
            _inputField.text = colorText;

        }
        private void UpdateColor(Color color)
        {
            _inputField.textComponent.color = color;
        }

        public struct BehaviorSetting
        {
            public Color InitializedColor { get; }
            public Action<Color> ApplyAction { get; }
            public Action<Color> CopyAction { get; }

            public BehaviorSetting(Color initializedColor, Action<Color> applyAction, Action<Color> copyAction)
            {
                InitializedColor = initializedColor;
                ApplyAction = applyAction;
                CopyAction = copyAction;
            }
        }
    }
}
