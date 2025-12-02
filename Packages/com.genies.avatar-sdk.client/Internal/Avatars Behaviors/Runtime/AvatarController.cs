using System.Linq;
using Cysharp.Threading.Tasks;
using Genies.CameraSystem;
using Genies.CameraSystem.Focusable;
using Genies.Naf;
using Genies.PerformanceMonitoring;
using UnityEngine;

namespace Genies.Avatars.Behaviors
{
    /// <summary>
    /// The avatar controller manages avatars as individual instances, providing functionality for avatar creation, animation control, and lifecycle management.
    /// This class serves as the main implementation of the IAvatarController interface and handles avatar behavior orchestration.
    /// </summary>
    public class AvatarController : MonoBehaviour, IAvatarController
    {
        /// <summary>
        /// Gets the GameObject containing the genie (avatar) representation.
        /// </summary>
        public GameObject GenieGameObject => Controller.Genie.Root;

        /// <summary>
        /// Gets the parent GameObject that contains the avatar controller.
        /// </summary>
        public GameObject ParentGameObject => this.gameObject;

        /// <summary>
        /// Gets the underlying NativeUnifiedGenieController that handles avatar functionality.
        /// </summary>
        public NativeUnifiedGenieController Controller { get; private set; }

        /// <summary>
        /// Gets the GenieAnimationController that manages avatar animations.
        /// </summary>
        public GenieAnimationController AnimationController { get; private set; }

        /// <summary>
        /// Gets or sets the Animator component responsible for avatar animations.
        /// </summary>
        public Animator Animator { get; set; }

        /// <summary>
        /// Gets or sets the camera used for animating and viewing the avatar.
        /// </summary>
        public Camera AnimatedCamera { get; set; }

        /// <summary>
        /// Gets the focusable component that allows camera targeting of this avatar.
        /// </summary>
        public IFocusable Focusable { get; private set; }

        /// <summary>
        /// Gets the RuntimeAnimatorController used for avatar animations.
        /// </summary>
        public RuntimeAnimatorController RuntimeAnimatorController { get; private set; }

        /// <summary>
        /// Gets a value indicating whether the avatar controller has been loaded and is ready for use.
        /// </summary>
        public bool IsLoaded => Controller != null;

        /// <summary>
        /// Gets a value indicating whether the avatar is currently visible and active.
        /// </summary>
        public bool IsVisible => isActiveAndEnabled;

        /// <summary>
        /// Gets a value indicating whether the avatar definition has been modified and needs to be saved.
        /// </summary>
        public bool IsDirty { get; private set; } // whether or not the SavedDefinition is in sync with the current state of the controller

        private SkinnedMeshRenderer _skinnedMeshRenderer;

        private CustomInstrumentationManager _instrumentationManager => CustomInstrumentationManager.Instance;
        private static string _rootTransaction => CustomInstrumentationOperations.LoadAvatarTransaction;

        /// <summary>
        /// Initializes the avatar controller with the specified NativeUnifiedGenieController.
        /// This method sets up all necessary components including animation controllers, cameras, and interaction capabilities.
        /// </summary>
        /// <param name="genieController">The NativeUnifiedGenieController to be managed by this avatar controller.</param>
        public void Initialize(NativeUnifiedGenieController genieController)
        {
            Controller = genieController;

            // move the camera as a child of the genie
            var animatedCamera = GetComponentInChildren<Camera>();
            animatedCamera.transform.SetParent(genieController.Genie.Root.transform, false);

            // setup the unified genie with all the stuff needed for the ComposerApp
            AnimationController = GetComponent<GenieAnimationController>();
            Animator = GetComponentInChildren<Animator>();
            RuntimeAnimatorController = Animator.runtimeAnimatorController;
            AnimatedCamera = GetComponentInChildren<Camera>();
            GenieFocusable genieFocusable = Controller.Genie.Root.AddComponent<GenieFocusable>();
            genieFocusable.Initialize(Controller.Genie);
            Focusable = genieFocusable;
            _skinnedMeshRenderer ??= GetComponentInChildren<SkinnedMeshRenderer>();

            transform.localPosition = Vector3.zero;
        }

        /// <summary>
        /// Initializes the avatar controller for custom avatars without a NativeUnifiedGenieController.
        /// This method is used for avatars that don't utilize the full Genie system but still need animation and camera setup.
        /// </summary>
        public void InitializeCustomAvatar()
        {
            // Initializes everything unrelated to NativeUnifiedGenieController for the custom avatar

            // move the camera as a child of the genie
            var animatedCamera = GetComponentInChildren<Camera>();
            if(animatedCamera != null)
            {
                animatedCamera.transform.SetParent(transform, false);
            }

            // setup the unified genie with all the stuff needed for the ComposerApp
            AnimationController = GetComponent<GenieAnimationController>();
            Animator = GetComponentInChildren<Animator>();
            if(Animator != null)
            {
                RuntimeAnimatorController = Animator.runtimeAnimatorController;
            }

            AnimatedCamera = GetComponentInChildren<Camera>();
            _skinnedMeshRenderer ??= GetComponentInChildren<SkinnedMeshRenderer>();

            transform.localPosition = Vector3.zero;
        }

        /// <summary>
        /// Sets the avatar definition to configure the avatar's appearance and properties.
        /// This method includes performance tracking and applies the definition asynchronously.
        /// </summary>
        /// <param name="definition">The JSON string containing the avatar definition.</param>
        /// <returns>A task that completes when the definition has been applied.</returns>
        public async UniTask SetDefinition(string definition)
        {
            if (definition == null)
            {
                return;
            }

            //track
            var wasTracked = false;
            if (!_instrumentationManager.RunningTransactions.Contains(_rootTransaction))
            {
                _instrumentationManager.StartTransaction(_rootTransaction, "AvatarController.SetDefinition");
                wasTracked = true;
            }

            //set
            await Controller.SetDefinitionAsync(definition);

            if (wasTracked)
            {
                _instrumentationManager.FinishTransaction(_rootTransaction);
            }
        }

        /// <summary>
        /// Gets the current avatar definition as a JSON string.
        /// </summary>
        /// <returns>The JSON representation of the current avatar definition.</returns>
        public string GetDefinition()
        {
            return Controller.GetDefinition();
        }

        /// <summary>
        /// Disposes of the avatar controller and cleans up all associated resources.
        /// This method destroys GameObjects, disposes controllers, and frees memory.
        /// </summary>
        public void Dispose()
        {
            Controller.Dispose();
            Controller.Genie?.Dispose();

            Destroy(gameObject);

            Destroy(Controller?.Genie?.Root.gameObject);

            if(GenieGameObject != null)
            {
                Destroy(GenieGameObject.gameObject);
            }
        }

        /// <summary>
        /// Sets the avatar to use its default animation controller.
        /// Restores the original RuntimeAnimatorController that was configured during initialization.
        /// </summary>
        public void SetDefaultAvatarAnimation()
        {
            Animator.runtimeAnimatorController = RuntimeAnimatorController;
        }
    }
}
