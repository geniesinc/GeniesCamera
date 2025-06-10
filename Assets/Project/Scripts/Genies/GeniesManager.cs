using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Genies.Avatars;
using Genies.Components.Accounts;
using UnityEngine.XR.ARFoundation;
using System.Net.NetworkInformation;
using System;
using UnityEditor;
using System.Threading.Tasks;

public class GeniesManager : MonoBehaviour
{
    public event Action<int> OnGenieThumbnailChanged;
    public event Action OnCurrentGenieTeleported;
    public event Action OnFirstGenieInitialized;

    // Legacy/offline emotes as compared to new cloud emote pipeline.
    // They live here so that we don't have to store them on every Genie
    // prefab. The EmoteManager (which lives on the Genie prefab) will
    // pull them via genieController.geniesManager.animClip
    public AnimationClip LegacyVogueAnim;
    public AnimationClip LegacyPeaceAnim;
    public AnimationClip LegacyWaveAnim;

    // This is used and set by the Genies this manager spawns. We keep it
    // here though, in case the Genie is switched out in the middle of an action.
    public bool IsLocomoting { get; set; } = false;

    // Reference to the currently active Genie. At this time, there is only one
    // Genie in the scene at a time.
    public GenieController CurrentGenie { get; private set; }
    // Reference to the dynamically generated User Genie, as compared to the
    // default offline Genies baked into this app, Friends Genies, or anything else.
    public GenieController UserGenie { get; private set; }

    [SerializeField] private GameObject _userGeniePrefab;
    [SerializeField] private HintTextController _hintTextPrefab;

    // Note: This is specifically a list of GameObjects as compared to a list of GenieController objects,
    // as is typical in fields serialized in the Editor. This is because in order to instantiate properly
    // from the Resources directory, it MUST be a GameObject type.
    private List<GameObject> _availableGeniePrefabs;
    public int GenieCount { get { return _availableGeniePrefabs.Count; } }

    // Tracks which genie is currently active to avoid double-calls
    private int _currGenieIdx = -1;

    // Which of the available Genie prefabs is the User Genie?
    public const int USER_GENIE_INDEX = 0;
    // Which of the available Genie prefabs is an "Offline"/local Genie?
    public const int LOCAL_GENIE_INDEX = 1;
    // This is the cutest height in XR, deal with it.
    public const float GENIE_HEIGHT_FOR_XR = 1.2446f;
    // This is where our offline Genies are stored in Resources.
    private const string GENIES_STARTER_PACK_ROOT = "GeniesStarterPack";

    // Displayed when the user double-taps but there is no flat surface at tap point
    private const string DOUBLE_TAP_HINT = "could not detect flat surface\nat double-tap position";

    // Tracks if all onboarding/preloading is complete and game is fully interactive
    public bool IsGameReady { get {return _appManager.IsGameReady; } }

    // This is set by the MainMenuController when the user toggles the ForceLookAtCamera button.
    // It is used by individual spawned Genies who query this value when Gazing.
    public bool ForceLookAtCamera = false;

    // We must update in LateUpdate, otherwise the mocap cannot overlay
    // on top of the animation...
    private ARFace _lastSeenFace;
    // We want to apply this in LateUpdate
    private Quaternion _targetSpineRotation;

    // Stored managers
    AppManager _appManager;
    InputManager _inputManager;
    FacesManager _facesManager;
    CameraManager _cameraManager;
    UserLoginController _userLoginController;
    UserGenieLoader _userGenieLoader;
    SpatialMeshController _spatialMeshController;
    JoystickController _joystickController;

    private bool _didInitialize = false;

