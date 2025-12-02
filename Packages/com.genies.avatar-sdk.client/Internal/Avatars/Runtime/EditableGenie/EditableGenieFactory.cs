using Cysharp.Threading.Tasks;
using Genies.Assets.Services;
using Genies.Refs;
using UnityEngine;

namespace Genies.Avatars
{
    public static class EditableGenieFactory
    {
        private const string EditableGeniePrefabResourcesPath = "EditableGenie";

        private static EditableGenie _defaultEditableGeniePrefab;

        public static async UniTask<EditableGenie> CreateAsync(string species, string subSpecies = null, Transform parent = null,
            string lod = AssetLod.Default, AvatarsContext context = null, EditableGenie umaGeniePrefab = null)
        {
            // fallback to the default context if none provided
            context ??= DefaultAvatarsContext.Instance;
            
            if (species == GenieSpecies.UnifiedGAP)
            {
                // if species is UnifiedGAP, we load 
                return await CreateFromSubSpeciesAsync(subSpecies, context, parent, lod, umaGeniePrefab);
            }

            // try to load the species asset
            Ref<SpeciesAsset> speciesAssetRef = await context.SpeciesLoader.LoadAsync(species, lod);
            if (!speciesAssetRef.IsAlive)
            {
                Debug.LogError($"[{nameof(EditableGenieFactory)}] couldn't load the species asset for the species {species}");
                return null;
            }

            EditableGenie umaGenie = await CreateAsync(speciesAssetRef, parent, umaGeniePrefab);
            return umaGenie;
        }
        
        private static async UniTask<EditableGenie> CreateFromSubSpeciesAsync(string subSpecies, AvatarsContext context, 
            Transform parent = null, string lod = AssetLod.Default, EditableGenie umaGeniePrefab = null)
        {
            // try to load the species asset
            Ref<SubSpeciesAsset> speciesAssetRef = await context.SubSpeciesLoader.LoadAsync(subSpecies, lod);
            if (!speciesAssetRef.IsAlive)
            {
                Debug.LogError($"[{nameof(EditableGenieFactory)}] couldn't load the SubSpeciesAsset for the subSpecies {subSpecies}");
                return null;
            }

            EditableGenie umaGenie = await CreateAsync(speciesAssetRef, parent, umaGeniePrefab);
            return umaGenie;
        }


        public static async UniTask<EditableGenie> CreateAsync(Ref<SpeciesAsset> speciesAssetRef, Transform parent = null,
            EditableGenie umaGeniePrefab = null)
        {
            // try to instantiate a EditableGenie
            EditableGenie genie = Instantiate(parent, umaGeniePrefab);
            if (!genie)
            {
                return null;
            }

            // await for one frame so Unity has time to assign all the serialized data to the instance...
            await UniTask.Yield(); // this line fixed 2 hours of debugging :)

            // try to initialize the genie
            bool successful = await genie.TryInitializeAsync(speciesAssetRef);
            return successful ? genie : null;
        }
        
        public static async UniTask<EditableGenie> CreateAsync(Ref<SubSpeciesAsset> subSpeciesAssetRef, Transform parent = null,
            EditableGenie umaGeniePrefab = null)
        {
            // try to instantiate a NonUmaGenie
            EditableGenie genie = Instantiate(parent, umaGeniePrefab);
            if (!genie)
            {
                return null;
            }

            // await for one frame so Unity has time to assign all the serialized data to the instance...
            await UniTask.Yield(); // this line fixed 2 hours of debugging :)

            // try to initialize the genie
            bool successful = await genie.TryInitializeWithGAPAsync(subSpeciesAssetRef);
            return successful ? genie : null;
        }

        private static EditableGenie Instantiate(Transform parent, EditableGenie umaGeniePrefab)
        {
            umaGeniePrefab ??= _defaultEditableGeniePrefab;

            if (!umaGeniePrefab)
            {
                // try to load the default prefab from resources
                _defaultEditableGeniePrefab = Resources.Load<EditableGenie>(EditableGeniePrefabResourcesPath);
                if (!_defaultEditableGeniePrefab)
                {
                    Debug.LogError($"[{nameof(EditableGenieFactory)}] could not load the default UMA Genie prefab from resources. Path: {EditableGeniePrefabResourcesPath}");
                    return null;
                }

                umaGeniePrefab = _defaultEditableGeniePrefab;
            }

            // instantiate the avatar from the prefab and fetch the Uma Genie component
            EditableGenie umaGenie = Object.Instantiate(umaGeniePrefab, parent);
            umaGenie.gameObject.name = umaGeniePrefab.gameObject.name;

            return umaGenie;
        }
    }
}
