using Genies.UI.Animations;
using UnityEngine;

namespace Genies.UI.Widgets
{
    [RequireComponent(typeof(RectTransform))]
    public class VerticalShowHideAnimation : MonoBehaviour, IShowHideAnimation
    {
        public RectTransform RectTransform { get; set; }
        public float AnimationDuration = 0.17f;
        public bool IsBottomAnchor = true;
        private float _initialPosY;

        private void Awake()
        {
            RectTransform = GetComponent<RectTransform>();
            _initialPosY = RectTransform.anchoredPosition.y;
            var hiddenPosition = IsBottomAnchor ? _initialPosY - RectTransform.sizeDelta.y : _initialPosY + RectTransform.sizeDelta.y;
            RectTransform.anchoredPosition = new Vector2(RectTransform.anchoredPosition.x, hiddenPosition);
        }

        public void Hide()
        {
            var hiddenPosition = IsBottomAnchor ? _initialPosY - RectTransform.sizeDelta.y : _initialPosY + RectTransform.sizeDelta.y;
            // Snappy spring for quick hide with subtle overshoot
            RectTransform.SpringAnchorPosY(hiddenPosition, SpringPhysics.Presets.Snappy)
                .OnCompletedOneShot(() => gameObject.SetActive(false));
        }

        public void Show()
        {
            // Gentle spring for smooth show with natural bounce
            RectTransform.SpringAnchorPosY(_initialPosY, SpringPhysics.Presets.Gentle);
        }
    }
}
