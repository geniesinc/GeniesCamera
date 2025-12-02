using Genies.UI.Animations;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Genies.CustomWearables.View
{
    [RequireComponent(typeof(CanvasGroup))]
    public class ToastMessage : MonoBehaviour
    {
        [Header("Setup")][Space(5)]
        [SerializeField] private Color colorSuccess;
        [SerializeField] private Color colorFailed;
        [SerializeField] private Sprite iconSuccess;
        [SerializeField] private Sprite iconFailed;

        [Header("Assets")][Space(5)]
        [SerializeField] private Image backgroundSelection;
        [SerializeField] private Image icon;
        [SerializeField] private TextMeshProUGUI messageLabel;

        private CanvasGroup _canvas;
        private RectTransform _rectTransform;

        private void Awake()
        {
            _canvas = GetComponent<CanvasGroup>();
            _rectTransform = GetComponent<RectTransform>();
            _canvas.alpha = 0.0f;
        }

        public void ShowMessageSuccess(float delayFade, float secondsToShow, string message)
        {
            icon.sprite = iconSuccess;
            icon.color = colorSuccess;
            backgroundSelection.color = colorSuccess;

            ShowMessage(delayFade, secondsToShow, message);
        }

        public void ShowMessageError(float delayFade, float secondsToShow, string message)
        {
            gameObject.SetActive(true);
            icon.sprite = iconFailed;
            icon.color = colorFailed;
            backgroundSelection.color = colorFailed;

            ShowMessage(delayFade, secondsToShow, message);
        }

        private void ShowMessage(float delayFade, float secondsToShow, string message)
        {
            gameObject.SetActive(true);
            _canvas.alpha = 0;
            messageLabel.text = message;
            LayoutRebuilder.ForceRebuildLayoutImmediate(_rectTransform); // necessary after changing the text so the component refits its size

            _canvas.AnimationFade(1, delayFade).OnCompletedOneShot(() =>
            {
                _canvas.AnimationFade(0, delayFade).SetDelay(secondsToShow);
            });
        }
    }
}
