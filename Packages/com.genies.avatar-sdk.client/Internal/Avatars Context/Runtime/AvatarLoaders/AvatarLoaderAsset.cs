using Cysharp.Threading.Tasks;
using Genies.Avatars;
using Genies.Refs;
using UnityEngine;

namespace Genies.Avatars.Context
{
    /// <summary>
    /// Extend this class to create <see cref="IAvatarLoader"/> implementation assets.
    /// </summary>
    public abstract class AvatarLoaderAsset : ScriptableObject, IAvatarLoader
    {
        public virtual async UniTask<IGenie> LoadAsync(Transform parent = null)
        {
            ISpeciesGenieController controller = await LoadControllerAsync(parent);
            return controller?.Genie;
        }

        public virtual UniTask<Ref<IGeniePrefab>> LoadAsPrefabAsync()
        {
            Debug.LogError($"[{nameof(AvatarLoaderAsset)}] this loader does not support loading as prefab");
            return UniTask.FromResult<Ref<IGeniePrefab>>(default);
        }

        public abstract UniTask<ISpeciesGenieController> LoadControllerAsync(Transform parent = null);
    }
}