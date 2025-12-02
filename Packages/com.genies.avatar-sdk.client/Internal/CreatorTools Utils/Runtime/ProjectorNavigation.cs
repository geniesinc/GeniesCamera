using System;
using System.Collections.Generic;
using Genies.Components.CreatorTools.TexturePlacement;
using Genies.CreatorTools.utils;
using UnityEngine;

namespace Genies.Components.CreatorTools.TexturePlacement.Navigation
{
    public class ProjectorNavigation : MonoBehaviour
    {

        [Header("Navigation Objects and Controls")]
        public Transform projectorParent;

        public Transform gestureScaleRotateTransform;
        private BoxCollider _moverCollider;
        private MeshFilter _dragPlaneFilter;

        [Tooltip("Should contain ProjectorNavigationManager. This script manages the camera controls")]
        public CameraOrbiter cameraOrbiter;

        [Tooltip(
            "This should be empty for now. This Material slot previously held Ian's projection material so it's scale value could be driven, " +
            "could be used for new shader controls in the future.")]
        public Material projectorMaterial;

        [Tooltip(
            "Should contian Movement_Handle. when clicked and dragged this collider moves the navigation system under the mouse along the target collider")]
        public Transform movementHandle;

        [Tooltip("Should contain ProjectorMover. This object is moved along the surface of the target collider and" +
                 "should have other components parented underneath")]
        public Transform projectorMover; // The object to move to the clicked position

        [Tooltip(
            "Should contain Joystick_Handle. When clicked or touched and dragged this collider rotates the joystick. ")]
        public Transform joystickHandle;

        [Tooltip(
            "Should contain Josytic_Pivot. This is the joysticks pivot and receives rotation data from our control stytem when the Joystick_Handle is being moved")]
        public Transform JoystickPivot;

        [Tooltip(
            "Should contain ScareRotate_Handle. When clicked or touched and dragged along, this collider is the drag durface for determining the scale and rotate of the texture")]
        public Transform ScaleRotateInput;

        [Tooltip(
            "Should contain ScareRotate_Pivot. This object is the pivot that receives rotation date from the scaleRotate system")]
        public Transform scaleRotatePivot;

        [Tooltip(
            "The puppetMover and DragPlane are spawned in to create a instanced surface the user drags along when using the joystick to determine rotation")]
        public GameObject JoystickPuppetMover_Prefab;

        private GameObject joysticPuppetMover_Instance;

        [Tooltip(
            "The puppetMover and DragPlane are spawned in to create a instanced surface the user drags along when using the joystick to determine rotation")]
        public GameObject JoystickDragPlane_Prefab;

        private GameObject joystickDragPlane_Instance;

        [Header("Movement Settings")] public bool MarioMode = false;

        private Vector3 _currUp = Vector3.up;
        private Vector3 _currNormal;
        private Vector3 _previousNormal;


        [Tooltip("Toggle Camera Orbiting")] public bool cameraOrbit = false;

        [Tooltip("Toggle the Angle Limitier.")]
        ///When draging the Navigator across a surface, the Angle limiter's purpose is to
        /// maintian the initial projector angle until difference between the starting
        /// position and rotation of the projector differs a specified amount from the
        /// position and normal angle of the new target surface.
        public bool angleLimiterToggle = true;

        //Even if angleLimiting setting is on, the angle limiting can be turned on and off
        //based on live user input during use.
        private bool angleLimiter;

        [Tooltip("Distance difference at wich the limmiter begins sampling surface normal.")]
        public float angleLimit_Distance_Threshold_Start = .2f;

        [Tooltip("Distance difference at wich limiter fully samples surface normal.")]
        public float angleLimit_Distance_Threshold_End = .3f;

        [Tooltip("Angle differnce at wich the limiter begins sampling from the surface normal.")]
        public float angleLimit_Angle_Threshold_Start = 45;

        [Tooltip("Angle difference at wich the limiter fully samples surface normal.")]
        public float angleLimit_Angle_Threshold_End = 90;

        [Space(3)] [Tooltip("Toggle angle smoothing")]
        ///Angle Smoothing simply lerps between different sampled Normal values of a the target
        ///collider each frame the navigator is dragged along that surface.
        /// The effect of this is that the angle is smoothed out as we move.
        public bool angleSmoothing = true;

        [Tooltip("Speed at wich the angle lerps from the last angle to a newly defined surface angle")]
        public float angleSmoothing_Speed = .1f;

