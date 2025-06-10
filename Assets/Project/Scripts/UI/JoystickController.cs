using UnityEngine;
using UnityEngine.EventSystems;

public class JoystickController : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
{
    [SerializeField] private RectTransform _joystickContainer;
    [SerializeField] private RectTransform _joystickHandle;
    private Canvas _canvas;
    private Camera _canvasCam;
    private Vector2 _maxInputMagnitude;

    private Vector2 _inputVector = Vector2.zero;
    public Vector2 InputVector { get { return _inputVector; } }
    private bool _isUsingJoystick = false;
    private bool _isUsingWASD = false;
    public bool IsUsingJoystick { get { return _isUsingJoystick || _isUsingWASD; } }

    private void Start()
    {
        Vector2 center = Vector2.one * 0.5f;

        _joystickContainer.pivot = center;
        _joystickHandle.anchorMin = center;
        _joystickHandle.anchorMax = center;
        _joystickHandle.pivot = center;
        _joystickHandle.anchoredPosition = Vector2.zero;

        _canvas = GetComponentInParent<Canvas>();
        _canvasCam = null;
        if (_canvas.renderMode == RenderMode.ScreenSpaceCamera)
        {
            _canvasCam = _canvas.worldCamera;
        }

        _maxInputMagnitude = _joystickContainer.sizeDelta / 2;
    }

    private void Update()
    {
#if UNITY_EDITOR
        ListenForWASDInput();
#endif
    }

    private void ListenForWASDInput()
    {
        // For Unity editor testing or desktop mode one day...
        _isUsingWASD = Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.D);
        if (_isUsingWASD)
        {
            // Y Value
            _inputVector.y = Input.GetKey(KeyCode.W) ? 0.5f : Input.GetKey(KeyCode.S) ? -0.5f : 0;
            // X Value
            _inputVector.x = Input.GetKey(KeyCode.A) ? -0.5f : Input.GetKey(KeyCode.D) ? 0.5f : 0;
            // Sprint
            if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
            {
                _inputVector *= 2f;
            }
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        _isUsingJoystick = RectTransformUtility.RectangleContainsScreenPoint(_joystickHandle,
                                                                eventData.position,
                                                                eventData.pressEventCamera);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        _inputVector = Vector2.zero;
        _joystickHandle.anchoredPosition = Vector2.zero;
        _isUsingJoystick = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!_isUsingJoystick)
        {
            return;
        }

        // Calculate the input vector as an offset and scale it as normalized to the container
        Vector2 neutralPos = RectTransformUtility.WorldToScreenPoint(_canvasCam, _joystickContainer.position);
        _inputVector = (eventData.position - neutralPos) / (_maxInputMagnitude * _canvas.scaleFactor);

        // Don't exceed the bounds of the joystick container
        if (_inputVector.magnitude > 1)
        {
            _inputVector.Normalize();
        }
        // Update the joystick handle position
        _joystickHandle.anchoredPosition = _inputVector * _maxInputMagnitude;
    }

}