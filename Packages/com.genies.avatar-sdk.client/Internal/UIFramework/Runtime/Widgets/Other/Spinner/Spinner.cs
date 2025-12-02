using Genies.UI.Animations;
using UnityEngine;
using UnityEngine.UI;

namespace Genies.UI.Widgets
{
    public class Spinner : Widget
    {
        public bool IsShown { get; protected set; }

        public Image ProgressForeground;
        public UnityEngine.Animation RotationAnimation;
        public bool ShowOnStartup;

        protected IAnimation<float> OpacityAnimation = new FloatAnimation();

        public override void OnWidgetInitialized()
        {
            base.OnWidgetInitialized();
            CheckUI();
        }

        protected void OnEnable()
        {
            if(ShowOnStartup)
            {
                Show();
            }
        }

        protected void CheckUI()
        {
#if UNITY_EDITOR
            if (ProgressForeground is null)
                Debug.LogError($"ProgressForeground not set!");

            if (CanvasGroup is null)
                Debug.LogError($"CanvasGroup not set!");

            if (RotationAnimation is null)
                Debug.LogError($"RotationAnimation not set!");
#endif
        }

        protected void Update()
        {
            OpacityAnimation.UpdateAnimation(Time.deltaTime);
            CanvasGroup.alpha = OpacityAnimation.AnimatedValue;
        }

        public void Show()
        {
            if (IsShown)
            {
                return;
            }

            IsShown = true;
            gameObject.SetActive(true);
            OpacityAnimation.Stop();
            RotationAnimation.Rewind("ProgressAnimation");
            RotationAnimation.Play();
            OpacityAnimation.Animate(0f, 1f, 0.25f);
        }

        public void Hide()
        {
            if (!IsShown)
            {
                return;
            }

            IsShown = false;
            OpacityAnimation.Stop();
            RotationAnimation.Stop();
            OpacityAnimation.Animate(1f, 0f, 0.25f, OnHidden);
        }

        public void HideImmediate()
        {
            if (!IsShown)
            {
                return;
            }

            IsShown = false;
            OpacityAnimation.Stop();
            RotationAnimation.Stop();

            CanvasGroup.alpha = 0;
            gameObject.SetActive(false);
        }

        private void OnHidden()
        {
            gameObject.SetActive(false);
        }
    }
}
