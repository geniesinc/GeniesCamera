using Cinemachine;
using Genies.Login.Native;
using UnityEngine;
using UnityEngine.Serialization;

namespace Genies.Avatars.Sdk.LoadMyAvatar
{
#if GENIES_SDK && !GENIES_INTERNAL
    internal class LoadMyAvatar : MonoBehaviour
#else
    public class LoadMyAvatar : MonoBehaviour
#endif
    {
        public bool ShouldLoadController = false;
        public GameObject AvatarSpawnLocation;
        public RuntimeAnimatorController OptionalController;
        public CinemachineFreeLook CinemachineFreeLookSettings;
        private GeniesAvatarController loadedController;

        private async void Start()
        {
            await GeniesLoginSdk.WaitUntilLoggedInAsync();
            var resultingAvatar = await GeniesAvatarsSdk.LoadUserAvatarAsync(
                parent: AvatarSpawnLocation != null ? AvatarSpawnLocation.transform : null,
                playerAnimationController: OptionalController != null ? OptionalController : null);

            if (ShouldLoadController)
            {
                // This will set the avatar as a child of the controller. We need to do this so we can properly
                // do camera based things, etc. We can't really set a parent for the loaded controller, as it will change its parent
                // when it collides with something that is tagged as a "ground layer"...mostly for moving platform purposes.
                loadedController = GeniesAvatarsSdk.InstantiateDefaultController(resultingAvatar);
            }

            if (loadedController != null)
            {
                loadedController.gameObject.SetActive(true);

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