    public void Initialize(AppManager appManager,
                           InputManager inputManager,
                           UserLoginController userLoginController,
                           UserGenieLoader userGenieLoader,
                           SpatialMeshController spatialMeshController,
                           CameraManager cameraManager,
                           FacesManager facesManager,
                           JoystickController joystickController)
    {
        // Store managers
        _appManager = appManager;
        _inputManager = inputManager;
        _userLoginController = userLoginController;
        _userGenieLoader = userGenieLoader;
        _cameraManager = cameraManager;
        _spatialMeshController = spatialMeshController;
        _facesManager = facesManager;
        _joystickController = joystickController;

        // Subscribe to callbacks
        _cameraManager.OnActiveCameraTypeChanged += PlaceGenieInNewCameraView;
        _appManager.OnGameReady += ShowGenieOnGameReady;

        // For animating the genies face
        _facesManager.OnARFaceUpdated += FacesManager_OnARFaceUpdated;

        // Putting this here because it doesn't require knowledge of if the interaction
        // hit the Genie or not. Could also apply to multiple Genies if we have more than
        // one in the Scene ever.
        _inputManager.OnDoubleTap += ProcessDoubleTap;
        _inputManager.OnTouchDown += ProcessTouch;
        _inputManager.OnTouchUp += ClearCurrentGenieTouchedState;
        _inputManager.OnTouchDrag += YawCurrentGenie;
        _inputManager.OnTwist += TwistCurrentGenie;
        _inputManager.OnTwoFingerDrag += TranslateCurrentGenieXZ;
        _inputManager.OnThreeFingerDrag += TranslateCurrentGenieY;
        _inputManager.OnScale += ScaleCurrentGenieByPixelDelta;

        // Using Start as opposed to Awake, because MainMenuController is using Awake
        // to set Callbacks to this class. We'd like those to be in place before we
        // call Set a genie so that we get the Thumbnail callback.
        _userLoginController.OnLoginSuccessful += SetUserGenieOnLogIn;
        //userLoginController.OnLoginStateAborted += SetLocalGenieOnLogInAborted;   

        LoadGeniePrefabs();

        _didInitialize = true;
    }

    private void LoadGeniePrefabs()
    {
        // Track our available Genies
        _availableGeniePrefabs = new List<GameObject>();

        // Populate list from resources dir
        UnityEngine.Object[] gameObjects = Resources.LoadAll(GENIES_STARTER_PACK_ROOT, typeof(GameObject));
        for (int i = 0; i < gameObjects.Length; i++)
        {
            var go = (GameObject)gameObjects[i];
            var gc = go.GetComponent<GenieController>();
            if (gc != null)
            {
                _availableGeniePrefabs.Add(go);
            }
        }

        // Store user genie as well
        _availableGeniePrefabs.Insert(USER_GENIE_INDEX, _userGeniePrefab);

        // Array consists of User Genie and then any offline genies
        if (_availableGeniePrefabs.Count < LOCAL_GENIE_INDEX)
        {
            Debug.LogError("You need at least one local Genie defined in GeniesManager.");
        }
    }

    private void LateUpdate()
    {
        if (!_didInitialize)
        {
            return;
        }

        // Apply mocap to the current Genie
        if (CurrentGenie != null && CurrentGenie.IsGenieSetUp)
        {
            // Get the last known pose from the Emote or AnimaitonController
            CurrentGenie.CacheAnimationPose();
            // Apply the spine mocap on top of that anim pose.
            // This changes the world space of the head transform.
            CurrentGenie.ApplySpineMocap(_targetSpineRotation);
            // Do this last because it uses the head transform as part
            // of its calculus to determine LookAt direction for eye anim.
            CurrentGenie.ApplyFacialMocap(_lastSeenFace);
        }
    }

    private void ProcessTouch(Vector2 touchPos)
    {
        RaycastHit hit;
        if (Physics.Raycast(_cameraManager.ActiveCamera.ScreenPointToRay(touchPos),
                            out hit,
                            100f,
                            1 << (int)Layers.Avatar))
        {
            var touchedGenie = hit.collider.GetComponentInParent<GenieController>();
            if(touchedGenie != null)
            {
                touchedGenie.SetTouchedState(true);
            }
        }
    }

    public void ReportGenieIsSetUp(GenieController genie)
    {
        // Is this our very first setup?
        bool isFirstGenie = CurrentGenie == null;

        // Track this as current
        CurrentGenie = genie;

        // LoginSequenceController is listening, and if it's
        // the first time, it will create a double-tap tutorial.
        if (isFirstGenie)
        {
            OnFirstGenieInitialized?.Invoke();
        }
    }

