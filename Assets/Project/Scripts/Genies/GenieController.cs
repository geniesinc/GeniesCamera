using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARKit;


// Locomote the Genie and apply user mocap as input
public class GenieController : MonoBehaviour
{

    // This value cross-fades (0-1) to determine how much influence the "Emote" animation
    // has as compared to the user's mocap input. This is for "blending" between the
    // Emote manager's PlayableGraph system and our own Animator system here on this component.
    public float EmoteBlendWeight;

    // Controls PlayableGraph animations, as compared to this class's Animator
    [SerializeField] private EmoteManager _emoteManager;
    // This is the little icon that lives in the menu
    [SerializeField] private Texture _thumbnailTexture;
    // OFfline Genies have this specified, otherwise it gets auto-created.
    [SerializeField] private Transform eyesMidPoint;
    // Offline Genies (Barbie, Skeletor) have this specified, otherwise it's 100.
    [SerializeField] private float _blendshapeScale = USER_GENIE_BLENDSHAPE_SCALE;
    // These are used on Bishop Briggs to prevent the Kubrick stare
    [SerializeField] private float _lookInMaxVal = 1f;
    [SerializeField] private float _lookOutMaxVal = 1f;
    [SerializeField] private float _lookUpMaxVal = 1f;
    [SerializeField] private float _lookDownMaxVal = 1f;
    // Prefabs we spawn on the fly
    [SerializeField] private TeleportParticlesController _teleportParticlesPrefab;
    [SerializeField] private PreloadFxController _preloadFxPrefab;


    // Genies' emotes are accessed by the Main Menu and the Emote Wheel menu, so
    // we will expose accessors here:
    public EmoteManager EmoteManager { get { return _emoteManager; } }
    public AnimationClip LegacyWaveAnim { get { return _geniesManager.LegacyWaveAnim; } }
    public AnimationClip LegacyVogueAnim { get{ return _geniesManager.LegacyVogueAnim; } }
    public AnimationClip LegacyPeaceAnim { get{ return _geniesManager.LegacyPeaceAnim; } }
    // Emote Manager needs access to Animator to crossfade it with the PlayableGraph.
    public Animator Animator { get { return _animator; } }
    // Accessed by the Main Menu for the Genie thumbnail on her selection button
    public Texture ThumbnailTexture { get { return _thumbnailTexture; } }
    // Accessor for TeleportParticles and LoadingCloud/PreloadFx to know what
    // scale to match. Also for Manager to know what scale a replacement Genie
    // would be to match.
    public float CurrScale { get { return transform.localScale.y; } }
    // This is used to calculate where fx go while the Genie is loading,
    // as well as how big the User Genie should be when she initially spawns
    // within the Camera frustum.
    // We use a default estimated height here so that the preload particles
    // know approximately where to exist.
    public float CurrHeight
    {
        get
        {
            // If there is no Genie yet, we can estimate its size based on lore.
            return _genieCollider == null ? CurrScale * GeniesManager.GENIE_HEIGHT_FOR_XR :
                    // We have to scale the box collider size, because it does not
                    // update along with the Transform chain.
                    _genieCollider.transform.lossyScale.y * _genieCollider.height;
        }
    }
    // User genies are dynamically created at runtime, and thus
    // need special handling to set their references as compared to
    // offline genies, which are manual prefabs and have their references already set.
    public bool IsUserGenie { get; set; } = false;
    public bool IsGenieSetUp { get; private set; } = false;


    // This is the Genie's dimensions (for creating a collider)
    // We should calculate this dynamically, but not today!
    const float GENIE_HEIGHT = 1.6f;
    const float GENIE_WIDTH = 0.38f;
    const float GENIE_DEPTH = 0.38f;
    // The smollest scalar we will allow the User to apply to the Genie
    const float GENIE_MIN_SCALE = 0.1f;
    // What is the pixel-to-meter ratio when a Genie is standing 2m away?
    const float METERS_TO_PIXELS = 0.0005f;
    // What is the pixel-to-degree ratio when you drag your finger across the screen?
    const float PIXELS_TO_DEGREES = -0.5f;
    // Blendshapes are typically normalized (0-1), but our company does 0-100..
    const float USER_GENIE_BLENDSHAPE_SCALE = 100f;
    // How many degrees does our Yaw animation cover?
    const float YAW_ANIM_TOTAL_ANGLE = 90f;
    // How long does our Yaw anim last? To be fair, we could query it!
    const float YAW_ANIM_DURATION = 1.567f;
    // name of param in anim controller
    const string PARAM_WHICH_ANIM_IDX = "GenieAnim";
    // name of trigger in anim controller
    const string TRIGGER_JUMP = "Jump"; // 1.667 seconds, btw!
    // name of param in anim controller
    const string WALK_RUN_BLEND = "WalkRunBlend";
    const string WALK_RUN_SPEED = "WalkRunSpeed";
    // how fast do we translate the genie to keep up w/ the walk anim?
    const float TRANSLATION_SPEED = 2;
    // So that we can compare floats 
    const float EPSILON = 0.0001f;
    // how many seconds do we need to wait before we start mirroring the input?
    const float MIRROR_MOCAP_THRESHOLD_TIME = 0.25f;
    // anim playback speed should not go beneath this threshold
    const float MIN_WALK_RUN_SPEED = 0.5f;