        [Space(3)] [Tooltip("Toggle the velocity cancel.")]
        ///The purpose of velocity cancel is to allow users to turn off the angle limiter.
        /// moving the navigator back and forth rapidly (and reaching a velocity of veocityCancleTarget
        /// the angle limiter will be turned off and the usser can sample the normal angle even
        /// where they started dragging.
        public bool velocityCancel = true;

        [Tooltip("The velocity at wich angle limiting will be cancled.")]
        public float velocityCancelTarget = 3.5f;

        [Space(3)] [Tooltip("Toggle the Angle Scanning.")]
        ///Angle scanning currently uses a mesh to cast a grid of racyasts at the target
        /// collider. These samples will be used to get an idea of the average surface
        /// angle and better control the angle of the projector and avoid outlier angles
        /// like the extreme angles of a nose.
        public bool angleScanning = false;

        [Tooltip("Toggle the Angle Scanning debugging.")]
        public bool angleScanning_Debug = false;

        [Tooltip(
            "This mesh determines the number and angle of scanning raycasts. 1 raycast per vertex along its normal angle.")]
        public MeshFilter angleScan_Mesh;

        [Tooltip(
            "This currently uses the AngeleScanner_Visualizer. This prefab is instantiated to track and visualize the raycast hits from the angle scanner")]
        public GameObject angleScan_VisualizerPrefab;

        [Tooltip(
            "This currently uses the AngleScanner_Visualizer_Shader. This is added to an instnaced material for each visualizer, so the visualizers color can be controlled based on its deviation from the target normal angle.")]
        public Shader angleScan_VisualizerShader;

        //holds the list of instantiated visualizer assets
        private List<Transform> angleScan_VisualizerList = new List<Transform>();

        //holds the average angle collected by our angle scanners
        private Quaternion AngleScan_averageAngle;

        //this is an additional visualizer that is larger and shows the average angle of all angle scanns
        private Transform AngleScan_AverageAngleVisualizer;
        [Space(3)] public bool moverScale = true;
        private float lastScale = 0;
        public float mover_minScale = .07f;
        public float mover_MaxScale = .3f;

        public float gesture_minScale = .25f;
        public float gesture_MaxScale = 4f;

        private Vector3 _moverScaleVector = new Vector3();
        private VirtualTransform _returnTransform = new VirtualTransform();


        [Header("Debug Options")]
        [Tooltip(
            "When checked, this will spawn a rendered copy of the otherwise invisibly target collider, ever frame the navigator is moving, for debugging purposes.")]
        public bool visualizeBodyCollider = false;

        private GameObject _visualizeBodyCollider_VisualizerObject;
        private SkinnedMeshRenderer _visualizeBodyCollider_VisuzlizerRenderer;

        //the list of individual raycast results from our scanner
        private List<VirtualTransform> scannerResults = new List<VirtualTransform>();


        [Header("LayerMaskAssignment")]
        //these layer masks are used to control the targeting of the various raycast in this project.

        [Tooltip("This layer is used primarily for the initial click inputs")]
        public LayerMask inputLayer;

        [Tooltip("This layer is used primarily for drag controls")]
        public LayerMask controlLayer;

        [Tooltip("This layer should contain the navigation meshcollider used to move along the characters surface")]
        public LayerMask proxyGeoLayer;

        [Tooltip("This layer is used to pickup a second orientation object." +
                 "The normal of this mesh collider and the proxy geo mesh collider will" +
                 "be averaged. This helps to smooth out the navigation angle. " +
                 "See Orientation adjuster under the ProjectionNavigationManger " +
                 "inside of the projector prefab")]
        public LayerMask orientationLayer;


        // The Tattooenator has the SkinnedMeshRenderer - the mesh we project on will be
        // the same mesh to detect clicks on.
        public Tattooenator Tattooenator { get; set; }

        // points to Tattooenator.SkinnedMeshRenderer
        private SkinnedMeshRenderer _skinnedMeshClicked;

        ///these are use to log click targets as we move between mous down,
        /// mouse held/dragged, and mouse up.

        public event Action<bool> OnMoverSelected;

        private bool mover_Clicked;
        private bool joystick_Clicked;
        private bool scaleRotate_Clicked;


        ///The results of the primary target raycast. See VirtualTransform Struct definition lower down for more info on what this stores
        private VirtualTransform activeRaycast;

