using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;

public class ButtonAnimator : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    private RectTransform _buttonRectTransform;
    // Scale when button is pressed
    private float _pressedFactor = 0.9f;

    // Time to animate back to normal size and color
    private float _animationTime = 0.1f;

    private Vector3 _originalScale;

    private void Start()
    {
        _buttonRectTransform = GetComponent<RectTransform>();
        _originalScale = _buttonRectTransform.localScale;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        _buttonRectTransform.localScale = _originalScale * _pressedFactor;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        AnimateButtonRelease();
    }

    private async void AnimateButtonRelease()
    {
        float timeElapsed = 0f;
        Vector3 startScale = _buttonRectTransform.localScale;

        // Null-checks in here, because this object might live on something that is getting destroyed!
        while (timeElapsed < _animationTime)
        {
            if(_buttonRectTransform == null)
            {
                return;
            }
            _buttonRectTransform.localScale = Vector3.Lerp(startScale, _originalScale, timeElapsed / _animationTime);
            timeElapsed += Time.deltaTime;
            await Task.Yield(); // let the frame finish
        }

        if(_buttonRectTransform != null)
        {
            _buttonRectTransform.localScale = _originalScale;
        }
    }
}
