using Cinemachine;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

namespace Genies.Sdk.Samples.AvatarStarter
{
    public class LoadMyAvatar : MonoBehaviour
    {
        [Header("New Input System Speeds")]
        public float FreeLookXAxisSpeedNewInputSystem = 1f;
        public float FreeLookYAxisSpeedNewInputSystem = 0.01f;
        public InputSystemUIInputModule InputSystemUIInputModule;

        [Header("Legacy Input System Speeds")]
        public float FreeLookXAxisSpeedLegacyInputSystem = 10f;
        public float FreeLookYAxisSpeedLegacyInputSystem = 0.1f;
        public StandaloneInputModule StandaloneInputModule;

        public ManagedAvatar LoadedAvatar;

        [Header("Scene References")]
        public GeniesAvatarController loadedController;
        public RuntimeAnimatorController OptionalController;
        public CinemachineFreeLook CinemachineFreeLookSettings;

        private void Awake()
        {
            // Avatar controller will eat inputs... dont enable until we're done logging in.
            loadedController.gameObject.SetActive(false);

            // The CinemachineInputProvider is used for the new Input System.
            // If we are using the old Input Manager, this component will cause issues and should be removed.
#if !ENABLE_INPUT_SYSTEM
            var inputProvider = CinemachineFreeLookSettings.GetComponent<CinemachineInputProvider>();
            if (inputProvider != null)
            {
                Destroy(inputProvider);
            }
#endif

#if ENABLE_INPUT_SYSTEM
            CinemachineFreeLookSettings.m_XAxis.m_MaxSpeed = FreeLookXAxisSpeedNewInputSystem;
            CinemachineFreeLookSettings.m_YAxis.m_MaxSpeed = FreeLookYAxisSpeedNewInputSystem;

            if (InputSystemUIInputModule != null)
            {
                InputSystemUIInputModule.enabled = true;
            }
            if (StandaloneInputModule != null)
            {
                Destroy(StandaloneInputModule);
            }
#else
            CinemachineFreeLookSettings.m_XAxis.m_MaxSpeed = FreeLookXAxisSpeedLegacyInputSystem;
            CinemachineFreeLookSettings.m_YAxis.m_MaxSpeed = FreeLookYAxisSpeedLegacyInputSystem;

            if (StandaloneInputModule != null)
            {
                StandaloneInputModule.enabled = true;
            }
            if (InputSystemUIInputModule != null)
            {
                Destroy(InputSystemUIInputModule);
            }
#endif
        }

        private void Start()
        {
            if (!AvatarSdk.IsLoggedIn)
            {
                AvatarSdk.Events.UserLoggedIn += LoadAvatar;

                return;
            }

            LoadAvatar();
        }

        private async void LoadAvatar()
        {
            if (loadedController != null)
            {
                // Parenting the loaded avatar to an inactive GO and then immediately activating it will crash the application.
                // Activate the parent object first.
                loadedController.gameObject.SetActive(true);
            }

            LoadedAvatar = await AvatarSdk.LoadUserAvatarAsync(
                parent: loadedController.transform,
                playerAnimationController: OptionalController != null ? OptionalController : null);

            var animatorEventBridge = LoadedAvatar.Root.gameObject.AddComponent<GeniesAnimatorEventBridge>();

            if (loadedController != null)
            {
                loadedController.SetAnimatorEventBridge(animatorEventBridge);
                loadedController.GenieSpawned = true;

                if (CinemachineFreeLookSettings.gameObject != null)
                {
                    CinemachineFreeLookSettings.gameObject.SetActive(true);
                    CinemachineFreeLookSettings.Follow = loadedController.CinemachineCameraTarget.transform;
                    CinemachineFreeLookSettings.LookAt = loadedController.CinemachineCameraTarget.transform;
                }
            }
        }
    }
}
