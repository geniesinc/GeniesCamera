using Cysharp.Threading.Tasks;
using Genies.Avatars;
using Genies.Refs;
using UnityEngine;

namespace Genies.Avatars.Context
{
    public sealed class GltfLodGroupAvatarLoader : IAvatarLoader
    {
        public GenieGltfImporter.LodGroupSource LodGroupSource;
        public GenieGltfImporter.Settings Settings;
        
        public UniTask<IGenie> LoadAsync(Transform parent = null)
        {
            return GenieGltfImporter.ImportAsync(LodGroupSource, parent, Settings);
        }
        
        public UniTask<Ref<IGeniePrefab>> LoadAsPrefabAsync()
        {
            return GenieGltfImporter.ImportAsPrefabAsync(LodGroupSource, Settings);
        }

        public UniTask<ISpeciesGenieController> LoadControllerAsync(Transform parent = null)
        {
            Debug.LogError($"[{nameof(GltfLodGroupAvatarLoader)}] glTF avatars are readonly so a controller cannot be instantiated from it");
            return UniTask.FromResult<ISpeciesGenieController>(null);
        }
    }
}