    // Is the user touching the genie with finger input?
    private bool _isUserTouchingMe = false;
    // Is the User yawing the genie with finger input (i.e. moving finger left/right)
    private bool _isUserYawing = false;
    // Is the Genie currently playing an Emote from the playable graph?
    private bool _isEmoting { get { return EmoteBlendWeight > 0; } }
    // Tracking our preload particle effects (they hide the naked genie while its loading)
    private PreloadFxController _preloadFxInstance;
    // List of renderers that have blendhsapes we need to animate
    private SkinnedMeshRenderer[] _skinnedMeshRenderers;
    // This plane helps us determine if the avatar is facing the same direciton
    // as the camera (Genie left = user left, Genie Right = user right).
    // Depending on the answer to this, we need to invert mocap ("mirror")
    // so that the user always feels like "when I lean left, Genie leans screen-left".
    private Plane _avatarPlane = new Plane();
    // whether we need to flip the input left/right
    private bool _doMirrorMocap = false;
    // for counting from the threshold time down to zero
    private float _doMirrorTimer = 0;
    // These quats store the original emote animation pose, to be blended with the
    // user's mocap input as they transition in and out of an isEmoting state.
    private Quaternion _animHeadRot, _animNeckRot, _animChestRot, _animSpine2Rot, _animSpine1Rot;
    // For offline/legacy Genies, because of how shoes were rigged at that time
    private Vector3 _platformShoeHeight = Vector3.zero;


    // These are set by Initializer:
    private GeniesManager _geniesManager;
    private CameraManager _cameraManager;
    private FacesManager _facesManager;
    private JoystickController _joystickController;
    // Auto-created during setup, or found on offline genies by searching tree
    private Animator _animator;
    private CapsuleCollider _genieCollider;
    // Auto created during setup, gives input we can use to move the Genies head like a puppet
    private GenieFaceController _faceController;
    // Can we safely use this class?
    private bool _didInitialize = false;


    public void Initialize(GeniesManager geniesManager,
                            CameraManager cameraManager,
                            JoystickController joystickController,
                            FacesManager facesManager,
                            bool isUserGenie)
    {
        if(_didInitialize)
        {
            Debug.LogError($"You are trying to initialize {gameObject.name} multiple times.", gameObject);
            return;
        }

        _geniesManager = geniesManager;
        _cameraManager = cameraManager;
        _facesManager = facesManager;
        _joystickController = joystickController;
        IsUserGenie = isUserGenie;

        _emoteManager = gameObject.AddComponent<EmoteManager>();
        _emoteManager.Initialize(this);

        _didInitialize = true;
    }
    
    private void OnDisable()
    {
        if (_preloadFxInstance != null)
        {
            Destroy(_preloadFxInstance.gameObject);
        }
    }

    private void Update()
    {
        if (!_didInitialize)
        {
            return;
        }

        // While we wait for the Genie to get setup, adjust the Scale to fit in frame.
        // This way, the Genie will be a nice scale when loaded, and also its PreloadFx
        // cloud will scale to a representative size as well.
        // "if is preloading..."
        if (_preloadFxInstance != null && !IsGenieSetUp)
        {
            FitGenieInFrame();
        }

        // I HAVE NO IDEA WHAT IS HAPPENING HERE. Local position and rotation are getting
        // zeroed out while the Genie is loading but "hidden" for many frames (like, ~100).
        // When the Genie appears, it stops. This happens inconsistently, as in, some launches
        // it doesn't happen and some launches it does.
        if (_animator != null &&
            // If your offset has changed (accounting for float precision)
            Vector3.Magnitude(_platformShoeHeight - _animator.transform.localPosition) > EPSILON &&
            !_isEmoting)
        {
            Debug.LogWarning($"Genie Y-offset is {_animator.transform.localPosition} but should be {_platformShoeHeight}.");
            _animator.transform.localPosition = _platformShoeHeight;
        }
    }

    private void FixedUpdate()
    {
        if (!_didInitialize)
        {
            return;
        }

        // We don't have a joystick in ScreenSpace.
        if (!_cameraManager.IsScreenspace && !_isUserYawing)
        {
            ProcessJoystickInput();
        }
    }