        //scale rotate logs
        private bool scaleRoate_Init;
        private Vector3 scaleRotate_initialRelativeVector;
        private float scaleRotate_InitialScaleDistance;
        private Vector3 scaleRoate_InitialScale;



        //Angle Limter Logs
        private Vector3 moverInitPosition;
        private Quaternion moverInitRotation;
        private Quaternion JoysticInitRotation;
        private Vector3 moverPreviousPosition;
        private bool _inputBegan;
        private Vector3 _inputPosition;


        /// The VirtualTranform has been built up as the one stop shope for post raycast data
        /// this was originally conceptualized as a good way to store transform data without
        /// holding an object or creating a seperate vector 3 and quaterion. but expanded to
        /// be more racyast hit data focused

        public struct VirtualTransform
        {
            //the position of the raycast hit
            public Vector3 position;

            //the rotation of the raycast hit
            public Quaternion rotation;

            public Vector3 normal;

            //the distance the raycast travled
            public Vector3 surfaceNormal;

            public float distance;

            //did the raycast make contact?
            public bool madeContact;

            //where did the raycast begin
            public Vector3 raycastOrigin;

            //what angle was the original raycast
            public Vector3 raycastAngle;

            //this method should allow a virtual transform to be set = to a Transform and
            //return that transforms position and rotaiton
            public static implicit operator VirtualTransform(Transform t)
            {
                VirtualTransform vt = new VirtualTransform();
                vt.position = t.position;
                vt.rotation = t.rotation;
                return vt;
            }

        }

        private void Start()
        {

            //Collect initial mover position
            moverInitPosition = projectorMover.position;
            moverInitRotation = projectorMover.rotation;
        }

        private void Update()
        {
            if (_moverCollider == null)
            {
                _moverCollider = movementHandle.GetComponent<BoxCollider>();
            }

            if (lastScale != gestureScaleRotateTransform.localScale.x)
            {
                //scale the mover handle.
                var gestureScale = gestureScaleRotateTransform.localScale.x;
                lastScale = gestureScale;
                // Ensure A stays within its allowed range
                gestureScale = Mathf.Clamp(gestureScale, gesture_minScale, gesture_MaxScale);

                // Calculate the t value to linearly interpolate B between Bmin and Bmax
                float t = (gestureScale - gesture_minScale) / (gesture_MaxScale - gesture_minScale);

                // Interpolate B between Bmin and Bmax based on t
                var moverScale = Mathf.Lerp(mover_minScale, mover_MaxScale, t);
                _moverScaleVector.x = moverScale;
                _moverScaleVector.y = moverScale * 0.5f;
                _moverScaleVector.z = moverScale;

                _moverCollider.size = _moverScaleVector;
            }


#if UNITY_EDITOR
		//ON MOUSE DOWN
        _inputBegan = Input.GetMouseButtonDown(0);
        _inputPosition = Input.mousePosition;
#else
            _inputBegan = Input.touchCount == 1 ? Input.touches[0].phase == TouchPhase.Began : false;
            _inputPosition = Input.touchCount == 1 ? Input.touches[0].position : Vector3.zero;
#endif

            if (_inputBegan)
            {
                _skinnedMeshClicked = Tattooenator.SkinnedMeshRenderer;
                RaycastHit hit;
                //this checks for clic input on input colliders
                if (Physics.Raycast(Camera.main.ScreenPointToRay(_inputPosition), out hit, float.MaxValue, inputLayer))
                {
                    //here we determin wich input was clicked and intialize that input for drag
                    if (hit.transform == movementHandle)
                    {
                        Click_MovementHandle();
                        mover_Clicked = true;
                        OnMoverSelected?.Invoke(true);
                        //Debug.Log("Mover Clicked");
                    }

                    if (hit.transform == joystickHandle)
                    {
                        joystick_Clicked = true;
                        Click_JoystickHandle();
                        //Debug.Log("Joystick Clicked");
                    }

                    if (hit.transform == ScaleRotateInput)
                    {
                        //Debug.Log("ScaleRotate Clicked");
                        scaleRotate_Clicked = true;
                        scaleRoate_Init = true;
                        Click_ScareRotateControl();
                    }
                }

            }

            bool dragMoved = false;
#if UNITY_EDITOR
        //WHILE MOUSE HELD (DRAG)
        dragMoved = Input.GetMouseButton(0);
#else
            dragMoved = Input.touchCount == 1 ? Input.touches[0].phase == TouchPhase.Moved : false;
#endif

            if (dragMoved)
            {
                if (mover_Clicked)
                {
                    Drag_MovementHandle(Tattooenator.MeshCollider.sharedMesh);
                }

                if (joystick_Clicked)
                {
                    Drag_JoystickHandle();

                }

                if (scaleRotate_Clicked)
                {
                    Drag_ScaleRotateControl();
                }
            }

            bool inputEnd = false;
#if UNITY_EDITOR
        //ON MOUSE UP (CLEANUP)
        inputEnd = Input.GetMouseButtonUp(0);
#else
            inputEnd = Input.touchCount == 1 ? Input.touches[0].phase == TouchPhase.Ended : false;
#endif

            if (inputEnd)
            {
                OnMoverSelected?.Invoke(false);

                //reset all controls
                mover_Clicked = false;
                joystick_Clicked = false;
                scaleRotate_Clicked = false;

                // sync the physics collider so that the collider moves with the transform.
                Physics.SyncTransforms();

                //delete any temp navigation objects
                Destroy(joysticPuppetMover_Instance);
                Destroy(joystickDragPlane_Instance);
            }
        }
        //END UPDATE FUNCTION-----------------

