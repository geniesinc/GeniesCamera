using Genies.CameraSystem.Focusable;
using Genies.ServiceManagement;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Genies.UIFramework
{
    public class GenieScreenSpaceCameraController : MonoBehaviour
    {
        private CanvasScaler CanvasScaler
        {
            get
            {
                if (_canvasScaler == null)
                {
                    _canvasScaler = ScreenSpaceTargetRectTransform.GetComponentInParent<CanvasScaler>();
                }

                return _canvasScaler;
            }
        }
        private CanvasScaler _canvasScaler;

        private Vector2 ScreenSize => new Vector2(Screen.width, Screen.height);
        private Vector2 ReferenceResolution => CanvasScaler.referenceResolution;
        private Vector2 ReferenceResolutionRatio => ReferenceResolution / ScreenSize;

        [Header("References")]
        [FormerlySerializedAs("mainCam")]
        public Camera MainCam;
        [FormerlySerializedAs("screenSpaceTargetRectTransform")] public RectTransform ScreenSpaceTargetRectTransform;
        [FormerlySerializedAs("debugScreenSpaceBoundsRt")] public RectTransform DebugScreenSpaceBoundsRt;

        [Header("Options")]
        [FormerlySerializedAs("alwaysUpdate")]
        public bool AlwaysUpdate = false;
        [FormerlySerializedAs("padding")] public Vector2 Padding = new Vector2(.15f, .15f);
        [FormerlySerializedAs("centerOffset")] public Vector2 CenterOffset;
        [FormerlySerializedAs("dampMovement")] public bool DampMovement = false;
        [FormerlySerializedAs("dampTime")] public float DampTime = 0.5f;
        public Vector2 BoundsMinMax = new Vector2(0f, 1f);
        [FormerlySerializedAs("debugMode")] public bool DebugMode = false;

        // The square magnitude the new center should overcome to apply a framing operation
        [FormerlySerializedAs("recenterThreshold")] public float RecenterThreshold = 0.01f;
        public float RotationSpeed = 2;
        public float VerticalSpeed = 1.5f;
        public float FocusingSpeed = 6f;
        public const float DeclinationError = 0.05f;
        private Vector3 _dampVelocity;
        private bool _cameraIsMoving;
        private bool _isFocusingTargetObject;

        public IFocusable CurrentFocusableObject { get; private set; }
        private Bounds _currentBounds;
        private Bounds _currentBoundsFocusRegion;
        [FormerlySerializedAs("screenSpaceBounds")] public Rect ScreenSpaceBounds;

        //for garbage mitigation
        private Vector3[] _worldCorners = new Vector3[4];

        private void Awake()
        {
            this.RegisterSelf();
        }

        //Adjust the normalized vertical region of the bounds we would like to focus on
        //0 represents the bottom of the bounds, 1 is the top
        //ex: (0,1) is the default.  This will focus the entire bounds
        //ex: (0.5, 1) this would focus only on the upper half of the bounds
        public void SetAvatarFocusRegion(Vector2 minMaxRange, bool immediate = false)
        {
            BoundsMinMax = minMaxRange;
            if (immediate)
            {
                FrameCurrentBounds();
            }
            else
            {
                AlwaysUpdate = true;
                DampMovement = true;
            }
        }

        public void ResetFocusRegion(bool immediate)
        {
            SetAvatarFocusRegion(new Vector2(0, 1), immediate);
        }

        public void SetScreenSpaceTargetRect(RectTransform newRect)
        {
            ScreenSpaceTargetRectTransform = newRect;
            _isFocusingTargetObject = false;
        }

        public void SetTargetFocusableObject(IFocusable focusableTargetObject)
        {
            if (CurrentFocusableObject == focusableTargetObject)
            {
                return;
            }

            CurrentFocusableObject = focusableTargetObject;
            _isFocusingTargetObject = true;
        }

        public bool IsFinishedMoving()
        {
            return !_cameraIsMoving;
        }

        private void Update()
        {
            if (CurrentFocusableObject is null)
            {
                return;
            }

            PointAndFrame();
        }

        private void PointAndFrame()
        {
            RotateAndPointCamera();

            FrameBounds();

            UpdateHeight();
        }

        private void RotateAndPointCamera()
        {
            if (AlwaysUpdate && _isFocusingTargetObject)
            {
                if(RotateTowardsDirectionAndPoint())
                {
                    _isFocusingTargetObject = false;
                }
            }
        }

        private void UpdateHeight()
        {
            if (!AlwaysUpdate)
            {
                return;
            }

            if (CurrentFocusableObject == null)
            {
                return;
            }

            Bounds bounds = CurrentFocusableObject.GetBounds();
            var cameraHeight = transform.position.y;

            if (_isFocusingTargetObject && !IsWithin(cameraHeight,
                bounds.center.y - DeclinationError,
                bounds.center.y + DeclinationError))
            {
                MoveCameraToTargetHeight(bounds);
            }
        }

        private bool RotateTowardsDirectionAndPoint()
        {
            if (CurrentFocusableObject == null)
            {
                return false;
            }

            Bounds bounds = CurrentFocusableObject.GetBounds();
            Quaternion cameraRotation, c1, c2;
            ComputeRotations(bounds, out cameraRotation, out c1, out c2);
            PointCamera(cameraRotation);
            var angle = Quaternion.Angle(c1, c2);
            if (angle <= 0.2f)
            {
                return true;
            }

            RotateAroundTarget(bounds, c1, c2, angle);
            return false;
        }

        /// <summary>
        /// Moves camera to the height of the IFocusable
        /// </summary>
        /// <param name="bounds"></param>
        private void MoveCameraToTargetHeight(Bounds bounds)
        {
            var pos = transform.position;
            var desiredPosition = new Vector3(pos.x, bounds.center.y, pos.z);
            var position = Vector3.SmoothDamp(pos, desiredPosition, ref _dampVelocity, DampTime);

            if (transform.position != desiredPosition)
            {
                transform.position = Vector3.MoveTowards(transform.position, position, Time.deltaTime * VerticalSpeed);
            }
        }

        /// <summary>
        /// Points camera to focusable object,
        /// detects the closest rotation to view vector of the object
        /// and rotates around focusable object in that direction
        /// </summary>
        /// <param name="bounds">Bounds of the focusable object</param>
        /// <param name="targetCameraRotation">Target camera rotation</param>
        /// <param name="c1">Camera rotation</param>
        /// <param name="c2">Focusable object rotation</param>
        /// <param name="angle">Angle between camera rotation and focusable object rotation</param>
        private void RotateAroundTarget(Bounds bounds,
            Quaternion c1,
            Quaternion c2,
            float angle)
        {
            bool left = Mathf.DeltaAngle(c1.eulerAngles.y, c2.eulerAngles.y) < 0f;
            var rotationAxis = left ? -Vector3.up : Vector3.up;

            transform.RotateAround(bounds.center, rotationAxis, angle * RotationSpeed * Time.deltaTime);
        }

        private void PointCamera(Quaternion targetCameraRotation)
        {
            Quaternion current = transform.localRotation;
            transform.localRotation = Quaternion.Slerp(current, targetCameraRotation, Time.deltaTime * FocusingSpeed);
        }

        /// <summary>
        /// Computes camera rotation, camera rotation based on projected camera position,
        /// and focusable object rotation
        /// </summary>
        /// <param name="bounds">Bounds of the focusable object</param>
        /// <param name="cameraRotation">Camera rotation</param>
        /// <param name="c1">Camera rotation based on projected camera position</param>
        /// <param name="c2">Focusable object rotation</param>
        private void ComputeRotations(Bounds bounds,
            out Quaternion cameraRotation,
            out Quaternion c1,
            out Quaternion c2)
        {
            var pos = transform.position;
            Vector3 projectedPosition = new Vector3(pos.x, bounds.center.y, pos.z);
            cameraRotation = Quaternion.LookRotation(bounds.center - pos);

            c1 = Quaternion.identity;
            c2 = Quaternion.identity;

            //Checking if the vector was zero to avoid rotation
            var movementC1 = projectedPosition - bounds.center;
            if (movementC1 != Vector3.zero) {
                c1 = Quaternion.LookRotation(movementC1);
                c1.eulerAngles = new Vector3(0f, c1.eulerAngles.y, 0f);
            }

            //Checking if the vector was zero to avoid rotation
            var movementC2 = CurrentFocusableObject.TargetViewDirection - bounds.center;
            if (movementC2 != Vector3.zero) {
                c2 = Quaternion.LookRotation(movementC2);
                c2.eulerAngles = new Vector3(0f, c2.eulerAngles.y, 0f);
            }
        }

        private void FrameBounds()
        {
            if (AlwaysUpdate && CurrentFocusableObject != null &&
                ScreenSpaceTargetRectTransform != null)
            {
                UpdateCurrentBounds();
                FrameCurrentBounds();
            }
        }

        private void UpdateCurrentBounds()
        {
            _currentBounds = CurrentFocusableObject.GetBounds();
            _currentBoundsFocusRegion = new Bounds();

            var min = new Vector3(_currentBounds.min.x, Mathf.Lerp(_currentBounds.min.y, _currentBounds.max.y, BoundsMinMax.x), _currentBounds.min.z);
            var max = new Vector3(_currentBounds.max.x, Mathf.Lerp(_currentBounds.min.y, _currentBounds.max.y, BoundsMinMax.y), _currentBounds.max.z);
            _currentBoundsFocusRegion.SetMinMax(min, max);

            ScreenSpaceBounds = CalculateScreenSpaceBounds();
        }

        private void FrameCurrentBounds()
        {
            var focusBoundsCenter = _currentBoundsFocusRegion.center;
            var vectorToAvatar = focusBoundsCenter - MainCam.transform.position;
            var screenSpaceTargetRect = new Rect(ScreenSpaceTargetRectTransform.position, ScreenSpaceTargetRectTransform.rect.size);
            var screenSpaceTargetCenterRay = MainCam.ScreenPointToRay(screenSpaceTargetRect.position);
            var targetZoneCenterWorldPoint = screenSpaceTargetCenterRay.GetPoint(vectorToAvatar.magnitude);
            var panDelta = focusBoundsCenter - targetZoneCenterWorldPoint;
            panDelta = Vector3.ProjectOnPlane(panDelta, MainCam.transform.forward);

            var screenSpaceSizeDelta = Mathf.Max(ScreenSpaceBounds.height / screenSpaceTargetRect.height, ScreenSpaceBounds.width / screenSpaceTargetRect.width);
            var distTargetPos = (focusBoundsCenter + (-vectorToAvatar * screenSpaceSizeDelta));
            var distDelta = distTargetPos - MainCam.transform.position;

            Vector3 combinedDelta = distDelta + panDelta;
            combinedDelta = new Vector3(combinedDelta.x + CenterOffset.x, combinedDelta.y + CenterOffset.y, combinedDelta.z);
            combinedDelta = combinedDelta.sqrMagnitude > RecenterThreshold ? combinedDelta : Vector3.zero;

            if (DampMovement)
            {
                var position = MainCam.transform.position;
                var target = position + combinedDelta;
                target = _isFocusingTargetObject ? new Vector3(target.x, position.y, target.z) : target;
                position = Vector3.SmoothDamp(position, target, ref _dampVelocity, DampTime);
                MainCam.transform.position = position;

                _cameraIsMoving = target != position;
            }
            else
            {
                MainCam.transform.position += combinedDelta;
            }
        }

        private Rect CalculateScreenSpaceBounds()
        {
            Vector3 e = _currentBoundsFocusRegion.extents;
            Vector3 c = _currentBoundsFocusRegion.center;

            var camTransformRight = MainCam.transform.right;
            var up = new Vector3(c.x, c.y + (e.y + Padding.y), c.z);
            var down = new Vector3(c.x, c.y - (e.y + Padding.y), c.z);
            var left = c + -camTransformRight * (e.x + Padding.x);
            var right = c + camTransformRight * (e.x + Padding.x);

            //set corners
            _worldCorners[0] = up;
            _worldCorners[1] = down;
            _worldCorners[2] = left;
            _worldCorners[3] = right;

            //transform to screen space
            for (int i = 0; i < _worldCorners.Length; i++)
            {
                _worldCorners[i] = MainCam.WorldToScreenPoint(_worldCorners[i]) * ReferenceResolutionRatio;
            }

            //get bounds
            float maxX = float.MinValue;
            float minX = float.MaxValue;
            float maxY = float.MinValue;
            float minY = float.MaxValue;
            for (int i = 0; i < _worldCorners.Length; i++)
            {
                if (_worldCorners[i].x > maxX)
                {
                    maxX = _worldCorners[i].x;
                }

                if (_worldCorners[i].x < minX)
                {
                    minX = _worldCorners[i].x;
                }

                if (_worldCorners[i].y > maxY)
                {
                    maxY = _worldCorners[i].y;
                }

                if (_worldCorners[i].y < minY)
                {
                    minY = _worldCorners[i].y;
                }
            }

            return Rect.MinMaxRect(minX, minY, maxX, maxY);
        }

        private void OnValidate()
        {
            if (MainCam == null)
            {
                MainCam = Camera.main;
            }

            //Allows us to test the region focusing in the editor using the inspector
            if (Application.isEditor && DebugMode)
            {
                SetAvatarFocusRegion(BoundsMinMax);
            }
        }

        private bool IsWithin(float value, float minimum, float maximum)
        {
            return value >= minimum && value <= maximum;
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (!Application.isPlaying || !Application.isEditor || !DebugMode)
                return;

            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(_currentBounds.center, _currentBounds.size);

            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(_currentBoundsFocusRegion.center, _currentBoundsFocusRegion.size);

            if (DebugMode && DebugScreenSpaceBoundsRt != null)
            {
                DebugScreenSpaceBoundsRt.anchoredPosition = ScreenSpaceBounds.center;
                DebugScreenSpaceBoundsRt.sizeDelta = ScreenSpaceBounds.size;
            }
        }
#endif
    }
}
