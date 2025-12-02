using Cysharp.Threading.Tasks;
using Genies.Avatars;
using Genies.Refs;
using UnityEngine;

namespace Genies.Avatars.Context
{
    /// <summary>
    /// <see cref="AvatarLoaderAsset"/> implementation for loading avatars from multiple glTF URLs as a LODGroup
    /// instance. It is the serializable asset version of <see cref="GltfLodGroupAvatarLoader"/>.
    /// </summary>
#if GENIES_INTERNAL
    [CreateAssetMenu(fileName = "GltfLodGroupAvatarLoader", menuName = "Genies/Avatar Loaders/glTF LOD Group Avatar Loader")]
#endif
    public sealed class GltfLodGroupAvatarLoaderAsset : AvatarLoaderAsset
    {
        public GenieGltfImporter.LodGroupSource lodGroupSource;
        public GenieGltfImporter.Settings settings = new();

        private readonly GltfLodGroupAvatarLoader _loader = new();

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

        private void SyncLoader()
        {
            _loader.LodGroupSource = lodGroupSource;
            _loader.Settings = settings;
        }
    }
}
