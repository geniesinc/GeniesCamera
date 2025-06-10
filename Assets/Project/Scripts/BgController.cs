using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine.Video;
using System;
using System.Threading.Tasks;

public enum BgMode
{
	WORLDSPACE_AR,
	SCREENSPACE_PASSTHROUGH,
	SCREENSPACE_COLOR,
	SCREENSPACE_MEDIA
}

public class BgController : MonoBehaviour
{
	// Contains rendering elements for bg and fg full screen renders. Home of
	// canvas scaler, always enabled, and polled to monitor for screen dimension changes.
	[SerializeField] private RectTransform _bgFullScreenContainer;
	public Canvas BgFullScreenCanvas { get { return _bgFullScreenContainer.GetComponent<Canvas>(); } }
	[SerializeField] private RawImage _fgTextureRawImage;
	[SerializeField] private RawImage _bgTextureRawImage;
	[SerializeField] private VideoPlayer _videoPlayer;
	[SerializeField] private RecordingManager _recordingManager;
	

	// Manage bg as the screen dimensions change (portrait/landscape, etc)
	private Vector2 _prevScreenDimensions;

	// State management
	private BgMode _currBgMode = BgMode.WORLDSPACE_AR;
	private Dictionary<BgMode, Func<Task<bool>>> _stateToSetupFunction;
	private Dictionary<BgMode, Action> _stateToCleanupFunction;

	// default color bg is green screen green
	// but not literally bc that would hurt your eyes.
	// this is just to get the idea across...
	public static Color DEFAULT_SCREENSPACE_BG_COLOR = new Color(32 / 255f, 171 / 255f, 57 / 255f);

	private Color _previousBgColor = DEFAULT_SCREENSPACE_BG_COLOR;
	// This is stored at the class level because our iOS plugin requires an anonymous
	// function be passed in. Since said function cannot have a return value by nature
	// of its structure, we will set this stored value instead.
	private string _userMediaPath;
	private TaskCompletionSource<bool> _userMediaQueryTcs;
	// These are scaled by a canvas scalar, so we cannot just query Screen.width etc.
	private Vector2 _currScreenDimensions
	{
		get
		{
			return new Vector2(_bgFullScreenContainer.rect.width,
							   _bgFullScreenContainer.rect.height);
		}
	}
	private int _worldspaceCam_cullingMask_default;
	private int _worldspaceCam_cullingMask_minusAvatars;
	private MainMenuController _mainMenuController;
	private CameraManager _cameraManager;
	private bool _didInitialize = false;

	public bool IsPortrait { get { return _currScreenDimensions.y >= _currScreenDimensions.x; } }

	public void Initialize(CameraManager cameraManager,
								MainMenuController mainMenuController)
	{
		_cameraManager = cameraManager;
		_mainMenuController = mainMenuController;

		// Camera setup
		_worldspaceCam_cullingMask_default = _cameraManager.XRCamera.cullingMask;
		_worldspaceCam_cullingMask_minusAvatars = _worldspaceCam_cullingMask_default & ~(1 << (int)Layers.Avatar);

		// State setup
		_stateToSetupFunction = new Dictionary<BgMode, Func<Task<bool>>>()
			{
				{BgMode.WORLDSPACE_AR, SetupWorldspaceAR},
				{BgMode.SCREENSPACE_PASSTHROUGH, SetupScreenspacePassthrough},
				{BgMode.SCREENSPACE_COLOR, SetupScreenspaceColor},
				{BgMode.SCREENSPACE_MEDIA, SetupScreenspaceMedia}
			};

		_stateToCleanupFunction = new Dictionary<BgMode, Action>()
			{
				{BgMode.WORLDSPACE_AR, CleanupWorldspaceAR},
				{BgMode.SCREENSPACE_PASSTHROUGH, CleanupScreenspacePassthrough},
				{BgMode.SCREENSPACE_COLOR, CleanupScreenspaceColor},
				{BgMode.SCREENSPACE_MEDIA, CleanupScreenspaceMedia}
			};

		// Make sure the recording camera is set up correctly
		int renderScreenspaceCanvas_mask = 1 << _bgFullScreenContainer.gameObject.layer;
		if (_cameraManager.ScreenspaceCamera_forRecording.cullingMask != renderScreenspaceCanvas_mask)
		{
			Debug.LogWarning($"{_cameraManager.ScreenspaceCamera_forRecording.gameObject.name} should " +
				$"use a culling mask that corresponds to the Screenspace Overlay's layer.", gameObject);
			_cameraManager.ScreenspaceCamera_forRecording.cullingMask = renderScreenspaceCanvas_mask;
		}

		// Listen for the user to change the bg
		_mainMenuController.OnBgModeChanged += OnUserChangedBgMode;
		_mainMenuController.OnScreenspaceBgColorChanged += SetBgColor;		

		_didInitialize = true;
	}