        //SCALE ROTATE initialzie
        private void Click_ScareRotateControl()
        {
            Debug.Log("Firing");
            joysticPuppetMover_Instance = Instantiate(JoystickPuppetMover_Prefab, ScaleRotateInput.position,
                ScaleRotateInput.rotation);
            joystickDragPlane_Instance = Instantiate(JoystickDragPlane_Prefab, ScaleRotateInput.position,
                ScaleRotateInput.rotation);
            joystickDragPlane_Instance.transform.Rotate(0, 0, 0);

        }

        //DRAG SCALLE ROTATE
        private void Drag_ScaleRotateControl()
        {
            if (_dragPlaneFilter == null)
            {
                _dragPlaneFilter = joystickDragPlane_Instance.GetComponent<MeshFilter>();
            }

            RaycastHit hit;
            //after we click the input layer, we can begin draging on the scaleRotate_Handle, wich is a plane
            if (Physics.Raycast(Camera.main.ScreenPointToRay(_inputPosition), out hit, float.MaxValue, controlLayer))
            {
                var hitTransform = GetTransformFromHit(hit, _dragPlaneFilter.sharedMesh, _dragPlaneFilter.transform);
                joysticPuppetMover_Instance.transform.position = hitTransform.position;
                joysticPuppetMover_Instance.transform.rotation = hitTransform.rotation;

                if (scaleRoate_Init)
                {
                    Debug.Log("rotation intialized");
                    scaleRotate_initialRelativeVector = scaleRotatePivot.InverseTransformPoint(hitTransform.position);
                    scaleRotate_InitialScaleDistance =
                        Vector3.Distance(ScaleRotateInput.position, hitTransform.position);
                    scaleRoate_InitialScale = scaleRotatePivot.localScale;
                    scaleRoate_Init = false;
                }

                //can this be moved to Click_ScareRotateControl()?
                if (!scaleRoate_Init)
                {

                    // Calculate the current vector from object A to object B
                    Vector3 currentRelativeVector = scaleRotatePivot.InverseTransformPoint(hitTransform.position);
                    float angle = Vector3.SignedAngle(scaleRotate_initialRelativeVector, currentRelativeVector,
                        Vector3.up);
                    scaleRotatePivot.Rotate(Vector3.up, angle, Space.Self);

                    //Calculate the current distance from target to mover
                    float currentNormalizedDistance =
                        (Vector3.Distance(ScaleRotateInput.position, joysticPuppetMover_Instance.transform.position) /
                         scaleRotate_InitialScaleDistance);

                    scaleRotatePivot.localScale =
                        new Vector3(1, 1, 1) * (currentNormalizedDistance * scaleRoate_InitialScale.x);

                }

                gestureScaleRotateTransform.localScale = scaleRotatePivot.localScale;

            }

        }


        //JOYSTICK CONTROLS
        private void Click_JoystickHandle()
        {
            //Initialzie Joystick control
            //spawn in a plane and an object to mover across itdrag solution for handle movement
            joysticPuppetMover_Instance = Instantiate(JoystickPuppetMover_Prefab, joystickHandle.position,
                joystickHandle.rotation);
            joystickDragPlane_Instance = Instantiate(JoystickDragPlane_Prefab, joystickHandle.position,
                Camera.main.transform.rotation);

            joystickDragPlane_Instance.transform.Rotate(-90, 0, 0);
        }



