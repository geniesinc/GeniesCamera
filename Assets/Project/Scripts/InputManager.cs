using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

// Capture user Touches, defines complex Touch interactions,
// and makes Touch events out of them for other classes to listen to.
//
// InputManager lives on the same prefab as the XR Origin, so it also
// serves as a centralized reference point for input types other than Touch,
// such as Face and Camera.
public class InputManager : MonoBehaviour
{
    // Input events
    public event Action<float, Vector2> OnScale;
    public event Action<float, Vector2> OnTwist;
    public event Action<Vector2,Vector2> OnTwoFingerDrag;
    public event Action<Vector2,Vector2> OnThreeFingerDrag;
    public event Action<Vector2,float>  OnSwipeRelease;
    public event Action<Vector2> OnTouchDown;
    public event Action<Vector2> OnTouchDrag;
    public event Action<Vector2> OnTouchUp;
    public event Action<Vector2> OnDoubleTap;
    public event Action<Vector2> OnThreeFingerTap;


    // Used to calculate distance a touch is moving ("touch delta")
    private Vector2 _prevAvgTouchPos;
    // Used for scaling or rotating with two fingers
    private int _prevTouchCount;
    private float _prevTouchDist;
    private float _prevTouchAngle;

    // Used to calculate Swipe Velocity, where
    // a Swipe is defined as a horizontal drag in Pixel space
    // and is calcualted over the span of 10 frames.
    private float[] _swipeCache = new float[10];
    private int _currSwipeCacheIdx = 0;

    // We don't bother to handle touches on the UI layer, because
    // those are handled natively through Unity UI components.
    // Therefore, we will consider UI touches to be "invalid" touches.
    private bool _isValidTouch = false;

    // For raycasting against the UI. By making a class variable, we only
    // have to allocate for these once.
    PointerEventData _eventDataCurrentPosition = new PointerEventData(EventSystem.current);
    List<RaycastResult> _raycastResults = new List<RaycastResult>();

    // Used for faster Editor/Debugging QoL implementations for developer
    private bool _isDesktop = false;

    // Used to detect double taps
    private float _lastTouchUpTime = 0f;

    private bool _didInitialize = false;

    // Double tap detection related
    private const float MAX_DOUBLE_TAP_DIST = 300;
    private const float MAX_TIME_BTWN_DOUBLE_TAP = 0.25f;

    public void Initialize()
    {
#if UNITY_EDITOR
        _isDesktop = true;
#endif

        // Sanity check, remove non-zero garbage init vals
        ClearSwipeCache();

        _didInitialize = true;
    }