    private void OnDestroy()
    {
		if (_didInitialize)
		{
			_mainMenuController.OnBgModeChanged -= OnUserChangedBgMode;
			_mainMenuController.OnScreenspaceBgColorChanged -= SetBgColor;	
		}
    }

    private void Update()
	{
		// Wait patiently.
		if (!_didInitialize)
		{
			return;
		}

		// Maybe there's a call back for this, but in editor it's when you change the screen size, or
		// more importantly, on device when you switch from portrait to landscape. Or possibly one day
		// same thing on desktop!
		if (_prevScreenDimensions != _currScreenDimensions)
		{
			UpdateScreenDimensions();
			_prevScreenDimensions = _currScreenDimensions;
		}
	}

	private void OnUserChangedBgMode(BgMode newBgMode)
	{
		Debug.Log($"User changed background mode to: {newBgMode}");
		SetBgState(newBgMode);
	}

	private void UpdateScreenDimensions()
	{
		// For the BACKGROUND render layer, scale matters for User Media.
		if (_currBgMode == BgMode.SCREENSPACE_MEDIA)
		{
			RescaleBackgroundImage(_bgTextureRawImage.texture.width, _bgTextureRawImage.texture.height);
		}

		// For the FOREGROUND render layer, it's the Genie herself, so scale matters
		if (_currBgMode == BgMode.SCREENSPACE_PASSTHROUGH ||
			_currBgMode == BgMode.SCREENSPACE_COLOR ||
			_currBgMode == BgMode.SCREENSPACE_MEDIA)
		{
			AssignNewRenderTexture(_fgTextureRawImage, _cameraManager.ScreenspaceCamera);
		}
		// For the BACKGROUND render layer, scale matters for Passthrough.
		// Note, we cannot simply RescaleBackgroundImage because RenderTextures dont scale.
		if (_currBgMode == BgMode.SCREENSPACE_PASSTHROUGH)
		{
			AssignNewRenderTexture(_bgTextureRawImage, _cameraManager.XRCamera);
		}
	}

	private async void SetBgState(BgMode newBgState)
	{
		// This could switch the active/recording camera values and
		// end up in an unexpected state when recording ends.
		if (_recordingManager.IsRecording)
		{
			Debug.LogError("Cannot change background state while recording is active. Make the UI better!");
			return;
		}

		// Cleanup current state
		_stateToCleanupFunction[_currBgMode]();

		// Setup new state
		bool didSucceed = await _stateToSetupFunction[newBgState]();

		// Revert to previous state if needed (e.g. user cancelled media selection)
		if (didSucceed)
		{
			_currBgMode = newBgState;
		}
		else
		{
			await _stateToSetupFunction[_currBgMode]();
		}
		
	}

	// User UI Interactions
	

	// Setup functions
	private void AssignNewRenderTexture(RawImage rawImage, Camera camera)
	{
		// Needs update?
		if (camera.targetTexture != null &&
			camera.targetTexture == rawImage.texture &&
			(int)camera.targetTexture.width == (int)_currScreenDimensions.x &&
			(int)camera.targetTexture.height == (int)_currScreenDimensions.y)
		{
			return;
		}

		// You cannot adjust render texture scale, you must make a new one :'(
		RenderTexture scaledTexture = new RenderTexture((int)_currScreenDimensions.x,
														(int)_currScreenDimensions.y, 0);
		scaledTexture.Create();

		// Set output
		camera.targetTexture = scaledTexture;

		// Set input
		rawImage.texture = scaledTexture;
	}

	private void PresetupScreenspaceState(ActiveCameraType newCameraType)
	{
		_cameraManager.SetActiveCameraType(newCameraType);

		// Turn on/off screen space textures
		_bgTextureRawImage.gameObject.SetActive(_cameraManager.IsScreenspace);
		_fgTextureRawImage.gameObject.SetActive(_cameraManager.IsScreenspace);
	}

	private Task<bool> SetupWorldspaceAR()
	{
		// Turn off BG and FG render layers, we will use the raw XR camera
		PresetupScreenspaceState(ActiveCameraType.XRCamera);

		// This signature is needed to return True for an "async" function
		return Task.FromResult(true);
	}

