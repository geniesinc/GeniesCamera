using System;
using System.Collections;
using System.Collections.Generic;
using Genies.Components.Accounts;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class MainMenuController : MonoBehaviour
{
    public event Action<Color> OnScreenspaceBgColorChanged;
    public event Action<BgMode> OnBgModeChanged;

    // Manager references
    [SerializeField] private RecordingManager _recordingManager;
    [SerializeField] private JoystickController _joystickController;
    [SerializeField] private DebugDisplayController _debugDisplayController;
    // Dynamic menu prefabs
    [SerializeField] private LightingMenuController _lightingMenuPrefab;
    // UI Buttons
    [SerializeField] private Button _accountMenuButton;
    [SerializeField] private Button _geniesMenuButton;
    [SerializeField] private Button _backgroundMenuButton;
    [SerializeField] private Button _backgroundSubmenuButton_AR;
    [SerializeField] private Button _backgroundSubmenuButton_Passthrough;
    [SerializeField] private Button _backgroundSubmenuButton_Color;
    [SerializeField] private Button _backgroundSubmenuButton_Media;
    [SerializeField] private Button _lightingMenuButton;
    [SerializeField] private Button _eyeLockButton;
    [SerializeField] private Button _openDebugDisplayButton;
    [SerializeField] private Button _closeDebugDisplayButton;
    [SerializeField] private Button _quickActionButton_Jump;
    [SerializeField] private Button _quickActionButton_Wave;
    [SerializeField] private Button _quickActionButton_Vogue;
    [SerializeField] private Button _quickActionButton_Peace;
    // UI Elements
    [SerializeField] private Canvas _screenSpaceBgCanvas;
    [SerializeField] private GameObject _barMenu;
    [SerializeField] private GameObject _bottomMenu;
    // Genie avatars menu (generated dynamically)
    [SerializeField] private RawImage _genieButtonRawImage;
    [SerializeField] private GameObject _genieSubmenuRoot;
    [SerializeField] private SubmenuButtonController _genieMenuItemPrefab;
    [SerializeField] private Transform _genieMenuItemParent;
    [SerializeField] private RawImage _buggyLayoutHack;
    // Bg Color Menu
    [SerializeField] private RawImage _bgButtonRawImage;
    [SerializeField] private GameObject _bgSubmenuRoot;
    // Icons we'll be using in the bg controller's submenu UI
    [SerializeField] private Texture _bgStateIcon_worldspaceAR;
    [SerializeField] private Texture _bgStateIcon_screespacePassthrough;
    [SerializeField] private Texture _bgStateIcon_screenspaceColor;
    [SerializeField] private Texture _bgStateIcon_screenspaceMedia;
    [SerializeField] private Texture _bgStateIcon_worldspaceColor;
    // For modal color picker menu dialogue
    [SerializeField] private ColorPickerMenuController _colorPickerMenuPrefab;
    // Eye lock menu
    [SerializeField] private Texture _eyeLockOnTexture;
    [SerializeField] private Texture _eyeLockOffTexture;
    [SerializeField] private RawImage _eyeLockRawImage;
    // Debug menu etc
    [SerializeField] private GameObject _debugDisplayIcon;
    [SerializeField] private GameObject[] _hideForCreators;

    private ColorPickerMenuController _colorPickerMenuController;
    private Color _previousBgColor = BgController.DEFAULT_SCREENSPACE_BG_COLOR;
    
    private AppManager _appManager;
    private GeniesManager _geniesManager;
    private UserLoginController _userLoginController;
    private LightController _lightController;
    private LightingMenuController _lightingMenuInstance;
    private CameraManager _cameraManager;

    // Tracking current UI state
    private GameObject _currentSubmenu;
    // We will show/hide the user genie button based on log in state
    private SubmenuButtonController[] _selectableGenieButtons;
    private bool _isMainMenuVisible = false;
    private bool _didInitialize = false;
    private Dictionary<BgMode, Texture> _bgModeIcons;

    public void Initialize(GeniesManager geniesManager,
                            InputManager inputManager,
                            UserLoginController userLoginController,
                            LightController lightController,
                            CameraManager cameraManager,
                            AppManager appManager)
    {
        // Store managers
        _appManager = appManager;
        _geniesManager = geniesManager;
        _userLoginController = userLoginController;
        _lightController = lightController;
        _cameraManager = cameraManager;

        // @InputManager is used to get the active camera and screenspace status 
        _recordingManager.Initialize(_cameraManager);
        _screenSpaceBgCanvas.worldCamera = _cameraManager.ScreenspaceCamera;
        _cameraManager.OnActiveCameraTypeChanged += HideJoystickInScreenspace;

        // Add listeners
        _geniesManager.OnGenieThumbnailChanged += SetGenieMenuIconByIdx;

        // Initialize the background mode icons
        _bgModeIcons = new Dictionary<BgMode, Texture>();
        _bgModeIcons[BgMode.WORLDSPACE_AR] = _bgStateIcon_worldspaceAR;
        _bgModeIcons[BgMode.SCREENSPACE_PASSTHROUGH] = _bgStateIcon_screespacePassthrough;
        _bgModeIcons[BgMode.SCREENSPACE_COLOR] = _bgStateIcon_screenspaceColor;
        _bgModeIcons[BgMode.SCREENSPACE_MEDIA] = _bgStateIcon_screenspaceMedia;

        // Hook up the background menu buttons
        _accountMenuButton.onClick.AddListener(ToggleAccountMenu);
        _geniesMenuButton.onClick.AddListener(ToggleGeniesMenu);
        _backgroundMenuButton.onClick.AddListener(ToggleBgModeMenu);
        _backgroundSubmenuButton_AR.onClick.AddListener(OnUserSelectWorldspaceAR);
        _backgroundSubmenuButton_Passthrough.onClick.AddListener(OnUserSelectScreenspacePassthrough);
        _backgroundSubmenuButton_Color.onClick.AddListener(OnUserSelectScreenspaceColor);
        _backgroundSubmenuButton_Media.onClick.AddListener(OnUserSelectScreenspaceMedia);
        _lightingMenuButton.onClick.AddListener(ToggleLightingMenu);
        _eyeLockButton.onClick.AddListener(ToggleEyeLockState);
        _openDebugDisplayButton.onClick.AddListener(ToggleDebugDisplay);
        _closeDebugDisplayButton.onClick.AddListener(ToggleDebugDisplay);
        _quickActionButton_Jump.onClick.AddListener(UserSelectJump);
        _quickActionButton_Wave.onClick.AddListener(UserSelectWave);
        _quickActionButton_Vogue.onClick.AddListener(UserSelectVogue);
        _quickActionButton_Peace.onClick.AddListener(UserSelectPeace);

        // Populate Genies buttons.
        // We call this here on the off-chance the GeniesManager has already populated the available genies.
        AddSelectableGenieButtons();

        // Listen for changes to login/logout. Note, by this point the user is already
        // completed the login sequence. So they have either selected to log in, or
        // opted to stay logged out.
        _userLoginController.OnLoginSuccessful += ShowUserGenieMenuItems;
        _userLoginController.OnLogOutSuccessful += HideUserGenieMenuItems;
        // This funciton will call one of the above two functions based on the current state.
        // It is also called automatically by AddGeniesButtons() above to initialize the ui:
        _userLoginController.OnLoginStateAborted += RefreshUserGenieMenuState;

        _appManager.OnGameReady += ShowMainMenu;

        SetMainMenuVisibility(_appManager.IsGameReady);

#if CREATOR_BUILD
        // Get rid of Genie button...
        RefreshUserGenieMenuState();
        // Creators use a simpler layout with no Genies account associated
        // nor xr developer debug view at this time
        for (int i=0; i < hideForCreators.Length; i++)
        {
            hideForCreators[i].SetActive(false);
        }
#endif

        _didInitialize = true;
    }

    private void OnDestroy()
    {
        if (_didInitialize)
        {
            _geniesManager.OnGenieThumbnailChanged -= SetGenieMenuIconByIdx;

            _userLoginController.OnLoginSuccessful -= ShowUserGenieMenuItems;
            _userLoginController.OnLogOutSuccessful -= ShowUserGenieMenuItems;
            _userLoginController.OnLoginStateAborted -= HideUserGenieMenuItems;

            _appManager.OnGameReady -= ShowMainMenu;

            _cameraManager.OnActiveCameraTypeChanged -= HideJoystickInScreenspace;

            _accountMenuButton.onClick.RemoveListener(ToggleAccountMenu);
            _geniesMenuButton.onClick.RemoveListener(ToggleGeniesMenu);
            _backgroundMenuButton.onClick.RemoveListener(ToggleBgModeMenu);
            _backgroundSubmenuButton_AR.onClick.RemoveListener(OnUserSelectWorldspaceAR);
            _backgroundSubmenuButton_Passthrough.onClick.RemoveListener(OnUserSelectScreenspacePassthrough);
            _backgroundSubmenuButton_Color.onClick.RemoveListener(OnUserSelectScreenspaceColor);
            _backgroundSubmenuButton_Media.onClick.RemoveListener(OnUserSelectScreenspaceMedia);
            _lightingMenuButton.onClick.RemoveListener(ToggleLightingMenu);
            _eyeLockButton.onClick.RemoveListener(ToggleEyeLockState);
            _openDebugDisplayButton.onClick.RemoveListener(ToggleDebugDisplay);
            _closeDebugDisplayButton.onClick.RemoveListener(ToggleDebugDisplay);
            _quickActionButton_Jump.onClick.RemoveListener(UserSelectJump);
            _quickActionButton_Wave.onClick.RemoveListener(UserSelectWave);
            _quickActionButton_Vogue.onClick.RemoveListener(UserSelectVogue);
            _quickActionButton_Peace.onClick.RemoveListener(UserSelectPeace);
        }
    }

    private void HideJoystickInScreenspace(ActiveCameraType newCameraType)
    {
        _joystickController.gameObject.SetActive(newCameraType != ActiveCameraType.ScreenspaceCamera);   
    }

    private void ShowUserGenieMenuItems()
    {
        _selectableGenieButtons[GeniesManager.USER_GENIE_INDEX].gameObject.SetActive(true);
    }

    private void HideUserGenieMenuItems()
    {
        _selectableGenieButtons[GeniesManager.USER_GENIE_INDEX].gameObject.SetActive(false);
        // This isn't really a menu item but hey.
        _ = _geniesManager.SetCurrentGenieByIdx(GeniesManager.LOCAL_GENIE_INDEX);
    }

    private void RefreshUserGenieMenuState()
    {
        _selectableGenieButtons[GeniesManager.USER_GENIE_INDEX].gameObject.SetActive(_userLoginController.IsLoggedIn);

        if (_geniesManager.CurrentGenie != null)
        {
            _genieButtonRawImage.texture = _geniesManager.CurrentGenie.ThumbnailTexture;
        }
    }

    private void ToggleDebugDisplay()
    {
        _debugDisplayController.gameObject.SetActive(!_debugDisplayController.gameObject.activeInHierarchy);
    }

    private void ToggleLightingMenu()
    {
        if (_lightingMenuInstance != null)
        {
            CloseLightingMenu();
        }
        else
        {
            OpenLightingMenu();
        }
    }

    private void ShowMainMenu()
    {
        SetMainMenuVisibility(true);
    }
    private void HideMainMenu()
    {
        SetMainMenuVisibility(false);
    }

    private void SetMainMenuVisibility(bool isVisible)
    {
        _bottomMenu.SetActive(isVisible);
        _barMenu.SetActive(isVisible);

        _isMainMenuVisible = isVisible;
    }

    private void OpenLightingMenu()
    {
        // Turn off anything thats open
        ToggleAnySubmenu(null);

        SetMainMenuVisibility(false);

        _lightingMenuInstance = Instantiate(_lightingMenuPrefab);
        _lightingMenuInstance.lightController = _lightController;
        _lightingMenuInstance.OnLightingMenuClosed += CloseLightingMenu;
    }

    private void CloseLightingMenu()
    {
        _lightingMenuInstance.OnLightingMenuClosed -= CloseLightingMenu;
        Destroy(_lightingMenuInstance.gameObject);

        SetMainMenuVisibility(true);
    }

    private void UserSelectVogue()
    {
        _geniesManager.CurrentGenie.EmoteManager.LegacyVogue();
    }

    private void UserSelectWave()
    {
        _geniesManager.CurrentGenie.EmoteManager.LegacyWave();
    }

    private void UserSelectPeace()
    {
        _geniesManager.CurrentGenie.EmoteManager.LegacyPeace();
    }

    private void UserSelectJump()
    {
        _geniesManager.CurrentGenie.Jump();
    }


    // Background Environment Menu
    private void OnUserSelectWorldspaceAR()
    {
        OnBgModeChanged?.Invoke(BgMode.WORLDSPACE_AR);
        UpdateBgMenuIcon(BgMode.WORLDSPACE_AR);
    }

    private void OnUserSelectScreenspacePassthrough()
    {
        OnBgModeChanged?.Invoke(BgMode.SCREENSPACE_PASSTHROUGH);
        UpdateBgMenuIcon(BgMode.SCREENSPACE_PASSTHROUGH);
	}

    private void OnUserSelectScreenspaceColor()
    {
        OnBgModeChanged?.Invoke(BgMode.SCREENSPACE_COLOR);
        UpdateBgMenuIcon(BgMode.SCREENSPACE_COLOR);

		// Setup modal colorpicker ui
		_colorPickerMenuController = Instantiate(_colorPickerMenuPrefab);
		_colorPickerMenuController.Initialize(_previousBgColor, OnUserSelectNewScreenspaceColor);
		_colorPickerMenuController.OnColorPickerClosed += CleanupScreenspaceColor_modalUI;

		SetMainMenuVisibility(false);
	}

    private void OnUserSelectNewScreenspaceColor(Color newColor)
    {
        // For the actual environment background color
        _previousBgColor = newColor;
        OnScreenspaceBgColorChanged?.Invoke(newColor);
    }

    private void OnUserSelectScreenspaceMedia()
    {
        OnBgModeChanged?.Invoke(BgMode.SCREENSPACE_MEDIA);
        UpdateBgMenuIcon(BgMode.SCREENSPACE_MEDIA);
    }

	private void CleanupWorldspaceColor_modalUI()
	{
		_colorPickerMenuController.OnColorPickerClosed -= CleanupWorldspaceColor_modalUI;

		Destroy(_colorPickerMenuController.gameObject);
		SetMainMenuVisibility(true);
	}

	private void CleanupScreenspaceColor_modalUI()
	{
		_colorPickerMenuController.OnColorPickerClosed -= CleanupScreenspaceColor_modalUI;

		Destroy(_colorPickerMenuController.gameObject);
		SetMainMenuVisibility(true);

		// Tells the main menu to close the bg selection submenu
		ToggleBgModeMenu();
	}

    private void ToggleEyeLockState()
    {
        _geniesManager.ForceLookAtCamera = !_geniesManager.ForceLookAtCamera;
        _eyeLockRawImage.texture = _geniesManager.ForceLookAtCamera ? _eyeLockOnTexture : _eyeLockOffTexture;
    }

    private void ToggleGeniesMenu()
    {
        ToggleAnySubmenu(_genieSubmenuRoot);
    }

    private void ToggleBgModeMenu()
    {
        ToggleAnySubmenu(_bgSubmenuRoot);
    }

    private void ToggleAccountMenu()
    {
        // Turn off anything thats open
        ToggleAnySubmenu(null);

        // Open modal dialogue
        _userLoginController.ShowLoginUI();
    }

    private void ToggleAnySubmenu(GameObject newSubmenu)
    {
        // Toggle off newMenu?
        if (_currentSubmenu == newSubmenu)
        {
            _currentSubmenu?.SetActive(false);
            _currentSubmenu = null;
        }
        // Toggle on newMenu
        else
        {
            // Toggle off previous?
            _currentSubmenu?.SetActive(false);
            // Toggle on next
            _currentSubmenu = newSubmenu;
            _currentSubmenu?.SetActive(true);
        }

        // do some sadness
        _buggyLayoutHack.enabled = false;
        StartCoroutine(ManuallyRefreshMenuLength());
    }

    private IEnumerator ManuallyRefreshMenuLength()
    {
        yield return null;
        _buggyLayoutHack.enabled = true;
    }

    private void UpdateBgMenuIcon(BgMode newBgMode)
    {
        _bgButtonRawImage.texture = _bgModeIcons[newBgMode];  

        // Close that menu!
        ToggleBgModeMenu();
    }

    private void SetGenieMenuIconByIdx(int genieIdx)
    {
        Texture t = _geniesManager.GetGenieThumbnailByIdx(genieIdx);
        _genieButtonRawImage.texture = t;
    }

    private void AddSelectableGenieButtons()
    {
        // Destroy any existing buttons, including the 4 placeholder buttons preopopulated in the editor
        // to get around a Unity bug with the Horizontal Layout Group not calculating the required width 
        // correctly the first time it is enabled.
        _selectableGenieButtons = new SubmenuButtonController[_geniesManager.GenieCount];
        for (int i = _genieMenuItemParent.childCount-1; i >= 0; i--)
        {
            Destroy(_genieMenuItemParent.GetChild(i).gameObject);
        }
        
        // Dynamically add Genies from the GeneisManager class
        _selectableGenieButtons = new SubmenuButtonController[_geniesManager.GenieCount];
        for (int i = 0; i < _geniesManager.GenieCount; i++)
        {
            // Set button and icon
            SubmenuButtonController button = Instantiate(_genieMenuItemPrefab, _genieMenuItemParent);

            int capturedIndex = i; // Capture the index for the lambda expression
            UnityAction[] actions = new UnityAction[2]{
                () => _ = _geniesManager.SetCurrentGenieByIdx(capturedIndex),
                () => ToggleGeniesMenu()
            };
            button.Initialize(_geniesManager.GetGenieThumbnailByIdx(i), actions);

            // Hack: We will treat the user genie button a bit differently.
            if (i == GeniesManager.USER_GENIE_INDEX)
            {
                button.MakeBgTransparent();
            }

            _selectableGenieButtons[i] = button;
        }
    }

}