    private void Update()
    {
        // Prevent any race conditions by checking if we are initialized
        if (!_didInitialize)
        {
            return;
        }

        // Did you just begin a single finger Touch Down or mouse Click Down?
        bool didAddTouchThisFrame = _prevTouchCount < Input.touchCount;
        if ((_isDesktop && GetAnyMouseButtonDown()) ||
            (!_isDesktop && Input.touchCount >= 1 && didAddTouchThisFrame))
        {
            // Check to make sure they're not trying to touch the UI
            ValidateNewTouch();

            // Invalid touch is a foreground screenspace UI button touch
            if (!_isValidTouch)
            {
                _prevTouchCount = Input.touchCount;
                return;
            }

            // Track actions related to single-finger interaction
            // In this case, the average touch point *is* the touch point!
            Vector2 currAvgTouchPos = GetAverageTouchPoint();

            // Signal to subscribers that we've made a touch
            OnTouchDown?.Invoke(currAvgTouchPos);

            // Should also check if the distance isn't too far...
            /*Debug.Log($"Last Touch Delta: {Time.time - lastTouchUpTime}s," +
                $" {Vector2.Distance(currAvgTouchPos, prevAvgTouchPos)}px");*/

            if ((Time.time - _lastTouchUpTime <= MAX_TIME_BTWN_DOUBLE_TAP) &&
                (Vector2.Distance(currAvgTouchPos, _prevAvgTouchPos) <= MAX_DOUBLE_TAP_DIST) &&
                (Input.touchCount == 1 || _isDesktop))
            {
                // Double tap detected
                OnDoubleTap?.Invoke(currAvgTouchPos);
            }

            // Initialize this so that we can calculate deltas
            _prevAvgTouchPos = currAvgTouchPos;
        }

        // If you just released a finger (MouseButtonUp and TouchUp phases are sometimes missed!)
        if ((!_isDesktop && _isValidTouch && Input.touchCount == 0) ||
            (_isDesktop && _isValidTouch && !GetAnyMouseButton()))
        {
            // Signal to subscribers that we're done with a touch
            OnTouchUp?.Invoke(_prevAvgTouchPos);

            // We may also be done with a swipe!
            OnSwipeRelease?.Invoke(_prevAvgTouchPos, GetCurrentSwipeVelocity());

            // Reset swipe velocity
            ClearSwipeCache();

            // Reset this variable
            //Debug.Log("[InputManager] Touch Up: Resetting isValidTouch to False.");
            _isValidTouch = false;

            // Start timer for a doubletap
            _lastTouchUpTime = Time.time;
        }

        // A single feature touch persists...
        if ((!_isDesktop && _isValidTouch && Input.touchCount == 1) ||
            (_isDesktop && _isValidTouch && Input.GetMouseButton(0)))
        {

            // Did you just come from having 2 fingers down?
            if (_prevTouchCount > 1)
            {
                // If so, we need to clear that cache to prevent popping,
                // which will occur if your average touch point was between
                // two touches, and is now suddenly only one touch.
                _prevAvgTouchPos = GetAverageTouchPoint();
            }

            // We are dragging our finger across the screen
            Vector2 deltaPixels = GetAverageTouchPoint() - _prevAvgTouchPos;
            OnTouchDrag?.Invoke(deltaPixels);

            // Track the speed so that we can ramp down
            _swipeCache[_currSwipeCacheIdx] = deltaPixels.x;

            // Keep index in bounds using the % operator
            _currSwipeCacheIdx++;
            _currSwipeCacheIdx = _currSwipeCacheIdx % _swipeCache.Length;

            // Track this to continue calculating deltas
            _prevAvgTouchPos = GetAverageTouchPoint();
        }

        // Two finger Scaling: First touch down, or held frame
        if (!_isDesktop && _isValidTouch && Input.touchCount > 1)
        {
            float currTouchDist = Vector2.Distance(Input.touches[0].position, Input.touches[1].position);
            float currTouchAngle = GetLineSegmentAngle(Input.touches[0].position, Input.touches[1].position);
            Vector2 currAvgTouchPos = GetAverageTouchPoint();

            // If you just put down a second finger (or two at once), then
            // reset the previousTouchDistance cache.
            if (_prevTouchCount != Input.touchCount)
            {
                // On Second Touch Begin:
                _prevTouchDist = currTouchDist;
                _prevTouchAngle = currTouchAngle;
                _prevAvgTouchPos = currAvgTouchPos;
            }

            //Debug.Log($"Scale: {currTouchDist - prevTouchDist}x ({currTouchDist} - {prevTouchDist})");

            OnScale?.Invoke(currTouchDist - _prevTouchDist, GetAverageTouchPoint());
            OnTwist?.Invoke(currTouchAngle - _prevTouchAngle, GetAverageTouchPoint());

            Vector2 deltaPixels = currAvgTouchPos - _prevAvgTouchPos;
            if (Input.touchCount == 3)
            {
                OnThreeFingerDrag?.Invoke(deltaPixels, currAvgTouchPos);
            }
            else
            {
                // TwoFingers == 2 fingers || 4+ fingers, lol.
                OnTwoFingerDrag?.Invoke(deltaPixels, currAvgTouchPos);
            }

            _prevTouchDist = currTouchDist;
            _prevTouchAngle = currTouchAngle;
            _prevAvgTouchPos = currAvgTouchPos;
        }

        // MMB drag translate in XZ
        if (_isDesktop && _isValidTouch && Input.GetMouseButton(2))
        {
            Vector2 currTouchPos = GetAverageTouchPoint();
            Vector2 deltaPixels = currTouchPos - _prevAvgTouchPos;
            if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
            {
                OnThreeFingerDrag?.Invoke(deltaPixels, currTouchPos);
            }
            else
            {
                OnTwoFingerDrag?.Invoke(deltaPixels, currTouchPos);
            }

            _prevAvgTouchPos = currTouchPos;
        }

        // Triple finger touch
        if (!_isDesktop && _prevTouchCount < 3 && Input.touchCount >= 3)
        {
            OnThreeFingerTap?.Invoke(GetAverageTouchPoint());
        }
        if (_isDesktop && Input.GetKeyDown(KeyCode.Alpha3))
        {
            OnThreeFingerTap?.Invoke(GetAverageTouchPoint());
        }

        // Mouse wheel Scaling
        if (_isDesktop && Input.mouseScrollDelta.y != 0)
        {
            if (Input.GetKey(KeyCode.T))
            {
                OnTwist?.Invoke(Mathf.Sign(Input.mouseScrollDelta.y) * 10f, GetAverageTouchPoint());
            }
            else
            {
                OnScale?.Invoke(10 * Input.mouseScrollDelta.y, GetAverageTouchPoint());
            }
        }

        _prevTouchCount = Input.touchCount;
    }

