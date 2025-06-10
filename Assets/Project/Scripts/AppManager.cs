using Genies.Components.Accounts;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.InputSystem;


// Our main boostrapper for controlling app launch and dependencies.
public class AppManager : MonoBehaviour
{
    public event System.Action OnGameReady;

    [SerializeField] private string _appVersionString;
    [SerializeField] private InputManager _inputManagerPrefab;
    [SerializeField] private LaunchSequenceController _launchSequenceControllerPrefab;
    [SerializeField] private GeniesManager _geniesManagerPrefab;
    [SerializeField] private UserGenieLoader _userGenieLoaderPrefab;
    [SerializeField] private MainMenuController _mainMenuControllerPrefab;
    // We have an interesting bug where the ambient lighting gets effed if
    // there is no Direcional light in the scene at launch. So, we will
    // start with all of our lighting in the scene instead of spawning it
    // for now. Maybe related: AmbientLighting is no longer working reliably :(
    [SerializeField] private LightController _lightController;
    
    // Hack: We need the current UserId in InventoryManager and AddItemMenuController
    public static string UserId
    {
        get
        {
            return Instance._userLoginController.UserId;
        }
    }

    // Hack: We need to grab the camera while animating via the FaceController
    public static Camera ActiveCamera { get { return Instance._cameraManager.ActiveCamera; } }

    private bool _isGameReady = false;
    public bool IsGameReady { get { return _isGameReady; } }

    // Class variables
    private LaunchSequenceController _launchSequenceController;
    private UserLoginController _userLoginController;
    private InputManager _inputManager;
    private GeniesManager _geniesManager;
    private UserGenieLoader _userGenieLoader;
    private SpatialMeshController _spatialMeshController;
    private MainMenuController _mainMenuController;
    private BgController _bgController;
    private FacesManager _facesManager;
    private CameraManager _cameraManager;
    private XROrigin _xrOrigin;
    private JoystickController _joystickController;
    // HACK: This enables our static function hacks to exist ^^;;
    private static AppManager Instance;