        private void Drag_JoystickHandle()
        {
            if (_dragPlaneFilter == null)
            {
                _dragPlaneFilter = joystickDragPlane_Instance.GetComponent<MeshFilter>();
            }

            RaycastHit hit;

            if (Physics.Raycast(Camera.main.ScreenPointToRay(_inputPosition), out hit, float.MaxValue, controlLayer))
            {

                var hitTransform = GetTransformFromHit(hit, _dragPlaneFilter.sharedMesh,
                    joystickDragPlane_Instance.transform);

                joysticPuppetMover_Instance.transform.position = hitTransform.position;

                //Aim Joystick
                Quaternion rotation =
                    Quaternion.LookRotation((hitTransform.position - JoystickPivot.position).normalized);
                //below, the quaterion (90,0,0) corrects for the mesh orientation
                //on the josytick shaft, this could be removed later if a custom mesh is used
                JoystickPivot.rotation = rotation * Quaternion.Euler(90, 0, 0);
            }

            if (gestureScaleRotateTransform.localScale.x < 1)
            {
                Debug.Log("gestureScaleRotateTransform.localScale is : " + gestureScaleRotateTransform.localScale.x);
            }

        }

        //MOVEMENT CONTROLS
        private void Click_MovementHandle()
        {
            if (angleLimiterToggle)
            {
                //collect initial rotation positions for angle limiting
                //Scanning Note: these angles may need to be collecetd for angle scanning
                //even if angle limiting is off
                moverInitPosition = projectorMover.position;
                moverInitRotation = projectorMover.rotation;
                JoysticInitRotation = JoystickPivot.localRotation;
                angleLimiter = true;
            }
            //if we are not angle limiting, return the joystick to its identity, its rotation
            //is reset and it does not interfere with our pure mesh normal angle
            else
            {
                JoystickPivot.localRotation = Quaternion.identity;
            }
        }