    private void ClearCurrentGenieTouchedState(Vector2 lastTouchPos)
    {
        if (CurrentGenie != null && CurrentGenie.IsGenieSetUp)
        {
            CurrentGenie.SetTouchedState(false);
        }
    }

    private void YawCurrentGenie(Vector2 deltaPixels)
    {
        if (CurrentGenie != null && CurrentGenie.IsGenieSetUp)
        {
            CurrentGenie.YawGenie(deltaPixels);
        }
    }

    private void TwistCurrentGenie(float deltaAngle, Vector2 pivotPointScreen)
    {
        if (CurrentGenie != null && CurrentGenie.IsGenieSetUp)
        {
            CurrentGenie.TwistGenie(deltaAngle, pivotPointScreen);
        }
    }

    private void TranslateCurrentGenieXZ(Vector2 deltaPixels, Vector2 currScreenPoint)
    {
        if (CurrentGenie != null && CurrentGenie.IsGenieSetUp)
        {
            CurrentGenie.TranslateGenieXZ(deltaPixels);
        }
    }

    private void TranslateCurrentGenieY(Vector2 deltaPixels, Vector2 currScreenPoint)
    {
        if (CurrentGenie != null && CurrentGenie.IsGenieSetUp)
        {
            CurrentGenie.TranslateGenieY(deltaPixels);
        }
    }

    private void ScaleCurrentGenieByPixelDelta(float deltaPixels, Vector2 pivotPointScreen)
    {
        if (CurrentGenie != null && CurrentGenie.IsGenieSetUp)
        {
            CurrentGenie.ScaleGenieByPixelDelta(deltaPixels, pivotPointScreen);
        }
    }

    private void OnDestroy()
    {
        if (_didInitialize)
        {
            _cameraManager.OnActiveCameraTypeChanged -= PlaceGenieInNewCameraView;
            _appManager.OnGameReady -= ShowGenieOnGameReady;

            _facesManager.OnARFaceUpdated -= FacesManager_OnARFaceUpdated;

            _inputManager.OnDoubleTap -= ProcessDoubleTap;
            _inputManager.OnTouchDown -= ProcessTouch;
            _inputManager.OnTouchUp -= ClearCurrentGenieTouchedState;
            _inputManager.OnTouchDrag -= YawCurrentGenie;
            _inputManager.OnTwist -= TwistCurrentGenie;
            _inputManager.OnTwoFingerDrag -= TranslateCurrentGenieXZ;
            _inputManager.OnThreeFingerDrag -= TranslateCurrentGenieY;
            _inputManager.OnScale -= ScaleCurrentGenieByPixelDelta;

            _userLoginController.OnLoginSuccessful -= SetUserGenieOnLogIn;
            //userLoginController.OnLoginStateAborted -= SetLocalGenieOnLogInAborted;   
        }
    }

    private void FacesManager_OnARFaceUpdated(ARFace arFace)
    {
        if (arFace == null && _lastSeenFace == null)
        {
            Debug.LogWarning("No ARFace detected, cannot apply mocap.");
            return;
        }

        _lastSeenFace = arFace != null? arFace : _lastSeenFace;

        // First thing we want to do is get the ARFace rotations into local space.

        // We could dick around with Quaternions, but Pose is a handy trick to substitute
        // for like a transform.InverseTransformRotation type call.
        Pose arFacePoseWorld = new Pose(arFace.transform.position, arFace.transform.rotation);
        Pose arFacePoseLocal = _cameraManager.XRCamera.transform.InverseTransformPose(arFacePoseWorld);
        _targetSpineRotation = arFacePoseLocal.rotation;

#if UNITY_EDITOR
        // We are looking at the camera though IRL, so we need to flip this rotation to
        // pretend the camera and face are looking in the same direction.
        // ...or something?? I thought that's what we're doing but it's only in Editor, lol.
        _targetSpineRotation = Quaternion.Euler(0, 180f, 0) * _targetSpineRotation;
#endif
    }