	private Task<bool> SetupScreenspacePassthrough()
	{
		// Turn on BG (passthrough cam) and FG (genie) render layers
		PresetupScreenspaceState(ActiveCameraType.ScreenspaceCamera);

		// The FG render layer (genie) may already exist
		AssignNewRenderTexture(_fgTextureRawImage, _cameraManager.ScreenspaceCamera);
		// The BG layer well become our passthrough camera's render texture
		AssignNewRenderTexture(_bgTextureRawImage, _cameraManager.XRCamera);

		_cameraManager.XRCamera.cullingMask = _worldspaceCam_cullingMask_minusAvatars;

		// This signature is needed to return True for an "async" function
		return Task.FromResult(true);
	}


	private Task<bool> SetupScreenspaceColor()
	{
		PresetupScreenspaceState(ActiveCameraType.ScreenspaceCamera);

		// The FG render layer (genie) may already exist
		AssignNewRenderTexture(_fgTextureRawImage, _cameraManager.ScreenspaceCamera);
		// The BG layer will remain null, but we can then tint it!

		// Remember user's previous selection
		_bgTextureRawImage.color = _previousBgColor;

		// This signature is needed to return True for an "async" function
		return Task.FromResult(true);
	}

	private async Task<bool> SetupScreenspaceMedia()
	{
		// Reset the TaskCompletionSource
		_userMediaQueryTcs = new TaskCompletionSource<bool>();

		// Begin the async operation
		GetUserMediaPath();

		// Wait until the query is done
		await _userMediaQueryTcs.Task;
		// This sets the _userMediaPath variable
		_userMediaPath = _userMediaPath?.Trim();

		// Make sure they selected something and/or gave permissions
		if (string.IsNullOrEmpty(_userMediaPath))
		{
			Debug.LogWarning("No media selected by user.");
			return false;
		}

		// Check media type
		NativeGallery.MediaType mediaType = NativeGallery.GetMediaTypeOfFile(_userMediaPath);

		bool isValidMedia = mediaType == NativeGallery.MediaType.Image || mediaType == NativeGallery.MediaType.Video;

		Debug.Log($"User Selected Media: {_userMediaPath}.\nType must be Image or Video: {mediaType}. Valid? {isValidMedia}");

		if (isValidMedia)
		{
			Debug.Log("SetupScreenspaceMediaUponSuccess! With media: " + _userMediaPath);

			// Make sure we are in screen space to display it properly
			PresetupScreenspaceState(ActiveCameraType.ScreenspaceCamera);

			// It's either an image or a video at this point.
			if (mediaType == NativeGallery.MediaType.Image)
			{
				SetBackgroundImageFromPath(_userMediaPath);
			}
			else
			{
				SetBackgroundVideoFromPath(_userMediaPath);
			}

			// Make sure the Genie renders on top of the media 
			AssignNewRenderTexture(_fgTextureRawImage, _cameraManager.ScreenspaceCamera);
		}

		return isValidMedia;
	}
	private void GetUserMediaPath()
	{
		// Clear this
		_userMediaPath = null;

		// Show Modal UI
		if (NativeGallery.CanSelectMultipleMediaTypesFromGallery())
		{
			NativeGallery.GetMixedMediaFromGallery((path) =>
			{
				Debug.Log("Setting userSelectedMediaPath: " + path);
				_userMediaPath = path;
				_userMediaQueryTcs?.TrySetResult(true);

			}, NativeGallery.MediaType.Image | NativeGallery.MediaType.Video, "Select an image or video");
		}
	}

	private void CleanupWorldspaceAR() { }

	private void CleanupScreenspacePassthrough()
	{
		_cameraManager.XRCamera.cullingMask = _worldspaceCam_cullingMask_default;
		_cameraManager.XRCamera.targetTexture = null;

		// background texture was created to capture the passthrough camera's
		// IRL render view.
		Destroy(_bgTextureRawImage.texture);
		_bgTextureRawImage.texture = null;
	}

	private void CleanupScreenspaceColor()
	{
		// Color mode may have set a color, so reset it
		_bgTextureRawImage.color = Color.white;
	}

	private void CleanupScreenspaceMedia()
	{
		if (_videoPlayer.isPlaying)
		{
			_videoPlayer.Stop();
		}
		// Video player or image texture may have been set, so we need to clean it up.
		if (_bgTextureRawImage.texture != null)
		{
			Destroy(_bgTextureRawImage.texture);
			_bgTextureRawImage.texture = null;
		}

		// Video player may have rotated the texture, so reset it
		_bgTextureRawImage.transform.localRotation = Quaternion.identity;
	}

