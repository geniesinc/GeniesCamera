using Cysharp.Threading.Tasks;
using Genies.Avatars;
using UnityEngine;

namespace Genies.Avatars.Context
{
    public static class AvatarLoaderExtensions
    {
        public static async UniTask<T> LoadControllerAsync<T>(this IAvatarLoader loader, Transform parent = null)
            where T : ISpeciesGenieController
        {
            ISpeciesGenieController controller = await loader.LoadControllerAsync(parent);
            if (controller is T validController)
            {
                return validController;
            }

            Debug.LogError($"The loaded genie controller is of type {controller.GetType().Name} but you expected type {typeof(T).Name}");
            controller?.Dispose();
            return default;
        }

        public static async UniTask<IGenie> LoadBakeAsync(this IAvatarLoader loader, Transform parent = null, bool urpBake = false)
        {
            IGenie genie = await loader.LoadAsync(parent);
            IGenie bakedGenie = await genie.BakeAsync(parent, urpBake);
            genie.Dispose();

            return bakedGenie;
        }

        public static async UniTask<IGenieSnapshot> LoadSnapshotAsync(this IAvatarLoader loader,
            RuntimeAnimatorController pose = null, Transform parent = null, bool urpBake = false)
        {
            IGenie genie = await loader.LoadAsync(parent);

            if (pose && genie.Animator)
            {
                await UniTask.Yield();
                genie.Animator.runtimeAnimatorController = pose;
                genie.Animator.Update(0.0f);
                await UniTask.Yield();
            }

            IGenieSnapshot genieSnapshot = await genie.TakeSnapshotAsync(parent, urpBake);
            genie.Dispose();

            return genieSnapshot;
        }
    }
}
