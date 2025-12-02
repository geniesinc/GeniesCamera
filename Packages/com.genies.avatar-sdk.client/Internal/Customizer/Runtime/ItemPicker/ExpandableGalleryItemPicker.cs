using System;
using Genies.UI;
using Genies.UI.Animations;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Genies.Customization.Framework.ItemPicker
{
    public class ExpandableGalleryItemPicker : GalleryItemPicker
    {
        [FormerlySerializedAs("panel")]
        [SerializeField]
        private ExpandablePanel _panel;
        [SerializeField]
        private Button _overlayButton;
        public Image overlay;
        public float overlayAlphaIn;

        public bool IsTransitioning => _panel.IsTransitioning;

        private void Awake()
        {
            _overlayButton.onClick.AddListener(Hide);
            _panel.TransitionStarted += OnTransitionStarted;
            _panel.TransitionEnded += OnTransitionEnded;
        }

        private void OnDestroy()
        {
            _overlayButton.onClick.RemoveListener(Hide);
            _panel.TransitionStarted -= OnTransitionStarted;
            _panel.TransitionEnded -= OnTransitionEnded;
        }

        private void OnTransitionStarted(int fromState, int toState)
        {
            if (toState == (int)GalleryItemPickerPanelState.Hidden)
            {
                AnimateOverlay(0);
            }
            else
            {
                AnimateOverlay(overlayAlphaIn);
            }
        }

        private void OnTransitionEnded(int state)
        {
            if (state == (int)GalleryItemPickerPanelState.Hidden)
            {
                OnHidden();
                base.Hide();
            }
        }

        public override void Show(IItemPickerDataSource dataSource)
        {
            base.Show(dataSource);
            _panel.SetState((int)GalleryItemPickerPanelState.HalfSize);
        }

        public override void Hide()
        {
            if (_panel.State != (int)GalleryItemPickerPanelState.Hidden)
            {
                _panel.SetState((int)GalleryItemPickerPanelState.Hidden);
            }
        }

        private void AnimateOverlay(float targetAlpha)
        {
            var targetColor = overlay.color;
            targetColor.a = targetAlpha;
            // Use smooth spring for natural color transition
            overlay.SpringColor(targetColor, SpringPhysics.Presets.Smooth).Play();
        }


    }
}