        //BIG DRAG MOVEMENT METHOD
        private void Drag_MovementHandle(Mesh incMeshDataRef)
        {
            //Ke here is your hook for where raycast data from your touch input should be checked
            //your hit should be stored in a raycastHit hit llike the raycast below
            RaycastHit hit;

            //and here you should sub in a bool that tells us wether our drag is hitting our target collider (proxy, avatar, wearable(MVP2) etc)
            if (Physics.Raycast(Camera.main.ScreenPointToRay(_inputPosition), out hit, float.MaxValue, proxyGeoLayer))
            {
                ///passing the hit, skinned mesh renderer shared mesh, and the skinned mesh renderer transform into here
                /// will produce an Virtual transform,
                var hitTransform =
                    GetTransformFromHit(hit, _skinnedMeshClicked.sharedMesh, _skinnedMeshClicked.transform);
                //attempt to control y rotation

                //log this insitial raycast data before we modify it
                activeRaycast = hitTransform;

                //toggle between our two rotation retention systems
                if (MarioMode)
                {
                    var qdif = Quaternion.FromToRotation(_previousNormal, _currNormal);
                    _currUp = qdif * _currUp;
                    _previousNormal = _currNormal;
                    hitTransform.rotation = Quaternion.LookRotation(_currUp, _currNormal);
                }
                else
                {
                    var t2 = Vector3.Cross(_currNormal, Vector3.up);
                    hitTransform.rotation = Quaternion.LookRotation(t2, _currNormal);
                }


                //reset the joystick if we are moving
                var joysticCurrentRot = Quaternion.identity;


                //Check if there is an orientation  mesh to blend with our avatar surface
                //this will not be used for mvp1
                if (Physics.Raycast(Camera.main.ScreenPointToRay(_inputPosition), out hit, float.MaxValue,
                        orientationLayer))
                {

                    //might need to be a check to get skinned mesh renderer or mesh renderer eventually
                    var orientationMesh = hit.collider.gameObject.GetComponent<MeshFilter>();

                    hitTransform.rotation = Quaternion.Slerp(hitTransform.rotation,
                        GetTransformFromHit(hit, orientationMesh.mesh, hit.transform).rotation,
                        0.5f);
                }

                //collect mover velocity
                var velocity = (Vector3.Distance(hitTransform.position, moverPreviousPosition)) / Time.deltaTime;

                //if we are moving at a speed above the threshold reset our rotations and turn off angle limiter
                if (velocity > velocityCancelTarget)
                {
                    projectorMover.position = hitTransform.position;
                    projectorMover.rotation = hitTransform.rotation;
                    JoystickPivot.localRotation = Quaternion.identity;
                    angleLimiter = false;

                }

                //if our velocity goes over a certain threshold our user has likely gotten annoyed and we turn angle limiting off
                //see angle limiting below
                float velocityPause = 1;
                if (velocityCancel)
                {
                    velocityPause = Mathf.Clamp(velocity, 0, 1);
                }


                if (angleLimiter)
                {
                    ///If angle limiting snaps and does not look like it is working, this is likely due to
                    ///a rapid positon jump when we first start moving the mover.
                    /// in this case we may want to turn of the speed shut off for angle limiting with the
                    /// first second or so of use

                    var distanceDiff = Vector3.Distance(moverInitPosition, hitTransform.position);
                    var rotationDiff = Quaternion.Angle(moverInitRotation, hitTransform.rotation);

                    //get the normalized values of the distance and rotation diferences based on inspector thresholds
                    float norm_DistanceDiff = (distanceDiff - angleLimit_Distance_Threshold_Start) /
                                              (angleLimit_Distance_Threshold_End - angleLimit_Distance_Threshold_Start);
                    float norm_RotationDiff = (rotationDiff - angleLimit_Angle_Threshold_Start) /
                                              (angleLimit_Angle_Threshold_End - angleLimit_Angle_Threshold_Start);
                    //get the highest of those 2 values
                    float norm_threshold = Mathf.Max(norm_DistanceDiff, norm_RotationDiff);


                    //get the target rotation between the initial rotation, and the new hit, based on the threshold
                    var targetRotation_mover = Quaternion.Lerp(
                        moverInitRotation, hitTransform.rotation, norm_threshold);
                    var targetRotation_Joystick = Quaternion.Lerp(
                        JoysticInitRotation, Quaternion.identity, norm_threshold);

                    //set the rotations each frame
                    hitTransform.rotation = Quaternion.Lerp(projectorMover.rotation, targetRotation_mover,
                        angleSmoothing_Speed * velocityPause);
                    joysticCurrentRot = Quaternion.Lerp(JoystickPivot.localRotation, targetRotation_Joystick,
                        angleSmoothing_Speed * velocityPause);

                    //we always set the position pure

                }

                if (!angleLimiter && angleSmoothing)
                {
                    hitTransform.rotation = Quaternion.Lerp(projectorMover.rotation, hitTransform.rotation,
                        angleSmoothing_Speed * velocityPause);
                    joysticCurrentRot = Quaternion.Lerp(JoystickPivot.localRotation, Quaternion.identity,
                        angleSmoothing_Speed * velocityPause);
                }

                //THis is where we set our final mover position
                projectorMover.position = hitTransform.position;
                projectorMover.rotation = hitTransform.rotation;
                JoystickPivot.localRotation = joysticCurrentRot;

                //capture result after all movement calculations have been captured on the mover
                //for use next frame
                moverPreviousPosition = projectorMover.position;
            }
        }


        //ANGLE SCANNING MVP2
        private List<RaycastHit> CastFromMesh(MeshFilter scannerMesh, Mesh hitMesh, LayerMask mask)
        {

            Vector3[] vertices = scannerMesh.mesh.vertices;
            Vector3[] normals = scannerMesh.mesh.normals;


            List<RaycastHit> hitList = new List<RaycastHit>();

            for (int i = 0; i < vertices.Length; i++)
            {
                RaycastHit thisHit = new RaycastHit();
                Vector3 vertex = scannerMesh.transform.TransformPoint(vertices[i]);
                Vector3 normal = scannerMesh.transform.TransformDirection(normals[i]);
                if (Physics.Raycast(vertex, normal, out thisHit, float.MaxValue, mask))
                {
                    hitList.Add(thisHit);
                    var result = GetTransformFromHit(thisHit, hitMesh, thisHit.transform);
                    result.raycastOrigin = vertex;
                    result.raycastAngle = normal;
                    scannerResults.Add(result);
                }
                else
                {
                    scannerResults.Add(missedRaycast(vertex, normal));
                }

            }

            //Debug.Log("collected Hits: "+ hitList.Count +" Collected Misses:" + scannerResults.Count);
            return hitList;
        }

