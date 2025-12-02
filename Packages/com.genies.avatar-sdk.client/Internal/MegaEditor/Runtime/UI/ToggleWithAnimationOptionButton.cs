using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Genies.Looks.Customization.UI
{
    internal sealed class ToggleWithAnimationOptionButton : MonoBehaviour
    {
        [SerializeField]
        private Button button;

        [SerializeField]
        private TMP_Text text;

        [SerializeField]
        private float horizontalMargin = 16.0f;

        public int Index { get; private set; }
        public string Label { get; private set; }
        public RectTransform RectTransform => _rectTransform ??= GetComponent<RectTransform>();

        public event Action<int> Clicked;

        private RectTransform _rectTransform;

        public void Set(int index, string label)
        {
            Index = index;
            text.text = Label = label;

            // recalculate the button's width based on the label text
            Vector2 preferredSize = text.GetPreferredValues();
            float width = preferredSize.x + 2.0f * horizontalMargin;
            RectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
        }

        private void Awake()
        {
            button.onClick.AddListener(OnClicked);
        }

        private void OnDestroy()
        {
            button.onClick.RemoveListener(OnClicked);
        }

        private void OnClicked()
        {
            Clicked?.Invoke(Index);
        }
    }
}