    private void Awake()
    {
        Instance = this;

        // 30 is requested by the MediaSaver plugin...
        Application.targetFrameRate = RecordingManager.TARGET_FRAME_RATE_FOR_RECORDING;

#if CREATOR_BUILD
        // Throw up a splash screen for Creators, because they skip the default
        // Genies Login splash screen
        Instantiate(creatorSplashControllerPrefab).Initiaialize(appVersionString)
#endif

        // Input Manager
        _inputManager = Instantiate(_inputManagerPrefab);
        _inputManager.Initialize(); 

        _cameraManager = _inputManager.GetComponentInChildren<CameraManager>();
        if(_cameraManager == null)
        {
            Debug.LogError("CameraManager not found in InputManager's children!");
            return;
        }
        _facesManager = _inputManager.GetComponentInChildren<FacesManager>();
        if (_facesManager == null)
        {
            Debug.LogError("FacesManager not found in InputManager's children!");
            return;
        }

        // Hack: SpatialMeshController shares a ton of references with InputManager,
        // so they live on the same prefab.
        _spatialMeshController = _inputManager.GetComponentInChildren<SpatialMeshController>();
        _xrOrigin = _inputManager.GetComponentInChildren<XROrigin>();
        _spatialMeshController.Initialize(_xrOrigin, _inputManager, _cameraManager);

        // Launch Sequence Controller (Login UI, Floor Finder UI, etc)
        _launchSequenceController = Instantiate(_launchSequenceControllerPrefab);
        // Login UI and backend combined ^_^;;
        _userLoginController = _launchSequenceController.UserLoginController;
        _userLoginController.Initialize();

        // Manage Genies
        _geniesManager = Instantiate(_geniesManagerPrefab);
        _userGenieLoader = Instantiate(_userGenieLoaderPrefab);

        // Track faces in real time and send data to listeners
        // @CameraManager to know where the camera is so we can point the face at it in editor
        // @GeniesManager for editor use, reset the face any time the genie is teleported.
        _facesManager.Initialize(_cameraManager, _geniesManager);

        // Main menu controller
        _mainMenuController = Instantiate(_mainMenuControllerPrefab);
        _bgController = _mainMenuController.GetComponentInChildren<BgController>();
        _joystickController = _mainMenuController.GetComponentInChildren<JoystickController>();
        if (_joystickController == null)
        {
            Debug.LogError("JoystickController not found in MainMenuController's children!");
            return;
        }

        // Controls the background/environment the Genie exists within
        // @CameraManager to know when the camera is in screen space and adjust the background accordingly.
        // @MainMenuController to listen for commands from the user as they operate the UI
        _bgController.Initialize(_cameraManager, _mainMenuController);

        // @AppManager to know when the Game state is Ready and show the Genie.
        // @InputManager to know when the user is touching the screen and have CurrentGenie react.
        // @UserLoginController to know when the user is logged in and start loading the UserGenie.
        // @UserGenieLoader to load the User Genie and set it up.
        // @SpatialMeshController to be able to find the floor and place the Genie on it.
        // @CameraManager to know when the camera is in screen space and adjust the Genie accordingly.
        // @JoystickController to know when the user is using the joystick to control the Genie anim/position.
        _geniesManager.Initialize(this, _inputManager, _userLoginController, _userGenieLoader,
                                    _spatialMeshController, _cameraManager, _facesManager, _joystickController);

        // @GeniesManager to instantiate Geneis based on User selection
        // @InputManager to know when the user is touching the screen or using the joystick
        // @UserLoginController to know when the user is logged in and supply menus related to account
        // @LightController so that the user can adjust the lighting via the menu
        // @InventoryManager to know when the user is logged in and supply menus related to inventory
        // @CameraManager to grab the Recording camera, as Recording UX lives in Main Menu
        // @BgController to listen for callbacks and adjust UI to refelct current BG state
        _mainMenuController.Initialize(_geniesManager, _inputManager,
                                      _userLoginController, _lightController,
                                      _cameraManager, this);

        // Scene Lighting
        // @GeniesManager to know when the Genie is teleported and reset the lighting.
        // @InputManager to know when the user is in screen space and reset the lighting.
        // @SpatialMeshController access SpatialMesh materials to impact shadow darkness.
        _lightController.Initialize(_geniesManager, _cameraManager, _spatialMeshController);

        // Login sequence (Displaying the Login UI + the floor finder UI)
        // @AppVersionString becuase this is the UI that displays it.
        // @InputManager to know when the user has found the floor (a type of input).
        // @GeniesManager to know when Genie is initialized, as well as when to revoke User Genie.
        // @SpatialMeshController to see the mesh visibility, and know if lidar is supported.
        // @UserLoginController has events that impact the login UI.
        _launchSequenceController.Initialize(_appVersionString,
                                    _inputManager,
                                    _geniesManager,
                                    _spatialMeshController,
                                    _cameraManager);
        _launchSequenceController.OnFloorSearchComplete += SetGameStateToReady;

        // Sanity-preserving HACK to ensure Unity only listens to your input in Game play mode,
        // and not just randomly while it's out of focus:
        // See https://docs.unity3d.com/Packages/com.unity.inputsystem@1.4/changelog/CHANGELOG.html#140---2022-04-10
        // Disable Input System 1.4.0 shortcut feature by adding this component to an enabled GameObject
        // in your main/first scene.
        InputSystem.settings.SetInternalFeatureFlag("DISABLE_SHORTCUT_SUPPORT", true);

    }
    
    private void SetGameStateToReady()
    {
        _launchSequenceController.OnFloorSearchComplete -= SetGameStateToReady;
        _isGameReady = true;
        OnGameReady?.Invoke();
    }
}


public enum Layers
{
    Default = 0,
    TransparentFX = 1,
    IgnoreRaycast = 2,
    Water = 4,
    UI = 5,
    Avatar = 6,
    IKColliders = 7,
    PostProcessing = 8,
    ScreenSpace = 9,
    XRSimulatino = 30,
    SpatialMesh = 31
}