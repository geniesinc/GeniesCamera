using System;
using Cysharp.Threading.Tasks;
using Genies.Avatars.Behaviors;
using Genies.Avatars.Sdk;
using Genies.Avatars.Services;
using Genies.CameraSystem;
using Genies.CrashReporting;
using Genies.Customization.Framework;
using Genies.Customization.Framework.Navigation;
using Genies.Customization.MegaEditor;
using Genies.ServiceManagement;
using Genies.UI.Animations;
using Genies.UIFramework;
using Genies.VirtualCamera;
using UnityEngine;
using Animator = UnityEngine.Animator;
using Ease = Genies.UI.Animations.Ease;

namespace Genies.AvatarEditor.Core
{
#if GENIES_SDK && !GENIES_INTERNAL
    internal class AvatarEditingBehaviour
#else
    public class AvatarEditingBehaviour
#endif
    {
        private AvatarEditingScreen Screen { get; }

        private VirtualCameraManager VirtualCameraManager { get; }

        private VirtualCameraController<GeniesVirtualCameraCatalog> AvatarEditingVirtualCameraController { get; }

        private Customizer _customizer;
        private GeniesAvatar _currentCustomizedAvatar;
        private Animator _currentAvatarAnimator;
        private string _currentDefinition;
        private string _lastSavedDefinition; // Tracks the last successfully saved state
        private GameObject _currentCamera;
        private CameraState _originalCameraState;
        private Quaternion _originalAvatarRotation;
        public AvatarEditorMode CurrentEditMode;
        private IAvatarService _AvatarService => this.GetService<IAvatarService>();
        private IAvatarEditorSdkService _AvatarEditorSdkService => this.GetService<IAvatarEditorSdkService>();

        // Camera transition settings
        private const float CameraTransitionDuration = 2;
        private const Ease CameraTransitionEase = Ease.OutCubic;

        // Struct to store complete camera state
        private struct CameraState
        {
            public Vector3 Position;
            public Quaternion Rotation;
            public Vector3 LocalScale;
            public float FieldOfView;
            public float NearClipPlane;
            public float FarClipPlane;
            public bool Orthographic;
            public float OrthographicSize;
            public float Depth;
            public RenderingPath RenderingPath;
            public RenderTexture TargetTexture;
            public bool UseOcclusionCulling;
            public LayerMask CullingMask;
            public Color BackgroundColor;
            public CameraClearFlags ClearFlags;
            public bool HadAudioListener;
            public bool AudioListenerEnabled;
        }

        // Target category to navigate to when screen initializes
        private string _targetCategory = null;

        private bool IsDiscarding { get; set; }

        private AvatarSaveSettings _saveSettings = new AvatarSaveSettings(AvatarSaveOption.SaveRemotelyAndContinue);

        public AvatarEditingBehaviour(AvatarEditingScreen screen, VirtualCameraManager virtualCameraManager)
        {
            Screen = screen;
            _customizer = Screen.customizer;

            VirtualCameraManager = virtualCameraManager;
            AvatarEditingVirtualCameraController = virtualCameraManager.virtualCameraController;
        }

        public void SetTargetCategory(string categoryName)
        {
            _targetCategory = categoryName;
        }


        /// <summary>
        /// Sets the save behavior for the avatar editor.
        /// </summary>
        /// <param name="saveOption">The save option to use when saving the avatar</param>
        public void SetSaveOption(AvatarSaveOption saveOption)
        {
            SetSaveSettings(new AvatarSaveSettings(saveOption, _saveSettings.ProfileId));
        }

        /// <summary>
        /// Sets the save settings for the avatar editor, including custom profile ID.
        /// </summary>
        /// <param name="saveSettings">The save settings to use when saving the avatar</param>
        public void SetSaveSettings(AvatarSaveSettings saveSettings)
        {
            if (_saveSettings.SaveOption == saveSettings.SaveOption && _saveSettings.ProfileId == saveSettings.ProfileId)
            {
                return;
            }

            // If the customizer is currently active, we need to update the event handlers immediately
            if (_customizer != null)
            {
                // Remove the current save event handler
                if (_saveSettings.SaveOption == AvatarSaveOption.SaveRemotelyAndContinue || _saveSettings.SaveOption == AvatarSaveOption.SaveLocallyAndContinue)
                {
                    _customizer.SaveRequested -= Save;
                }
                else
                {
                    _customizer.SaveRequested -= SaveAndExit;
                }
            }

            // Update the save settings
            _saveSettings = saveSettings;

            // If the customizer is currently active, add the new save event handler
            if (_customizer != null)
            {
                if (_saveSettings.SaveOption == AvatarSaveOption.SaveRemotelyAndContinue || _saveSettings.SaveOption == AvatarSaveOption.SaveLocallyAndContinue)
                {
                    _customizer.SaveRequested += Save;
                }
                else
                {
                    _customizer.SaveRequested += SaveAndExit;
                }
            }
        }

        public async UniTask ProcessInitialization()
        {
            Screen.ToggleCustomizer(AvatarEditorMode.Avatar);
            Screen.ToggleCustomizer(AvatarEditorMode.Outfit);

            CurrentEditMode = AvatarEditorMode.Outfit;

            if (!string.IsNullOrWhiteSpace(_targetCategory))
            {
                // Switch to outfit mode if we have a target category
                Screen.ToggleCustomizer(AvatarEditorMode.Outfit);
                bool navigatedSuccessfully = await _customizer.NavigateToNestedCategory(Screen.outfitRootNode, _targetCategory);

                if (!navigatedSuccessfully)
                {
                    await _customizer.RefreshItemPicker();
                }

                _targetCategory = null;
            }
            else
            {
                await _customizer.RefreshItemPicker();
            }
        }

        public async UniTask StartEditing(GeniesAvatar avatar, Camera camera,
            NavigationGraph navGraph)
        {
            SetupCustomizationContext(avatar, camera);

            _currentDefinition = _currentCustomizedAvatar.GetDefinition();

            // Set up save behavior based on the configured save option
            if (_saveSettings.SaveOption == AvatarSaveOption.SaveRemotelyAndContinue || _saveSettings.SaveOption == AvatarSaveOption.SaveLocallyAndContinue)
            {
                _customizer.SaveRequested += Save;
            }
            else
            {
                _customizer.SaveRequested += SaveAndExit;
            }

            _customizer.ExitRequested += Exit;

            FocusOnAvatar();

            await _customizer.StartCustomization(navGraph);
        }

        public async UniTask StopEditing()
        {
            UniTask stopCustomizationTask = new UniTask();

            if (_customizer != null)
            {
                // Remove save behavior based on the configured save option
                if (_saveSettings.SaveOption == AvatarSaveOption.SaveRemotelyAndContinue || _saveSettings.SaveOption == AvatarSaveOption.SaveLocallyAndContinue)
                {
                    _customizer.SaveRequested -= Save;
                }
                else
                {
                    _customizer.SaveRequested -= SaveAndExit;
                }

                _customizer.ExitRequested -= Exit;

                stopCustomizationTask = _customizer.StopCustomization();
            }

            // Reset context
            ResetCustomizationContext();

            await UniTask.WhenAll(
                stopCustomizationTask,
                ResetCamera()
            );
        }

        private void FocusOnAvatar()
        {
            if (_currentCustomizedAvatar == null)
            {
                return;
            }

            AvatarEditingVirtualCameraController.ActivateVirtualCamera(GeniesVirtualCameraCatalog.FullBodyFocusCamera);
        }

        private void SaveAndExit()
        {
            Screen.PreviewSpinner.Show();
            ProcessAvatarSaved().Forget();
        }

        private void Save()
        {
            Screen.PreviewSpinner.Show();
            ProcessAvatarAndContinue().Forget();
            Screen.ToggleCustomizer(CurrentEditMode);
        }

        private async UniTask ProcessAvatarSaved()
        {
            await SaveAvatarDefinition();
            Screen.PreviewSpinner.Hide();
            await _AvatarEditorSdkService.CloseEditorAsync();
        }

        private async UniTask ProcessAvatarAndContinue()
        {
            await SaveAvatarDefinition();
            Screen.PreviewSpinner.Hide();
        }

        private async UniTask SaveAvatarDefinition()
        {
            try
            {
                // Update avatar definition

                var definition = _currentCustomizedAvatar.Controller.GetDefinitionType();

                if (_saveSettings.SaveOption == AvatarSaveOption.SaveLocallyAndContinue || _saveSettings.SaveOption == AvatarSaveOption.SaveLocallyAndExit)
                {
                    var profileId = string.IsNullOrEmpty(_saveSettings.ProfileId) ? LocalAvatarProcessor.NewTemplateName : _saveSettings.ProfileId;
                    _AvatarEditorSdkService.SaveAvatarDefinitionLocally(_currentCustomizedAvatar, profileId);
                }
                else
                {
                    await _AvatarService.UpdateAvatarAsync(definition);
                }

                // Update the last saved definition after successful save
                _lastSavedDefinition = _currentCustomizedAvatar.GetDefinition();
            }
            catch (Exception e)
            {
                CrashReporter.LogError($"Failed to save Avatar Editor Avatar Definition {e}");
            }
        }

        private void Exit()
        {
            _AvatarEditorSdkService.CloseEditorAsync().Forget();
        }

        public async UniTask DiscardAndExit()
        {
            if (IsDiscarding) { return; }
            IsDiscarding = true;

            await StopEditing();

            if (_currentCustomizedAvatar is not null)
            {
                // Revert to the last saved definition instead of the original definition
                await _currentCustomizedAvatar.SetDefinitionAsync(_lastSavedDefinition);
            }
        }

        private void SetupCustomizationContext(GeniesAvatar avatar, Camera camera)
        {
            _currentCamera = camera.gameObject;

            _currentCustomizedAvatar = avatar;
            _currentDefinition = _currentCustomizedAvatar.GetDefinition();
            _lastSavedDefinition = _currentDefinition; // Initialize last saved state to current state

            _originalCameraState = CaptureCameraState(camera);

            SetupAvatarInteraction();

            AvatarEditingVirtualCameraController.UpdateViewportInFocusCameras(Screen.editingViewPort);
            CustomizationContext.SetCustomizableAvatar(_currentCustomizedAvatar.Controller, null);
            CustomizationContext.SetVirtualCameraController(AvatarEditingVirtualCameraController);
        }

        private void SetupAvatarInteraction()
        {
            var root = _currentCustomizedAvatar.Controller.Genie.Root;

            // Terminate any existing animations on the avatar Transform
            root.transform.Terminate();

            // Capture the current rotation as the original (before reset)
            _originalAvatarRotation = root.transform.rotation;

            // Immediately reset rotation to identity BEFORE setting up camera
            root.transform.rotation = Quaternion.identity;

            var interactionController = ServiceManager.Get<GenieInteractionController>();

            if (interactionController != null)
            {
                interactionController.Controllable = root;
                interactionController.SmoothReset();
                interactionController.SetEnabled(true);
            }
            else
            {
                CrashReporter.LogWarning("GenieInteractionController service not found");
            }
        }

        private async void ResetCustomizationContext()
        {
            await ResetAvatarInteraction();

            AvatarEditingVirtualCameraController.UpdateViewportInFocusCameras(null);
            CustomizationContext.SetCustomizableAvatar(null, null);
            CustomizationContext.SetRealtimeLookView(null);
        }

        private async UniTask ResetAvatarInteraction()
        {
            // Clean up the interaction controller
            var interactionController = ServiceManager.Get<GenieInteractionController>();

            if (interactionController != null && interactionController.Controllable != null)
            {
                var controllable = interactionController.Controllable;

                controllable.transform.Terminate();
                interactionController.SetEnabledWithoutResetting(false);

                interactionController.Controllable = null;

                // Animate rotation back to original
                var rotationAnimation = controllable.transform.AnimateRotateQuaternion(
                    _originalAvatarRotation,
                    CameraTransitionDuration)
                    .SetEase(CameraTransitionEase);

                // Wait for animation to complete
                await rotationAnimation.WaitForCompletion();
            }
        }


        public void SwitchToNode(INavigationNode targetNode)
        {
            _customizer.GoToNode(targetNode, false);
        }

        private async UniTask ResetCamera()
        {
            DeactivateVirtualCamera();
            await SmoothResetToOriginalCamera();
        }



        private void DeactivateVirtualCamera()
        {
            if (Screen == null || Screen.transform.parent == null)
            {
                return;
            }

            var virtualCameraManager = Screen.transform.parent.GetComponentInChildren<VirtualCameraManager>();
            if (virtualCameraManager == null)
            {
                return;
            }

            if (_currentCamera == null) { return; }

            if (ReferenceEquals(_currentCamera, virtualCameraManager.CameraActiveCurrent.gameObject) is false)
            {
                return;
            }

            virtualCameraManager.Deactivate();

        }

        private async UniTask SmoothResetToOriginalCamera()
        {
            if (_currentCamera == null)
            {
                return;
            }

            Camera camera = _currentCamera.GetComponent<Camera>();
            if (camera == null)
            {
                return;
            }

            // Capture current camera state as the starting point for transition
            Vector3 startPos = camera.transform.position;
            Quaternion startRot = camera.transform.rotation;
            float startFov = camera.fieldOfView;

            // Target is the original camera state
            Vector3 targetPos = _originalCameraState.Position;
            Quaternion targetRot = _originalCameraState.Rotation;
            float targetFov = _originalCameraState.FieldOfView;
            float duration = CameraTransitionDuration;

            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);

                // Apply easing curve for smoother transition
                float easedT = UIVirtual.EasedValue(0f, 1f, t, CameraTransitionEase);

                // Smoothly transition position, rotation, and field of view
                camera.transform.position = Vector3.Lerp(startPos, targetPos, easedT);
                camera.transform.rotation = Quaternion.Slerp(startRot, targetRot, easedT);
                camera.fieldOfView = Mathf.Lerp(startFov, targetFov, easedT);

                await UniTask.Yield();
            }

