using Cysharp.Threading.Tasks;
using Genies.Animations;
using Genies.CameraSystem;
using Genies.Looks.Models;
using Genies.Avatars.Behaviors;
using Genies.Looks.Core.Data;
using UnityEngine;

namespace Genies.Looks.Core
{
    /// <summary>
    /// Class used for rendering looks at runtime with full real-time interaction support.
    /// This implementation of <see cref="IRealtimeLookView"/> provides complete avatar look rendering,
    /// including avatar controller management, camera system integration, and animation support.
    /// </summary>
    public class LookRealtimeView : MonoBehaviour, IRealtimeLookView
    {
        // Controllers
        /// <inheritdoc />
        public IAvatarController AvatarController { get; set; }
        /// <inheritdoc />
        public GameObject LooksViewObject { get; set; }
        /// <inheritdoc />
        public string Id => _currentLook.Id;


        // Class Variables
        private VirtualCameraController<AnimationVirtualCameraCatalog> _virtualCameraController;
        private bool IsReadyToShow { get; set; }

        private LookData _currentLook;
        private bool _lockAnimationPlayback;

        /// <inheritdoc />
        public async UniTask Initialize(LookData look, LooksDependencies dependencies)
        {
            IsReadyToShow = false;
            LooksViewObject = gameObject;

            // Create Avatar controller
            AvatarController = await AvatarControllerFactory.CreateNafGenie(null, transform);

            //Setup camera
            _virtualCameraController = dependencies.VirtualCameraService.AnimationCameraController;
            _virtualCameraController.SetAnimatedCamera(AvatarController.AnimatedCamera);

            // Animation Dependencies
            var components = dependencies.SwitcherComponents;

            // Set the animation controller
            components.UmaAnimator = AvatarController.Animator;
            components.OnAnimationLoopStarted += ResetBodyVariationValues;

            transform.position = Vector3.zero;

            // Set look definition
            await SetDefinition(look);
            IsReadyToShow = true;

            _virtualCameraController.SetFocusableInFocusCamera(AnimationVirtualCameraCatalog.FullBodyFocusCamera, AvatarController.Focusable);
        }

        /// <inheritdoc />
        public void ToggleAnimationPlayBackLock(bool value) => _lockAnimationPlayback = value;

        /// <inheritdoc />
        public async UniTask SetDefinition(LookData look)
        {
#if !PRODUCTION_BUILD
            var executeTime = Time.time;
#endif
            UniTask loadAvatar = AvatarController.SetDefinition(look.AvatarDefinition);
            await UniTask.WhenAll(loadAvatar);

            _currentLook = look;


            //TODO fix, once we have devtools packaged
// #if !PRODUCTION_BUILD
//             if (GeniesSceneContext.Context.DevTools.AssetToastMessageEnabled)
//             {
//                 executeTime = Time.time - executeTime;
//
//                 var toast = this.GetService<ToastMessage>(DevTools.DevToastKey);
//                 toast.ShowMessageSuccess(0.5f, 1f, "Look Definition Loaded: "+ executeTime.ToString("F2")+"s");
//             }
// #endif
        }

        /// <inheritdoc />
        public UniTask<LookData> GetDefinition()
        {
            _currentLook.AvatarDefinition = AvatarController.GetDefinition();

            return UniTask.FromResult(_currentLook);
        }

        /// <inheritdoc />
        public UniTask PreDownload()
        {
            throw new System.NotImplementedException();
        }

        /// <inheritdoc />
        public UniTask<Texture2D> GetThumbnail()
        {
            throw new System.NotImplementedException();
        }

        /// <inheritdoc />
        public async UniTask<bool> IsReadyToShowAsync()
        {
            await UniTask.WaitUntil(() => IsReadyToShow);
            return true;
        }

        /// <summary>
        /// Resets the body variation values to the current preset.
        /// This method is called when animation loops start to ensure consistent appearance.
        /// </summary>
        public async void ResetBodyVariationValues()
        {
            var preset = AvatarController.Controller.GetBodyPreset();
            await AvatarController.Controller.SetBodyPresetAsync(preset);
            Destroy(preset);
        }

        /// <inheritdoc />
        public void Clean()
        {
            _virtualCameraController.SetAnimatedCamera(null);
            AvatarController.Dispose();
            Destroy(gameObject);
        }
    }
}
