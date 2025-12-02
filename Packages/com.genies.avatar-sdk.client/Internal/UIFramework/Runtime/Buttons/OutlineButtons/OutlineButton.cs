using System;
using System.Collections;
using Genies.UI.Animations;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Genies.UIFramework
{
    [System.Serializable]
    public class OutlineButtonState
    {
        [Header("Colors")]
        public Color ButtonColor;
        public Color OutlineColor;
        public Color TextColor;

        [Header("Other Options")]
        public Vector2 ButtonOffset;
        public Vector2 OutlineOffset;
        public float scaleFactor;
    }

    public class OutlineButton : MonoBehaviour, IPointerDownHandler
    {
        [Header("References")]
        public RectTransform RectTransform;
        public Image OutlineImage;
        public RectTransform OutlineRectTransform;
        public Image ButtonImage;
        public RectTransform ButtonRectTransform;
        public TextMeshProUGUI ButtonText;

        [Header("Options")]
        [FormerlySerializedAs("startEnabled")] public bool StartEnabled = false;
        [Obsolete("Use 'StartEnabled' instead.")] public bool startEnabled => StartEnabled;
        public float AnimationTime;
        public OutlineButtonState DefaultState;
        public OutlineButtonState PressedState;
        public OutlineButtonState DisabledState;
        [FormerlySerializedAs("onClick")] public UnityEvent OnClick;
        [Obsolete("Use 'OnClick' instead.")] public UnityEvent onClick => OnClick;

        public bool IsEnabled { get; private set; }
        private bool isAnimating => playingAnimation != null;
        private Coroutine playingAnimation;

        private void Awake()
        {
            SetButtonEnabledImmediate(StartEnabled);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (!IsEnabled || isAnimating)
            {
                return;
            }

            PressButton();
        }

        private void PressButton()
        {
            playingAnimation = StartCoroutine(PressingButton());
        }

        public void SetButtonEnabled(bool enable)
        {
            if (isAnimating)
            {
                StopAnimating();
                SetButtonState(enable ? DefaultState : DisabledState);
            }

            if (enable == IsEnabled)
            {
                return;
            }

            IsEnabled = enable;

            if (IsEnabled)
            {
                playingAnimation = StartCoroutine(InterpolatingButtonStates(DisabledState, DefaultState, AnimationTime / 2f, Ease.InOutSine));
            }
            else
            {
                playingAnimation = StartCoroutine(InterpolatingButtonStates(DefaultState, DisabledState, AnimationTime / 2f, Ease.InOutSine));
            }
        }

        public void SetButtonEnabledImmediate(bool enable)
        {
            if (enable == IsEnabled)
            {
                return;
            }

            if (isAnimating)
            {
                StopAnimating();
            }

            IsEnabled = enable;

            if (IsEnabled)
            {
                SetButtonState(DefaultState);
            }
            else
            {
                SetButtonState(DisabledState);
            }
        }

        private void StopAnimating()
        {
            if (playingAnimation != null)
            {
                StopCoroutine(playingAnimation);
            }

            playingAnimation = null;
        }

        private IEnumerator InterpolatingButtonStates(OutlineButtonState from, OutlineButtonState to, float duration, Ease ease = Ease.Linear)
        {
            float t = 0;
            while (t < 1f)
            {
                t += Time.deltaTime / duration;
                t = Mathf.Clamp01(t);
                InterpolateButtonStates(from, to, t, ease);
                yield return null;
            }

            StopAnimating();
        }

        private IEnumerator PressingButton()
        {
            float t = 0;
            while (t < 1f)
            {
                t += Time.deltaTime / AnimationTime / .5f;
                t = Mathf.Clamp01(t);
                InterpolateButtonStates(DefaultState, PressedState, t, Ease.OutSine);
                yield return null;
            }

            t = 0;
            while (t < 1f)
            {
                t += Time.deltaTime / AnimationTime / .5f;
                t = Mathf.Clamp01(t);
                InterpolateButtonStates(PressedState, DefaultState, t, Ease.InOutSine);
                yield return null;
            }

            OnClick.Invoke();
            StopAnimating();
        }

        private void InterpolateButtonStates(OutlineButtonState from, OutlineButtonState to, float time, Ease ease = Ease.Linear)
        {
            if (ease != Ease.Linear)
            {
                time = AnimateVirtual.EasedValue(0f, 1f, time, ease);
            }

            ButtonImage.color = Color.Lerp(from.ButtonColor, to.ButtonColor, time);
            OutlineImage.color = Color.Lerp(from.OutlineColor, to.OutlineColor, time);
            ButtonText.color = Color.Lerp(from.TextColor, to.TextColor, time);

            RectTransform.localScale = Mathf.Lerp(from.scaleFactor, to.scaleFactor, time) * Vector3.one;

            ButtonRectTransform.anchoredPosition = Vector2.Lerp(from.ButtonOffset, to.ButtonOffset, time);
            OutlineRectTransform.anchoredPosition = Vector2.Lerp(from.OutlineOffset, to.OutlineOffset, time);
        }

        private void SetButtonState(OutlineButtonState state)
        {
            ButtonImage.color = state.ButtonColor;
            OutlineImage.color = state.OutlineColor;
            ButtonText.color = state.TextColor;

            RectTransform.localScale = state.scaleFactor * Vector3.one;

            ButtonRectTransform.anchoredPosition = state.ButtonOffset;
            OutlineRectTransform.anchoredPosition = state.OutlineOffset;
        }


        #region Debug
        [ContextMenu("Set Button Enabled")]
        private void SetButtonEnabledTrue()
        {
            SetButtonEnabled(true);
        }

        [ContextMenu("Set Button Disabled")]
        private void SetButtonEnabledFalse()
        {
            SetButtonEnabled(false);
        }

        [ContextMenu("Set Default State")]
        private void SetDefaultState()
        {
            SetButtonState(DefaultState);
        }

        [ContextMenu("Set Disabled State")]
        private void SetDisabledState()
        {
            SetButtonState(DisabledState);
        }

        [ContextMenu("Set Pressed State")]
        private void SetPressedState()
        {
            SetButtonState(PressedState);
        }
        #endregion

    }
}