        //ANGLE SCANNING MVP2
        private Quaternion GetAverageAngle(List<VirtualTransform> virtualTransformList)
        {
            Quaternion averageQuaternion = Quaternion.identity;
            int count = 0;

            foreach (VirtualTransform virtualTransform in virtualTransformList)
            {
                if (virtualTransform.madeContact)
                {
                    averageQuaternion.x += virtualTransform.rotation.x;
                    averageQuaternion.y += virtualTransform.rotation.y;
                    averageQuaternion.z += virtualTransform.rotation.z;
                    averageQuaternion.w += virtualTransform.rotation.w;
                    count++;
                }
            }

            if (count > 0)
            {
                averageQuaternion.x /= count;
                averageQuaternion.y /= count;
                averageQuaternion.z /= count;
                averageQuaternion.w /= count;
            }

            return averageQuaternion;
        }



        //ANGLE SCANNING MVP2
        //returns empty raycast data for missed ray debugging
        private VirtualTransform missedRaycast(Vector3 origin, Vector3 angle)
        {
            VirtualTransform miss = new VirtualTransform();
            miss.rotation = Quaternion.identity;
            miss.position = Vector3.zero;
            miss.distance = 1;
            miss.madeContact = false;
            miss.raycastOrigin = origin;
            miss.raycastAngle = angle;
            return miss;
        }

        ///this method gets a weighted average of the normal of a triangle, weighted based on the hits proximity to each vertex.
        private VirtualTransform GetTransformFromHit(
            RaycastHit hit,
            Mesh hitMesh,
            Transform hitTransform,
            Vector3 fromRotation = default(Vector3))
        {
            if (fromRotation == Vector3.zero)
            {
                fromRotation = Vector3.up;
            }

            int triangleIndex = hit.triangleIndex;
            Vector3[] vertices = hitMesh.vertices;
            int[] triangles = hitMesh.triangles;
            Vector3 p1 = hitTransform.TransformPoint(vertices[triangles[triangleIndex * 3]]);
            Vector3 p2 = hitTransform.TransformPoint(vertices[triangles[triangleIndex * 3 + 1]]);
            Vector3 p3 = hitTransform.TransformPoint(vertices[triangles[triangleIndex * 3 + 2]]);

            Vector3 barycentricCoord = GetBarycentricCoordinates(hit.point, p1, p2, p3);

            Vector3 surfaceNormal = hit.transform.TransformDirection(
                    hitMesh.normals[triangles[triangleIndex * 3]] * barycentricCoord.x
                    + hitMesh.normals[triangles[triangleIndex * 3 + 1]] * barycentricCoord.y
                    + hitMesh.normals[triangles[triangleIndex * 3 + 2]] * barycentricCoord.z)
                .normalized; // Move the object to the surface point with the correct normal angle


            _returnTransform.position = hit.point;
            _returnTransform.rotation = Quaternion.FromToRotation(fromRotation, surfaceNormal);
            _returnTransform.normal = hit.normal;
            _returnTransform.surfaceNormal = surfaceNormal;
            //normal = hit.normal;
            _currNormal = surfaceNormal;
            _returnTransform.distance = hit.distance;
            _returnTransform.madeContact = true;

            return _returnTransform;
        }


        // Calculate barycentric coordinates for a point in a triangle
        private Vector3 GetBarycentricCoordinates(Vector3 p, Vector3 p1, Vector3 p2, Vector3 p3)
        {
            Vector3 v0 = p2 - p1;
            Vector3 v1 = p3 - p1;
            Vector3 v2 = p - p1;

            float d00 = Vector3.Dot(v0, v0);
            float d01 = Vector3.Dot(v0, v1);
            float d11 = Vector3.Dot(v1, v1);
            float d20 = Vector3.Dot(v2, v0);
            float d21 = Vector3.Dot(v2, v1);

            float denominator = d00 * d11 - d01 * d01;

            float v = (d11 * d20 - d01 * d21) / denominator;
            float w = (d00 * d21 - d01 * d20) / denominator;
            float u = 1 - v - w;

            return new Vector3(u, v, w);
        }