    private void SetBgColor(Color color)
    {
		_bgTextureRawImage.color = color;
		_previousBgColor = _bgTextureRawImage.color;
	}


	private void SetBackgroundImageFromPath(string imgPath)
	{
		Debug.Log("Image Path: " + imgPath);

		/*
		// Example: Read bytes from disk! Doesn't work with HEIC
		byte[] bytes = File.ReadAllBytes(imgPath);
		imgTexture = new Texture2D(2, 2);
		imgTexture.LoadImage(bytes);
		bgTextureRawImage.texture = imgTexture;*/

		/*
		// Example: Read file from cloud. Doesn't work with HEIC
		if (!imgPath.StartsWith("file://"))
		{
			imgPath = $"file://{imgPath}";
		}
		WWW www = new WWW(imgPath);
		rawImage.texture = www.texture;*/

		// Very important to use the NativeGallery call, which supports HEIC
		// as well as PNG, JPG, etc.
		//https://github.com/yasirkula/UnityNativeGallery/issues/77
		_bgTextureRawImage.texture = NativeGallery.LoadImageAtPath(imgPath);
		RescaleBackgroundImage(_bgTextureRawImage.texture.width,
							   _bgTextureRawImage.texture.height);
	}

	private void SetBackgroundVideoFromPath(string videoPath)
    {
		StartCoroutine(LoadVideoFromPathAsync(videoPath));
	}

	IEnumerator LoadVideoFromPathAsync(string vidPath)
	{
		Debug.Log("Video Path: " + vidPath);
		
		// Configure video player
		_videoPlayer.source = VideoSource.Url;
		_videoPlayer.url = vidPath.StartsWith("file://") ? vidPath : $"file://{vidPath}";

		// https://discussions.unity.com/t/videoplayer-problem-how-to-check-video-resolution-without-videoclip/192075/2
		_videoPlayer.Prepare();

		Debug.Log("Waiting for video to be Prepared...");
		yield return new WaitUntil(() => _videoPlayer.isPrepared);

		_videoPlayer.isLooping = true;
		_videoPlayer.Play();

		// Get video rotation
		var videoProperties = NativeGallery.GetVideoProperties(vidPath);
		int vidWidth = _videoPlayer.texture.width;
		int vidHeight = _videoPlayer.texture.height;
		
		// Setup render textture
		var oldRenderTexture = _videoPlayer.targetTexture;
		if(oldRenderTexture != null)
        {
			Destroy(oldRenderTexture, 5f);
		}
		_videoPlayer.targetTexture = new RenderTexture(vidWidth, vidHeight, 0);

		// Make sure we are in screen space to display it properly
		PresetupScreenspaceState(ActiveCameraType.ScreenspaceCamera);

		_bgTextureRawImage.texture = _videoPlayer.targetTexture;

		// Videos rotate in dumb ways depending on how they were shot by OP :(
		// For 90 and -90 and 270 and -270, we need to rotate the canvas 90 degrees.
		// We also need to swap these variables so that ratios still work.
		if (Mathf.Abs(videoProperties.rotation) % 180 != 90)
		{
			vidWidth = _videoPlayer.texture.width;
			vidHeight = _videoPlayer.texture.height;
		}
		_bgTextureRawImage.transform.localRotation = Quaternion.Euler(Vector3.forward * -videoProperties.rotation);

		RescaleBackgroundImage(vidWidth, vidHeight);
	}

	void RescaleBackgroundImage(float imageWidth, float imageHeight)
	{
		// Sanity Check
		if (imageHeight == 0 || _currScreenDimensions.y == 0)
		{
			return;
		}

		Vector2 imageDimensions = new Vector2(imageWidth, imageHeight);

		_bgTextureRawImage.transform.localScale = Vector3.one;
		// Unsquash by X
		if (imageDimensions.GetAspectRatio() > _currScreenDimensions.GetAspectRatio())
		{
			Vector3 newScale = Vector3.one;
			newScale.x = imageDimensions.GetAspectRatio() / _currScreenDimensions.GetAspectRatio();
			_bgTextureRawImage.transform.localScale = newScale;
		}
		// Unsquash by Y
		else if (imageDimensions.GetAspectRatio() < _currScreenDimensions.GetAspectRatio())
		{
			Vector3 newScale = Vector3.one;
			newScale.y = (1 / imageDimensions.GetAspectRatio()) / (1 / _currScreenDimensions.GetAspectRatio());
			_bgTextureRawImage.transform.localScale = newScale;
		}
	}

}