    public Texture GetGenieThumbnailByIdx(int genieIdx)
    {
        return _availableGeniePrefabs[genieIdx].GetComponent<GenieController>().ThumbnailTexture;
    }

    // Being "not logged in" is different than being "logged out", because we also have
    // a state by which you are never logged in which calls this function.
    public void RemoveAndReplaceUserGenie()
    {
        if(UserGenie != null)
        {
            UserGenie.CleanUpUserGenie();
            UserGenie.gameObject.SetActive(false);
        }

        // If youre current Genie is set to the user genie (0) OR
        // it hasn't been set at all ever (-1)
        if (_currGenieIdx <= USER_GENIE_INDEX)
        {
            _ = SetCurrentGenieByIdx(LOCAL_GENIE_INDEX);
        }
    }

    public void SetUserGenieOnLogIn()
    {
        // if current genie hasn't been set by user
        if(_currGenieIdx < 0)
        {
            _ = SetCurrentGenieByIdx(USER_GENIE_INDEX);
        }
    }

    /*void SetLocalGenieOnLogInAborted()
    {
        // if current genie hasn't been set by user
        if (currGenieIdx < 0)
        {
            SetCurrentGenieByIdx(LOCAL_GENIE_INDEX);
        }
    }*/

    public float GetMinHeightToFitInFrame(Vector3 targetPosition)
    {
        // Make a plane that is positioned at the Genie's location and
        // yawed to face the camera (only y rotation).
        Vector3 planeNormal = Vector3.ProjectOnPlane(-_cameraManager.ActiveCamera.transform.forward, Vector3.up);
        Plane raycastCatcherPlane = new Plane(planeNormal, targetPosition);

        // The top of screen point represents the highest pixel we have visibility to
        // within the current viewport.
        Vector2 topOfScreenPoint = new Vector2(Screen.width / 2f, Screen.height);
        // Construct a ray going from this pixel outwards, to get the maximum possible
        // height of a genie in-frame.
        Ray topOfScreenRay = _cameraManager.ActiveCamera.ScreenPointToRay(topOfScreenPoint);

        // Declare a vairable and populate it based on our success with the raycast plane.
        float genieMaxVisibleHeight;
        if (raycastCatcherPlane.Raycast(topOfScreenRay, out float distance))
        {
            Vector3 camFrustrumTopPoint = topOfScreenRay.GetPoint(distance);
            genieMaxVisibleHeight = camFrustrumTopPoint.y - targetPosition.y;
        }
        // If you're pointing the phone directly at the ground for example, then we will
        // never hit our raycast plane and should just calculate the distance to the ground.
        else
        {
            // Height from floor to camera, genie takes up full screen
            genieMaxVisibleHeight = _cameraManager.ActiveCamera.transform.position.y - targetPosition.y;
        }

        // The max visible height will put the Genie's head right at the top of the frame.
        // This is sort of un-cute, we will want a little 20% buffer there (*0.8f)
        genieMaxVisibleHeight *= 0.8f;
        return Mathf.Min(genieMaxVisibleHeight, GENIE_HEIGHT_FOR_XR);
    }

    public Vector3 GetGeniePlacementPositionInCameraFrustum()
    {
        // Get the "cutest point" on screen (center ish)
        Vector2 screenPoint = CameraManager.GetIdealScreenPointForGeniePlacement();

        // Get floor via raycast at the "cutest point"
        Vector3? floorPoint = _spatialMeshController.GetFloorAtScreenPoint(screenPoint);
        if (floorPoint.HasValue)
        {
            return floorPoint.Value;
        }

        // Fallback on known floor height from initial raycast discovery
        Vector3 inFrontOfCamera = _cameraManager.ActiveCamera.transform.forward;
        inFrontOfCamera.y = _spatialMeshController.InitialFoundFloorPoint.y;

        return inFrontOfCamera;
    }

    public Vector3 GetHiddenPosition()
    {
        return Vector3.up * _cameraManager.ActiveCamera.farClipPlane;
    }