            RestoreAllCameraSettings(camera, _originalCameraState);
        }

        private CameraState CaptureCameraState(Camera camera)
        {
            var audioListener = camera.GetComponent<AudioListener>();
            return new CameraState
            {
                Position = camera.transform.position,
                Rotation = camera.transform.rotation,
                LocalScale = camera.transform.localScale,
                FieldOfView = camera.fieldOfView,
                NearClipPlane = camera.nearClipPlane,
                FarClipPlane = camera.farClipPlane,
                Orthographic = camera.orthographic,
                OrthographicSize = camera.orthographicSize,
                Depth = camera.depth,
                RenderingPath = camera.renderingPath,
                TargetTexture = camera.targetTexture,
                UseOcclusionCulling = camera.useOcclusionCulling,
                CullingMask = camera.cullingMask,
                BackgroundColor = camera.backgroundColor,
                ClearFlags = camera.clearFlags,
                HadAudioListener = audioListener != null,
                AudioListenerEnabled = audioListener != null && audioListener.enabled
            };
        }

        private void RestoreAllCameraSettings(Camera camera, CameraState state)
        {
            // Transform is already animated, just ensure final values
            camera.transform.position = state.Position;
            camera.transform.rotation = state.Rotation;
            camera.transform.localScale = state.LocalScale;

            // Restore all camera settings
            camera.fieldOfView = state.FieldOfView;
            camera.nearClipPlane = state.NearClipPlane;
            camera.farClipPlane = state.FarClipPlane;
            camera.orthographic = state.Orthographic;
            camera.orthographicSize = state.OrthographicSize;
            camera.depth = state.Depth;
            camera.renderingPath = state.RenderingPath;
            camera.targetTexture = state.TargetTexture;
            camera.useOcclusionCulling = state.UseOcclusionCulling;
            camera.cullingMask = state.CullingMask;
            camera.backgroundColor = state.BackgroundColor;
            camera.clearFlags = state.ClearFlags;

            // Restore AudioListener state
            var audioListener = camera.GetComponent<AudioListener>();
            if (state.HadAudioListener && audioListener != null)
            {
                audioListener.enabled = state.AudioListenerEnabled;
            }
            else if (!state.HadAudioListener && audioListener != null)
            {
                audioListener.enabled = false;
            }
        }
    }
}
