using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Genies.UI
{
    /// <summary>
    /// A UI widget that we use to display asset IDs on QA builds. It acts as a button and will copy
    /// the asset label text to the clipboard when clicked.
    /// </summary>
    public sealed class DebuggingAssetLabel : MonoBehaviour
    {
        [FormerlySerializedAs("button")] [SerializeField] private Button _button;
        [FormerlySerializedAs("label")] [SerializeField] private TMP_Text _label;

        // make sure that this is always hidden in prod builds and setting a label does nothing
#if PRODUCTION_BUILD
        private void Awake()
        {
            gameObject.SetActive(false);
        }

        public void SetLabel(string value) { }
#else
        private bool _hasLabel;

        private void Awake()
        {
            _button.onClick.AddListener(OnClick);
            AssetLabelDebugging.EnabledStateChanged += OnEnabledStateChanged;
            gameObject.SetActive(AssetLabelDebugging.Enabled && _hasLabel);
        }

        private void OnDestroy()
        {
            _button.onClick.RemoveListener(OnClick);
            AssetLabelDebugging.EnabledStateChanged -= OnEnabledStateChanged;
        }

        public void SetLabel(string value)
        {
            if (!gameObject || gameObject == null)
            {
                return;
            }

            _label.text = value;
            _hasLabel = !string.IsNullOrEmpty(value);
            gameObject.SetActive(AssetLabelDebugging.Enabled && _hasLabel);
        }

        private void OnClick()
        {
            string value = _label.text;

            if (!string.IsNullOrEmpty(value))
            {
                AssetLabelDebugging.NotifyAssetLabelClicked(value);
            }
        }

        private void OnEnabledStateChanged(bool enabled)
        {
            gameObject.SetActive(enabled && _hasLabel);
        }
#endif
    }
}
