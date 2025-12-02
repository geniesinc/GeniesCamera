using Cysharp.Threading.Tasks;
using Genies.Avatars;
using Genies.Refs;
using UnityEngine;

namespace Genies.Avatars.Context
{
    /// <summary>
    /// <see cref="AvatarLoaderAsset"/> implementation for loading avatars from a glTF URL. It is the serializable asset
    /// version of <see cref="GltfAvatarLoader"/>.
    /// </summary>
#if GENIES_INTERNAL
    [CreateAssetMenu(fileName = "GltfAvatarLoader", menuName = "Genies/Avatar Loaders/glTF Avatar Loader")]
#endif
    public sealed class GltfAvatarLoaderAsset : AvatarLoaderAsset
    {
        public string url;
        public GenieGltfImporter.Settings settings = new();

        private readonly GltfAvatarLoader _loader = new();

        public override UniTask<IGenie> LoadAsync(Transform parent = null)
        {
            SyncLoader();
            return _loader.LoadAsync(parent);
        }

        public override UniTask<Ref<IGeniePrefab>> LoadAsPrefabAsync()
        {
            SyncLoader();
            return _loader.LoadAsPrefabAsync();
        }

        public override UniTask<ISpeciesGenieController> LoadControllerAsync(Transform parent = null)
        {
            SyncLoader();
            return _loader.LoadControllerAsync(parent);
        }

        /// <summary>
        /// Forces the unload from memory of any genies loaded from the current URL, even if they are still in use.
        /// Don't use this method unless you know what you are doing.
        /// </summary>
        [ContextMenu("Force Unload")]
        public void ForceUnload()
        {
            SyncLoader();
            _loader.ForceUnload();
        }

        private void SyncLoader()
        {
            _loader.Url = url;
            _loader.Settings = settings;
        }
    }
}
