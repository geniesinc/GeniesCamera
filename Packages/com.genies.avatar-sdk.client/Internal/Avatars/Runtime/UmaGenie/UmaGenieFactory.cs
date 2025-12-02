using Cysharp.Threading.Tasks;
using Genies.Assets.Services;
using Genies.Refs;
using UnityEngine;

namespace Genies.Avatars
{
    public static class UmaGenieFactory
    {
        private const string UmaGeniePrefabResourcesPath = "UmaGenie";

        private static UmaGenie _defaultUmaGeniePrefab;

        public static async UniTask<UmaGenie> CreateAsync(string species, Transform parent = null, string lod = AssetLod.Default,
            AvatarsContext context = null, UmaGenie umaGeniePrefab = null)
        {
            // fallback to the default context if none provided
            context ??= DefaultAvatarsContext.Instance;

            // try to load the species asset
            Ref<SpeciesAsset> speciesAssetRef = await context.SpeciesLoader.LoadAsync(species, lod);
            if (!speciesAssetRef.IsAlive)
            {
                Debug.LogError($"[{nameof(UmaGenieFactory)}] couldn't load the species asset for the species {species}");
                return null;
            }

            UmaGenie umaGenie = await CreateAsync(speciesAssetRef, parent, lod, umaGeniePrefab);
            return umaGenie;
        }
        
        public static async UniTask<UmaGenie> CreateAsync(Ref<SpeciesAsset> speciesAssetRef, Transform parent = null,
            string lod = AssetLod.Default, UmaGenie umaGeniePrefab = null)
        {
            // try to instantiate a UmaGenie
            UmaGenie genie = Instantiate(parent, umaGeniePrefab);
            if (!genie)
            {
                return null;
            }

            // await for one frame so Unity has time to assign all the serialized data to the instance...
            await UniTask.Yield(); // this line fixed 2 hours of debugging :)

            // try to initialize the genie
            bool successful = await genie.TryInitializeAsync(lod, speciesAssetRef);
            return successful ? genie : null;
        }

        private static UmaGenie Instantiate(Transform parent, UmaGenie umaGeniePrefab)
        {
            umaGeniePrefab ??= _defaultUmaGeniePrefab;
            
            if (!umaGeniePrefab)
            {
                // try to load the default prefab from resources
                _defaultUmaGeniePrefab = Resources.Load<UmaGenie>(UmaGeniePrefabResourcesPath);
                if (!_defaultUmaGeniePrefab)
                {
                    Debug.LogError($"[{nameof(UmaGenieFactory)}] could not load the default UMA Genie prefab from resources. Path: {UmaGeniePrefabResourcesPath}");
                    return null;
                }
                
                umaGeniePrefab = _defaultUmaGeniePrefab;
            }

            // instantiate the avatar from the prefab and fetch the Uma Genie component
            UmaGenie umaGenie = Object.Instantiate(umaGeniePrefab, parent);
            umaGenie.gameObject.name = umaGeniePrefab.gameObject.name;
            
            return umaGenie;
        }
    }
}