    // This function is to help us remember/understand that we are dealing with
    // touch point averages! Because if we lift a finger off the touch array, then
    // suddenly our average point will change, and we will need to account for that.
    private Vector2 GetAverageTouchPoint()
    {
        return Input.mousePosition; ;
    }

    private bool GetAnyMouseButton()
    {
        return Input.GetMouseButton(0) || Input.GetMouseButton(1) || Input.GetMouseButton(2);
    }

    private bool GetAnyMouseButtonDown()
    {
        return Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1) || Input.GetMouseButtonDown(2);
    }

    private void ValidateNewTouch()
    {
        //Debug.Log($"InitializeNewTouch: {Input.touchCount} Touches");

        if (_isDesktop && IsTouchingUI(GetAverageTouchPoint()))
        {
            //Debug.Log("[InputManager] New Touch is Invalid: You are currently touching UI (Desktop).");
            _isValidTouch = false;
            return;
        }

        else if (!_isDesktop)
        {
            for (int i = 0; i < Input.touchCount; i++)
            {
                if (IsTouchingUI(Input.GetTouch(i).position))
                {
                    //Debug.Log("[InputManager] New Touch is Invalid: You are currently touching UI (Mobile).");
                    _isValidTouch = false;
                    return;
                }
            }
        }

        // It appears they're trying to interact with something other than the Fg UI Overlay
        //Debug.Log("[InputManager] New Touch is Valid: You are NOT currently touching UI.");
        _isValidTouch = true;
    }

    private bool IsTouchingUI(Vector2 touchPoint)
    {
        // Prepare to our parameter object
        _eventDataCurrentPosition.position = touchPoint;

        // Raycast against everything including UI and 3D game objects
        EventSystem.current.RaycastAll(_eventDataCurrentPosition, _raycastResults);
        for (int i = 0; i < _raycastResults.Count; i++)
        {
            // Determine if this is an attempt to interact with UI layer
            if (_raycastResults[i].gameObject.layer == (int)Layers.UI)
            {
                return true;
            }
        }

        return false;
    }

    private float GetLineSegmentAngle(Vector2 p1, Vector2 p2)
    {
        Vector2 hypoteneuse = p2 - p1;
        Vector2 adjacent = new Vector2(1, 0);

        float theta = Vector2.Angle(hypoteneuse, adjacent);

        // Is it negative direction?
        Vector3 crossProduct = Vector3.Cross(hypoteneuse, adjacent);
        if (crossProduct.z > 0)
        {
            theta = 360 - theta;
        }

        return theta;
    }

    private float GetCurrentSwipeVelocity()
    {
        float averageVelocity = 0f;
        for (int i = 0; i < _swipeCache.Length; i++)
        {
            averageVelocity += _swipeCache[i];
        }

        return averageVelocity / _swipeCache.Length;
    }

    private void ClearSwipeCache()
    {
        for (int i = 0; i < _swipeCache.Length; i++)
        {
            _swipeCache[i] = 0f;
        }
    }
}
