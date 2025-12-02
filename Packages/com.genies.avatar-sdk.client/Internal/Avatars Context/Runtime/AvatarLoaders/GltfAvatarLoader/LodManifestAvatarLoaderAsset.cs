using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Genies.Avatars;
using Genies.Refs;
using UnityEngine;

namespace Genies.Avatars.Context
{
    /// <summary>
    /// <see cref="AvatarLoaderAsset"/> implementation for loading avatars from multiple glTF URLs as a LODGroup
    /// instance. The only expected input is a single URL pointing to the lod manifest json file. Which should contain
    /// the information for the different LODs available. It is the serializable asset version of
    /// <see cref="LodManifestAvatarLoader"/>.
    /// </summary>
#if GENIES_INTERNAL
    [CreateAssetMenu(fileName = "LodManifestAvatarLoader", menuName = "Genies/Avatar Loaders/LOD Manifest Avatar Loader")]
#endif
    public sealed class LodManifestAvatarLoaderAsset : AvatarLoaderAsset
    {
        public string lodManifestUrl;

        public GenieGltfImporter.Settings settings = new();

        [Tooltip("How to filter what LODs from the manifest will be included on the load.")]
        public LodManifestAvatarLoader.Filter lodsFilter = LodManifestAvatarLoader.Filter.All;

        [Tooltip("The LOD indices to be loaded when Lods Filter is set to Indices")]
        public List<int> lodIndices = new();

        [Tooltip("The LOD names to be loaded when Lods Filter is set to Names")]
        public List<string> lodNames = new();

        private readonly LodManifestAvatarLoader _loader = new();

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
            _loader.LodManifestUrl = lodManifestUrl;
            _loader.Settings = settings;
            _loader.LodsFilter = lodsFilter;
            _loader.LodIndices = lodIndices;
            _loader.LodNames = lodNames;
        }
    }
}