    private void ShowGenieOnGameReady()
    {
        // Sanity-check
        if (CurrentGenie == null)
        {
            Debug.LogError("GeniesManager: Genie is null, cannot place OnGameReady.");
            return;
        }

        // Put the Genie in view
        if (CurrentGenie.IsGenieSetUp)
        {
            CurrentGenie.transform.position = GetGeniePlacementPositionInCameraFrustum();
            CurrentGenie.SetHeight(GetMinHeightToFitInFrame(CurrentGenie.transform.position));
            CurrentGenie.GreetUser();
        }
        // Put the Genie loading cloud in view
        else
        {
            CurrentGenie.OnGameReady_ShowPreloadFx();
        }
    }

    public void PlaceGenieInNewCameraView(ActiveCameraType activeCameraType)
    {
        // Sanity check
        if (CurrentGenie == null || !CurrentGenie.IsGenieSetUp)
        {
            return;
        }

        // If you're toggling into screen space
        if (activeCameraType == ActiveCameraType.ScreenspaceCamera)
        {
            CurrentGenie.transform.position = Vector3.zero;
            CurrentGenie.SetScale(1);
            CurrentGenie.transform.rotation = Quaternion.identity;
        }
        // If you're toggling back into world space
        else
        {
            CurrentGenie.transform.position = GetGeniePlacementPositionInCameraFrustum();
            CurrentGenie.transform.rotation = CurrentGenie.GetLookAtCameraYaw();
            CurrentGenie.SetHeight(GetMinHeightToFitInFrame(CurrentGenie.transform.position));
            CurrentGenie.Wave();
            CurrentGenie.MakeTeleportParticles(CurrentGenie.transform.position);
        }
    }

    private void ProcessDoubleTap(Vector2 touchPos)
    {
        // Make sure the genie has clothes and a floor exists!
        if(!IsGameReady)
        {
            return;
        }

        if (_cameraManager.IsScreenspace)
        {
            /*Vector3 worldPoint = inputManager.screenSpaceCamera.ScreenToWorldPoint(touchPos);
            worldPoint.z = 0f;
            // pivot is at her feet though, so offset by half height
            worldPoint += Vector3.down * boxCollider.size.y / 2f;
            TeleportGenie(worldPoint);*/

            // This fires by accident sometimes when the user can't put both fingers
            // down to scale at the same time, and does two scales in rapid succession,
            // like Thumb-Index, Index-Thumb.
            // Not sure this feature is even useful in 2D though? Removing.
            //
            // Note also that screenspace genies compositing technique may cause artifacts
            // with the double-tap particles. You can tell bc there will be black bboxes
            // around the various elements.
            return;
        }

        // Raycast into world and Teleport to available surface
        else
        {
            Vector3? floorPos = _spatialMeshController.GetFloorAtScreenPoint(touchPos);
            if (floorPos != null && CurrentGenie != null)
            {
                CurrentGenie.TeleportGenie(floorPos.Value);
                InvokeOnCurrentGenieTeleported();
            }
            else
            {
                // This will auto-destroy
                HintTextController htc = Instantiate(_hintTextPrefab);
                htc.hintText.text = DOUBLE_TAP_HINT;
            }
        }
    }

    // Called by the GenieController when the Genie is initially spawned.
    // This way, any nice callbacks (such as fixing the lighting) will occur
    // at that initial setup time.
    public void InvokeOnCurrentGenieTeleported()
    {
        OnCurrentGenieTeleported?.Invoke();
    }

    private Quaternion GetGenieLookAtCameraRotation(Vector3 defaultPos)
    {
        Vector3 targetForward = _cameraManager.ActiveCamera.transform.position - defaultPos;
        Vector3.ProjectOnPlane(targetForward, Vector3.up);

        Quaternion lookRotation = Quaternion.LookRotation(targetForward, Vector3.up);
        return lookRotation.ExtractYaw();
    }