        private void LogMesh(Mesh incMeshDataRef, bool init)
        {
            ///TestRenderer sits in scene and is used
            if (!init)
            {
                _visualizeBodyCollider_VisualizerObject = new GameObject("Proxy Geo Debug");
                _visualizeBodyCollider_VisuzlizerRenderer =
                    _visualizeBodyCollider_VisualizerObject.AddComponent<SkinnedMeshRenderer>();
            }

            _visualizeBodyCollider_VisuzlizerRenderer.sharedMesh = new Mesh();
            _visualizeBodyCollider_VisuzlizerRenderer.sharedMesh.Clear();
            _visualizeBodyCollider_VisuzlizerRenderer.sharedMesh.vertices = incMeshDataRef.vertices;
            _visualizeBodyCollider_VisuzlizerRenderer.sharedMesh.triangles = incMeshDataRef.triangles;
            _visualizeBodyCollider_VisuzlizerRenderer.sharedMesh.uv = incMeshDataRef.uv;
            _visualizeBodyCollider_VisuzlizerRenderer.sharedMesh.RecalculateNormals();

        }

        //gizmo debugging
        private void OnDrawGizmos()
        {
            ///This draws out the scanner Result raycasts
            if (scannerResults.Count > 0 && angleScanning && angleScanning_Debug)
            {

                foreach (var result in scannerResults)
                {
                    Color rayColor = new Color();
                    if (result.madeContact)
                    {
                        rayColor = Color.green;
                    }
                    else
                    {
                        rayColor = Color.red;
                    }

                    Debug.DrawRay(result.raycastOrigin, result.raycastAngle * result.distance, rayColor, 0, true);
                }
            }
        }

        //ANGLE SCANNING MVP2
        private void ObjectDrawScannerResults()
        {
            ///moves visualizers across the surface of the target mesh based on the
            ///scanner results colleceted in CastFromMesh()

            //if AngleScanning is enabled (there should also be a debugging toggle here
            if (angleScanning && angleScanning_Debug)
            {
                if (angleScan_VisualizerList.Count == 0)
                {
                    //Initialize Visualizer Objects
                    foreach (var result in scannerResults)
                    {
                        var visual = Instantiate(angleScan_VisualizerPrefab.transform, Vector3.zero,
                            Quaternion.identity);
                        Material mat = new Material(angleScan_VisualizerShader);
                        var rend = visual.GetComponent<Renderer>();
                        rend.material = mat;
                        angleScan_VisualizerList.Add(visual);
                    }
                }
                else
                {
                    int i = 0;
                    foreach (var result in scannerResults)
                    {
                        if (result.madeContact)
                        {
                            angleScan_VisualizerList[i].transform.position = result.position;
                            angleScan_VisualizerList[i].transform.rotation = result.rotation;
                            var rend = angleScan_VisualizerList[i].GetComponent<Renderer>();

                            float angledif = Quaternion.Angle(activeRaycast.rotation, result.rotation);
                            float normdif = 1 - (angledif / 90f);
                            rend.material.SetFloat("_Color", normdif);

                        }
                        else
                        {

                        }

                        i++;
                    }
                }

                if (AngleScan_AverageAngleVisualizer == null)
                {
                    var visual = Instantiate(angleScan_VisualizerPrefab.transform, Vector3.zero, Quaternion.identity);
                    Material mat = new Material(angleScan_VisualizerShader);
                    var rend = visual.GetComponent<Renderer>();
                    rend.material = mat;
                    visual.transform.localScale = new Vector3(.5f, 1f, .5f);
                    AngleScan_AverageAngleVisualizer = visual.transform;
                }
                else
                {
                    AngleScan_AverageAngleVisualizer.position = activeRaycast.position;
                    AngleScan_AverageAngleVisualizer.rotation = AngleScan_averageAngle;

                    var rend = AngleScan_AverageAngleVisualizer.GetComponent<Renderer>();
                    float angledif =
                        Quaternion.Angle(activeRaycast.rotation, AngleScan_AverageAngleVisualizer.rotation);
                    float normdif = 1 - (angledif / 90f);
                    rend.material.SetFloat("_Color", normdif);

                }
            }
            else
            {
                if (angleScan_VisualizerList.Count > 0)
                {
                    foreach (var thing in angleScan_VisualizerList)
                    {
                        Destroy(thing.gameObject);

                    }

                    angleScan_VisualizerList = new List<Transform>();
                }

                if (AngleScan_AverageAngleVisualizer != null)
                {
                    Destroy(AngleScan_AverageAngleVisualizer);
                }
            }
        }

    }
}