    // In addition to being called immediately for "offline genies", this
    // is also called by the UserGenieLoader as part of the login workflow.
    public void SetupGenie()
    {        
        if (IsGenieSetUp)
        {
            Debug.LogWarning($"You are setting up {gameObject.name} twice...! D:", gameObject);
        }

        // Link to the animator component which was created by the UserGenieLoader
        _animator = transform.GetComponentInChildren<Animator>();
        if (_animator == null)
        {
            Debug.LogError($"Could not find animator for {transform.name}.");
            return;
        }

        // We only want this on if we're emoting with root motion
        _animator.applyRootMotion = false;

        // Legacy genies have this because of the way heeled shoes work
        _platformShoeHeight = _animator.transform.localPosition;

        // Link Skinned Mesh renderers
        _skinnedMeshRenderers = transform.GetComponentsInChildren<SkinnedMeshRenderer>();

        // Setup the face for blendshape animation via ARKit
        if (_faceController == null)
        {
            _faceController = gameObject.AddComponent<GenieFaceController>();
            _faceController.Initialize(_skinnedMeshRenderers,
                                      (ARKitFaceSubsystem)_facesManager.subsystem,
                                      _blendshapeScale,
                                      _lookInMaxVal, _lookOutMaxVal, _lookUpMaxVal, _lookDownMaxVal);
        }

        // Ensure children are on the avatar layer 
        transform.SetLayersRecursively((int)Layers.Avatar);

        // Unfortunately, _animator.GetBoneTransform(HumanBodyBones.LeftEye) returns null
        // so we have to go mideavl on this one.
        if (eyesMidPoint == null)
        {
            Transform nose = transform.FindChildRecursively("NoseBind");
            Transform leftEye = transform.FindChildRecursively("LeftEyeSocket");
            if (nose != null && leftEye != null)
            {
                eyesMidPoint = new GameObject().GetComponent<Transform>();
                eyesMidPoint.parent = _animator.GetBoneTransform(HumanBodyBones.Head);
                eyesMidPoint.localRotation = Quaternion.identity;
                eyesMidPoint.localPosition = Vector3.zero;
                eyesMidPoint.name = "EyesMidpoint";
                Vector3 eyesMidPointPos = nose.position;
                eyesMidPointPos.y = leftEye.position.y;
                eyesMidPoint.transform.position = eyesMidPointPos;
            }
            else
            {
                Debug.LogWarning($"Could not define EyesMidPoint for {transform.name}.");
            }
        }

        // HACK: At the time of this code, there is an Editor bug with our main
        // Genie skin shader. It renders the genie solid black-- but this is alleviated
        // if there is literally any Geo in the camera frustum. So...
    #if UNITY_EDITOR
        var debugSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        debugSphere.transform.localScale = Vector3.one * 0.0001f;
        debugSphere.transform.position = eyesMidPoint.position;
        debugSphere.transform.parent = eyesMidPoint;
        debugSphere.name = "RenderBugFixer";
    #endif

        // Setup the box collider so we can touch the Genie
        _genieCollider = _animator.GetComponent<CapsuleCollider>();
        if (_genieCollider == null)
        {
            _genieCollider = _animator.gameObject.AddComponent<CapsuleCollider>();
            _genieCollider.radius = Mathf.Max(GENIE_DEPTH, GENIE_WIDTH) / 2f;
            _genieCollider.height = GENIE_HEIGHT;
            _genieCollider.center = new Vector3(0, GENIE_HEIGHT / 2f, 0);
        }

        // HACK: Setup pretty shaders that aren't distributed directly with
        // this project but are available through external packages under separate license.
        // This way our offline Genies will look as nice as our dynamically spawned User Genie.
        if (!IsUserGenie)
        {
            var megaStylizer = Shader.Find("Shader Graphs/MegaStylizer_5.1_Lite");
            if(megaStylizer != null)
            {
                var allRenderers = GetComponentsInChildren<Renderer>();
                foreach (var renderer in allRenderers)
                {
                    if (renderer is SkinnedMeshRenderer || renderer is MeshRenderer)
                    {
                        foreach (var mat in renderer.materials)
                        {
                            if (mat != null)
                            {
                                mat.shader = megaStylizer;
                                mat.SetFloat("_Flatness", 0.5f);
                            }
                        }
                    }
                }   
            }
        }

        // Swap preload fx for user genie
        if (_preloadFxInstance != null)
        {
            if (_geniesManager.IsGameReady)
            {
                PositionGenieAtParticleFx();
            }
            _preloadFxInstance.DestroyPretty();
        }

        // Get the fx, get the lighting, face the user
        TeleportGenie(transform.position);

        // Store setup state
        IsGenieSetUp = true;

        // Be cute :nailcare:
        EmoteManager.LegacyPeace();

        // This will set up the playable graph, and if it's the first time, it
        // will set a timer and teach you to double-tap to place your now-visible genie.
        _geniesManager.ReportGenieIsSetUp(this);
    }