    public async Task SetCurrentGenieByIdx(int genieIdx)
    {
        // Sanity check: Do we need to do anything?
        if (_currGenieIdx == genieIdx)
        {
            Debug.Log("Ignoring Selection: Requested Genie is Current Genie.");
            return;
        }

        // Get the stats of the current Genie so we can fill her shoes
        bool canCopyXform = CurrentGenie != null && CurrentGenie.IsGenieSetUp;
        Vector3 targetPos = canCopyXform ? CurrentGenie.transform.position :
                                IsGameReady ? GetGeniePlacementPositionInCameraFrustum():
                                              GetHiddenPosition();    
        Quaternion targetRot = canCopyXform ? CurrentGenie.transform.rotation : GetGenieLookAtCameraRotation(targetPos);
        float targetScale = canCopyXform ? CurrentGenie.CurrScale : 1;
        float targetHeight = canCopyXform ? CurrentGenie.CurrHeight : 0;

        // Get rid of it after copying its values
        HideOrDestroyOldGenie(CurrentGenie);

        // Unhide or create the user genie
        if (genieIdx == USER_GENIE_INDEX)
        {
            // This will generate a "loading cloud" if the user genie needs to load, so you will 
            // have *something* in the scene while the async operation happens.
            CurrentGenie = CreateOrRecallUserGenie(targetPos, targetRot, targetScale, targetHeight);
            // On the off chance it never loaded from the cloud properly...
            if (!_userGenieLoader.IsGenieLoaded || !UserGenie.IsGenieSetUp)
            {
                await _userGenieLoader.LoadUserGenieAsync(UserGenie.transform);
                // Remove any extra GeniesParty components
                _userGenieLoader.StripDefaultComponents();
                // Add any specific GeniesCamera components
                UserGenie.SetupGenie();
            }
        }
        else
        {
            CurrentGenie = CreateOfflineGenie(genieIdx, targetPos, targetRot, targetHeight, targetScale);
        }

        // Keep track of which genie per the UI
        _currGenieIdx = genieIdx;

        // Update thumbnail
        OnGenieThumbnailChanged?.Invoke(genieIdx);
    }

    private void HideOrDestroyOldGenie(GenieController genie)
    {
        if(genie == null)
        {
            return;
        }
        else if (genie.IsUserGenie)
        {
            genie.gameObject.SetActive(false);
        }
        else
        {
            Destroy(genie.gameObject);
        }
    }

    private GenieController CreateOrRecallUserGenie(Vector3 pos, Quaternion rot, float scale, float height)
    {
        if (UserGenie == null)
        {
            UserGenie = Instantiate(_availableGeniePrefabs[USER_GENIE_INDEX],
                                    GetHiddenPosition(),
                                    rot,
                                    transform).GetComponent<GenieController>();
            UserGenie.Initialize(this, _cameraManager, _joystickController, _facesManager, isUserGenie: true);

            // Show the cloud
            UserGenie.SetupUserGeniePreload();
        }
        else
        {
            UserGenie.gameObject.SetActive(true);
            UserGenie.transform.rotation = rot;

            // Flash a peace sign!
            if (UserGenie.IsGenieSetUp)
            {
                UserGenie.transform.position = pos;
                UserGenie.GreetUser();
            }
            // Show the cloud / hide the Genie
            else
            {
                UserGenie.transform.position = GetHiddenPosition();
                UserGenie.SetupUserGeniePreload();
            }

            // Height will be more accurate with 'everything works with everything',
            // but might not be calculable on some of our legacy stuff.
            if (height > 0)
            {
                UserGenie.SetHeight(height);
            }
            else
            {
                 UserGenie.SetScale(scale);    
            }
        }

        return UserGenie;
    }

    private GenieController CreateOfflineGenie(int idx, Vector3 pos, Quaternion rot, float height, float scale)
    {
        GenieController genie = Instantiate(_availableGeniePrefabs[idx], pos, rot, transform).GetComponent<GenieController>();
        genie.Initialize(this, _cameraManager, _joystickController, _facesManager, isUserGenie: false);
        genie.SetupGenie();

        if (_currGenieIdx < 0)
        {
            Vector3 targetPos = GetGeniePlacementPositionInCameraFrustum();
            height = GetMinHeightToFitInFrame(targetPos);
            genie.SetHeight(height);
        }
        else
        {
            if (height > 0) genie.SetHeight(height);
            else genie.SetScale(scale);
        }

        return genie;
    }
}