    // This is only called after the floor is found and the tutorial is over.
    public void SetupUserGeniePreload()
    {
        // Sanity check
        if (_preloadFxInstance != null)
        {
            Debug.LogError("You're trying to make preloadFx but they already exist. App state error?");
            return;
        }

        // Create a "Loading..." graphic and place it where the genie goes.
        _preloadFxInstance = Instantiate(_preloadFxPrefab);
        // @GenieController: To know where it goes and how big it should be
        // @InputManager: To know if we are in screen space
        _preloadFxInstance.Initialize(this, _cameraManager);

        // Do we need to hide the fx while we look for the floor?
        if (!_geniesManager.IsGameReady)
        {
            _preloadFxInstance.SetFxVisibility(false);
        }
    }

    private void FitGenieInFrame()
    {
        Vector3 targetPos = _geniesManager.GetGeniePlacementPositionInCameraFrustum();
        float targetHeight = _geniesManager.GetMinHeightToFitInFrame(targetPos);
        targetHeight = Mathf.Lerp(CurrHeight, targetHeight, Time.deltaTime * 2f);
        SetHeight(targetHeight);
    }

    private void ProcessJoystickInput()
    {
        // Get the current input weight.
        float joystickInputWeight = _joystickController.InputVector.magnitude;
        // If they're using the joystick, apply the current input weight.
        if (_joystickController.IsUsingJoystick && joystickInputWeight > 0)
        {
            // Get the normalized input in screen space
            Vector3 inputVector = new Vector3(_joystickController.InputVector.x,
                                              0f,
                                              _joystickController.InputVector.y).normalized;
            // Apply input relative to the camera
            Vector3 rotatedVector =
                    _cameraManager.ActiveCamera.transform.rotation.ExtractYaw() * inputVector;

            // Remap the speed of the walk/run animation between a legal range
            var walkRunSpeed = Mathf.Lerp(MIN_WALK_RUN_SPEED, 1, joystickInputWeight);
            _animator.SetFloat(WALK_RUN_SPEED, walkRunSpeed);
            // Blend between the walk and run animations themselves w/ natural input
            _animator.SetFloat(WALK_RUN_BLEND, joystickInputWeight);
            //Debug.Log($"Input: {joystickInputWeight}, Speed: {walkRunSpeed}, Blend: {walkRunBlend}");

            // Move the genie based on her scale, the joystick input, and the camera rotation.
            transform.position += rotatedVector * TRANSLATION_SPEED * joystickInputWeight * CurrScale * Time.deltaTime;

            // Target rotation should check if look rotation viewing vector is smaller/larger than zero,
            // or it will make a noisy warning (and have no visible result).
            Quaternion targetRotation = rotatedVector.magnitude > 0 ? Quaternion.LookRotation(rotatedVector, Vector3.up) :
                                                                      Quaternion.identity;
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 10f);

            // Update the animation clip if you haven't already
            if (!_geniesManager.IsLocomoting)
            {
                SetGenieAnim(GenieAnimIndex.WalkRunBlend);
                _geniesManager.IsLocomoting = true;
            }
        }
        else if (_geniesManager.IsLocomoting)
        {
            //Stop Animation
            SetGenieAnim(GenieAnimIndex.Idle);
            _geniesManager.IsLocomoting = false;
        }
    }

    public Vector3 GetPreloadFxPosition()
    {
        if (_geniesManager == null)
        {
            return Vector3.zero;
        }

        Vector3 targetPos = _geniesManager.GetGeniePlacementPositionInCameraFrustum();
        Vector3 verticalOffset = Vector3.up * CurrHeight / 2f;
        return targetPos + verticalOffset;
    }

    public void OnGameReady_ShowPreloadFx()
    {
        if (_preloadFxInstance != null)
        {
            _preloadFxInstance.SetFxVisibility(true);
        }
    }

    public bool IsVisibleInCameraFrustum()
    {
        // use Camera.WorldToViewportPoint, check if X and Y are within 0…1 range AND check Z> 0
        // if Z is <= 0 it means the point is behind the camera.
        Vector3 viewportPoint = _cameraManager.ActiveCamera.WorldToViewportPoint(_genieCollider.center);
        return viewportPoint.x >= 0 && viewportPoint.x <= 1 &&
                    viewportPoint.y >= 0 && viewportPoint.y <= 1 &&
                    viewportPoint.z >= 0;
    }

    // At the given position, how much do we need to scale this Genie so that they are not bigger
    // than the Camera Frustum?
    public void SetHeight(float targetHeight)
    {
        SetScale(CurrScale * (targetHeight / CurrHeight));
    }

    public void GreetUser()
    {
        transform.rotation = GetLookAtCameraYaw();
        EmoteManager.LegacyPeace();
        MakeTeleportParticles(transform.position);
    }

    public void Wave()
    {
        EmoteManager.LegacyWave();
    }

    private void PositionGenieAtParticleFx()
    {
        if (_cameraManager.IsScreenspace)
        {
            transform.position = Vector3.zero;
            transform.rotation = Quaternion.identity;
            SetScale(1);
        }
        else
        {
            // Put the genie on the floor below the effect
            Vector3? floorPoint = GetFloorBelowWorldPoint(_preloadFxInstance.transform.position);
            if (floorPoint != null)
            {
                transform.position = floorPoint.Value;
            }

            // Floor is not visible/accessible
            if (!IsVisibleInCameraFrustum())
            {
                transform.position = _geniesManager.GetGeniePlacementPositionInCameraFrustum();
            }

            // Look at the camera bc why not
            transform.rotation = GetLookAtCameraYaw();
        }
    }

    // Beneath any 3D point in space, there may be a floor. Raycast down
    // and get that floor point, if it exists. This is used by the Genie's
    // PreloadFx cloud, to make the Genie stand beneath the cloud (poof!).
    private Vector3? GetFloorBelowWorldPoint(Vector3 worldPoint)
    {
        RaycastHit hit;
        if (Physics.Raycast(worldPoint,
                            Vector3.down,
                            out hit,
                            _cameraManager.ActiveCamera.farClipPlane * 2f,
                            1 << (int)Layers.SpatialMesh) &&
            hit.normal.CanPlaceOnSurfaceWithNormal())
        {
            return hit.point;
        }

        return null;
    }

    public void ApplyFacialMocap(ARFace lastSeenFace)
    {
        // Sanity-check, or perhaps you're in Editor.
        if (lastSeenFace != null)
        {
            _faceController.ApplyMocapData(lastSeenFace);
        }

        if (_isUserTouchingMe || _geniesManager.ForceLookAtCamera)
        {
            _faceController.ApplyLookAtCameraGaze(eyesMidPoint);
        }
        else
        {
            _faceController.ApplyCurrentGaze();
        }
    }

    public void CleanUpUserGenie()
    {
        //Assings all needed variables looking at the children.
        // Needed when loading Genies at runtime.
        _animator = null;
        eyesMidPoint = null;
        _genieCollider = null;
        _skinnedMeshRenderers = null;

        // Destroy in reverse bc I can't remember if childCount gets called
        // once per loop or is stored at initial call, lol.
        for (int i = transform.childCount; i > 0; i--)
        {
            Destroy(transform.GetChild(i - 1).gameObject);
        }

        IsGenieSetUp = false;
    }

    public void CacheAnimationPose()
    {
        // Get the joint rotation values native to the animation/emote that is currently playing
        _animHeadRot = _animator.GetBoneTransform(HumanBodyBones.Head).localRotation;
        _animNeckRot = _animator.GetBoneTransform(HumanBodyBones.Neck).localRotation;
        _animChestRot = _animator.GetBoneTransform(HumanBodyBones.UpperChest).localRotation;
        _animSpine2Rot = _animator.GetBoneTransform(HumanBodyBones.Chest).localRotation;
        _animSpine1Rot = _animator.GetBoneTransform(HumanBodyBones.Spine).localRotation;
    }

    private bool ShouldMirrorMocap()
    {
        // Check if avatar has their back to the camera. If not, we
        // will need to flip the yaw (look l/r) and roll (tilt l/r) rotations.
        _avatarPlane.SetNormalAndPosition(transform.forward, transform.position);

        // GetSide(camera): Returns True if camera face and Genie face are looking at eachother.
        //                  Returns False if camera face and Genie face are looking in the same direction.
        bool areLookingAtEachOther = _avatarPlane.GetSide(_cameraManager.ActiveCamera.transform.position);

        // So, when camera and genie are "looking at eachother", like humans look into a mirror,
        // we will want to flip the direction of the input so that user's Left is Genie's Left, etc.
        return areLookingAtEachOther;
    }

    private Quaternion HandleMocapMirroring(Quaternion mocapRotation)
    {
        bool shouldMirrorMocap = ShouldMirrorMocap();

        // Handle transition between raw and mirrored states, otherwise
        // the Genie will snap. To replicate this, you could just yaw
        // the genie between facing the camera and facing away (180 deg)
        if (_doMirrorMocap != shouldMirrorMocap)
        {
            _doMirrorTimer += Time.deltaTime;

            if (_doMirrorTimer >= MIRROR_MOCAP_THRESHOLD_TIME)
            {
                // Update the state
                _doMirrorMocap = shouldMirrorMocap;
                _doMirrorTimer = 0f;
                // Apply final state's math
                if (_doMirrorMocap)
                {
                    mocapRotation = mocapRotation.GetMirror();
                }
            }
            else
            {
                // Don't update the state;
                // Apply the transitional state's math
                float t = _doMirrorTimer / MIRROR_MOCAP_THRESHOLD_TIME;

                Quaternion raw = mocapRotation;
                Quaternion mirrored = raw.GetMirror();

                // From current state TO target state
                Quaternion from = _doMirrorMocap ? mirrored : raw;
                Quaternion to = shouldMirrorMocap ? mirrored : raw;

                // This is our final calculation for this frame
                mocapRotation = Quaternion.Slerp(from, to, t);
            }
        }
        // No transition, the Genie is facing the same direction as she was last frame.
        else
        {
            // Apply final mirrored state if needed
            if (_doMirrorMocap)
            {
                mocapRotation = mocapRotation.GetMirror();
            }

            // Keep this ready for next time.
            _doMirrorTimer = 0f;
        }

        return mocapRotation;
    }

    public void ApplySpineMocap(Quaternion mocapRotation)
    {
        // We want to adjust the mocap based on the user's perspective.
        // If I am facing the Genie, as a User, I expect if I tilt to my
        // left, she tilts "screen left". Depenidng on what direction she is
        // facing, "screen left" may be her rig's Left OR Right.
        mocapRotation = HandleMocapMirroring(mocapRotation);

        // This will animate the spine and neck along with the User's head mocap.
        // We are using Slerp to quickly get a "percentage" of influence. So the User's
        // head rotation will impact the Rig's head rotation by 85%, the neck by 30%, etc.                
        var userHeadRot = Quaternion.Slerp(Quaternion.identity, mocapRotation, 0.85f);
        var userNeckRot = Quaternion.Slerp(Quaternion.identity, mocapRotation, 0.3f);
        var userChestRot = Quaternion.Slerp(Quaternion.identity, mocapRotation, 0.25f);
        var userSpine2Rot = Quaternion.Slerp(Quaternion.identity, mocapRotation, 0.25f);
        var userSpine1Rot = Quaternion.Slerp(Quaternion.identity, mocapRotation, 0.15f);

        // If the EmoteManager is currently blending us between the AnimController graph and
        // the Playable Graph, we want these target joint rotations to blend as well.
        var finalHeadRot = Quaternion.Slerp(userHeadRot, _animHeadRot, EmoteBlendWeight);
        var finalNeckRot = Quaternion.Slerp(userNeckRot, _animNeckRot, EmoteBlendWeight);
        var finalChestRot = Quaternion.Slerp(userChestRot, _animChestRot, EmoteBlendWeight);
        var finalSpine2Rot = Quaternion.Slerp(userSpine2Rot, _animSpine2Rot, EmoteBlendWeight);
        var finalSpine1Rot = Quaternion.Slerp(userSpine1Rot, _animSpine1Rot, EmoteBlendWeight);

        // Set joint rotation values based on user head pose, weights, and emote overrides
        _animator.GetBoneTransform(HumanBodyBones.Head).localRotation = finalHeadRot;
        _animator.GetBoneTransform(HumanBodyBones.Neck).localRotation = finalNeckRot;
        _animator.GetBoneTransform(HumanBodyBones.UpperChest).localRotation = finalChestRot;
        _animator.GetBoneTransform(HumanBodyBones.Chest).localRotation = finalSpine2Rot;
        _animator.GetBoneTransform(HumanBodyBones.Spine).localRotation = finalSpine1Rot;
    }

    public void SetTouchedState(bool isUserTouchingMeNew)
    {
        // If you've just stopped touching the Genie
        if (!isUserTouchingMeNew)
        {
            // If you were yawing the Genie with your finger right
            // before this, change the animation from Yaw to Idle
            if (_isUserYawing)
            {
                // If the user is Emoting, let them keep doing that.
                // Otherwise (if they were Yaw-animating) change to idle anim
                if (!_isEmoting)
                {
                    SetGenieAnim(GenieAnimIndex.Idle);
                }
                _isUserYawing = false;
            }
        }
        _isUserTouchingMe = isUserTouchingMeNew;
    }

    public void TranslateGenieY(Vector2 deltaPixels)
    {
        // We have already bastardized TranslateGenieXZ to work in screen space
        // so we can just call that here.
        if (_cameraManager.IsScreenspace)
        {
            TranslateGenieXZ(deltaPixels);
            return;
        }

        // Normal to imaginary plane the user is rubbing against: Imagine
        // a wall in front of the camera (i.e. orthogonal to cam forward)
        Camera cam = _cameraManager.ActiveCamera;
        Vector3 planeNormal = -cam.transform.forward;
        // The Genie (transform) is positioned ON this imaginary plane
        Vector3 pointOnPlane = transform.position;
        Plane raycastCatcherPlane = new Plane(planeNormal, pointOnPlane);

        // Where is the Genie on the screen, currently?
        Vector2 currScreenPointOffset = cam.WorldToScreenPoint(transform.position);
        // Create the ray based on the users touch to cast against
        // the raycastCatcherPlane.
        Ray ray = cam.ScreenPointToRay(currScreenPointOffset + deltaPixels);

        float distance;
        if (raycastCatcherPlane.Raycast(ray, out distance))
        {
            // Where did the user touch?
            Vector3 posOnRaycatcherPlane = ray.GetPoint(distance);
            // Neutralize the X and Z values
            posOnRaycatcherPlane.x = transform.position.x;
            posOnRaycatcherPlane.z = transform.position.z;
            // Move Genie only in Y
            transform.position = posOnRaycatcherPlane;
        }
    }

    public void TranslateGenieXZ(Vector2 deltaPixels)
    {
        Camera cam = _cameraManager.ActiveCamera;

        Vector3 planeNormal = _cameraManager.IsScreenspace ? -cam.transform.forward : Vector3.up;
        Vector3 pointOnPlane = new Vector3(0,
                                  _cameraManager.IsScreenspace ? 0 : transform.position.y,
                                  _cameraManager.IsScreenspace ? transform.position.z : 0);
        Plane raycastCatcherPlane = new Plane(planeNormal, pointOnPlane);

        Vector2 currScreenPointOffset = cam.WorldToScreenPoint(transform.position);
        Ray ray = cam.ScreenPointToRay(currScreenPointOffset + deltaPixels);

        float distance;
        if (raycastCatcherPlane.Raycast(ray, out distance))
        {
            transform.position = ray.GetPoint(distance);
            /*Debug.Log("Plane Normal: " + planeNormal + ", Point: " + pointOnPlane);
            Debug.Log($"Move to point on ray at {distance}m distance: {transform.position}");
            Debug.DrawRay(ray.origin, ray.direction, Color.blue, 0.5f);*/
        }
    }

    public void ScaleGenieByPixelDelta(float deltaPixels, Vector2 pivotPointScreen)
    {
        float newScale = CurrScale +
            (deltaPixels * METERS_TO_PIXELS * Vector3.Distance(_cameraManager.ActiveCamera.transform.position,
                                                                transform.position));

        newScale = Mathf.Max(GENIE_MIN_SCALE, newScale);

        // If you're in world space, scaling from her feet is fine.
        // But if you're in screen space, scale AROUND the pivot point for a comfy UX.
        if (_cameraManager.IsScreenspace)
        {
            // Convert screen touch point to world point
            Ray screenPointRay = _cameraManager.ActiveCamera.ScreenPointToRay(pivotPointScreen);
            Vector3 cameraPosAtCharHeight = screenPointRay.origin;
            cameraPosAtCharHeight.y = transform.position.y;
            float flatDistance = Vector3.Distance(transform.position, cameraPosAtCharHeight);
            Vector3 pivotPointWorld = screenPointRay.origin + (screenPointRay.direction * flatDistance);
            // How far is the genie from our worldspace touch point
            Vector3 genieOffsetFromScalePivot = transform.position - pivotPointWorld;
            // How much are we scaling this frame
            float deltaScaleRatio = newScale / CurrScale;
            // Our current position is equal to our touch input worldspace point PLUS the genie offset.
            // Our NEW position would be our touch input worldspace point PLUS a weighted offset.
            // The original genie offset is weighted by how much we are actually scaling this frame.
            transform.position = pivotPointWorld + (genieOffsetFromScalePivot * deltaScaleRatio);
        }

        SetScale(newScale);
    }

    public void SetScale(float newScale)
    {
        newScale = newScale < GENIE_MIN_SCALE ? GENIE_MIN_SCALE : newScale;
        transform.localScale = Vector3.one * newScale;
    }

    // Rotate the Genie on her Z Axis, totally stupid and illegal in 3D but cute in 2D!
    public void TwistGenie(float deltaAngle, Vector2 pivotPointScreen)
    {
        if (_cameraManager.IsScreenspace)
        {
            Vector3 pivotPointWorld = _cameraManager.ScreenspaceCamera.ScreenToWorldPoint(pivotPointScreen);
            pivotPointWorld.z = 0;

            transform.RotateAround(pivotPointWorld,
                                   Vector3.forward,
                                   -deltaAngle);
        }
    }

    public void YawGenie(Vector2 deltaPixels)
    {
        if (_geniesManager.IsLocomoting)
        {
            return;
        }

        // Emotes can involve the Genie walking away from their
        // pivot point, which means that upon teleportation, they
        // have an undesired offset to their final destination
        if (_isEmoting)
        {
            PropagateRootMotion();
        }

        // Yaw the Genie based on touch input delta (horizontal swipe only)
        transform.rotation *= Quaternion.AngleAxis(deltaPixels.x * PIXELS_TO_DEGREES, Vector3.up);
        //Play animation while yawing if it has not started yet
        if (!_isUserYawing)
        {
            SetGenieAnim(GenieAnimIndex.Yaw);
        }
        // Animation plays at different speeds depending of how many pixels swiped. 
        // Speed calculation: ((angle rotated/ total angle animation * animation lenght)/deltaTime)
        float speed = (((deltaPixels.x * PIXELS_TO_DEGREES) / YAW_ANIM_TOTAL_ANGLE) * YAW_ANIM_DURATION) / Time.deltaTime;
        _animator.SetFloat("YawSpeed", speed);
        _isUserYawing = true;
    }

    public Quaternion GetLookAtCameraYaw()
    {
        if (_cameraManager.IsScreenspace)
        {
            Quaternion currRoll = transform.rotation.ExtractRoll();
            return currRoll * Quaternion.identity;
        }
        else
        {
            Vector3 targetForward = _cameraManager.ActiveCamera.transform.position - transform.position;

            // Refine target forward based on if we can pull the exact camera we need.
            // At one point, these vars weren't initialized so we had to check!
            if (_cameraManager != null || _cameraManager.ActiveCamera != null)
            {
                targetForward = _cameraManager.ActiveCamera.transform.position - transform.position;
            }
            Vector3.ProjectOnPlane(targetForward, Vector3.up);

            Quaternion lookRotation = Quaternion.LookRotation(targetForward, Vector3.up);
            return lookRotation.ExtractYaw();
        }
    }

    public void TeleportGenie(Vector3 targetPos)
    {
        // otherwise, you're gonna teleport the naked genie
        if (_preloadFxInstance != null)
        {
            return;
        }

        // Emotes can involve the Genie walking away from their
        // pivot point, which means that upon teleportation, they
        // have an undesired offset to their final destination
        if (_isEmoting)
        {
            PropagateRootMotion();
        }

        MakeTeleportParticles(targetPos);

        // Go to point
        transform.position = targetPos;

        // Look at camera
        transform.rotation = GetLookAtCameraYaw();
    }

    public void MakeTeleportParticles(Vector3 rootPosition)
    {
        // Currently having an alpha issue with particle shader on iOS
        if (_cameraManager.IsScreenspace)
        {
            Debug.Log("Ignoring request to MakeTeleportParticles while in ScreenSpace.");
            return;
        }

        // Create particle effect at current center of mass
        TeleportParticlesController teleportParticles =
            Instantiate(_teleportParticlesPrefab,
                        rootPosition + Vector3.up * (CurrHeight * 0.4f),
                        Quaternion.identity);
        teleportParticles.Initialize(CurrScale);
    }

    public void Jump()
    {
        // Jumps can interrupt an Emote.
        if (_isEmoting)
        {
            EmoteManager.SoftStopEmote();
        }
        _animator.SetTrigger(TRIGGER_JUMP);
    }

    private void SetGenieAnim(GenieAnimIndex incomingAnim)
    {
        // Our locomotion animation can and should interrupt any currently
        // playing emotes.
        if (_isEmoting)
        {
            // UNLESS the incoming locomotion animation request is a Yaw. Why?
            // (1) It's very easy to accidentally "yaw" an impercetpible amount
            //     because your finger accidentally brushes the screen.
            // (2) It's nice to be able to "spin" your Emote while it's playing.
            if (incomingAnim != GenieAnimIndex.Yaw)
            {
                // Otherwise interrupt the current emote. So like, if you're
                // emoting and you want to Joystick/Jump, you can!
                EmoteManager.SoftStopEmote();
            }
        }

        // Idle, Walk, Yaw, etc.
        // We can set this even if Genie isEmoting, because the EmoteManager
        // is playing the emote back via a PlayableGraph, whcih is crossfaded
        // on top of our animation tree.
        _animator.SetInteger(PARAM_WHICH_ANIM_IDX, (int)incomingAnim);
    }

    // Stores the local position of the Genie beneath its parent, zero's it out, and
    // applys that stored offset to the parent.
    public void PropagateRootMotion()
    {
        // Add the platform shoe height (scaled), or you'll be flatfooted with your
        // heeled shoe penetrating the ground.
        transform.position = _animator.transform.position -
                                (_platformShoeHeight * _animator.transform.lossyScale.y);
        // Reset this, as you may be coming off an Emote with root anim
        _animator.transform.localPosition = _platformShoeHeight;
        // Propagate and reset child rotation
        transform.rotation *= _animator.transform.localRotation;
        _animator.transform.localRotation = Quaternion.identity;
    }
}

// References to animation slots within the AnimationController state machine
public enum GenieAnimIndex : int
{
    Idle = 0,
    WalkRunBlend = 1,
    Yaw = 2
}