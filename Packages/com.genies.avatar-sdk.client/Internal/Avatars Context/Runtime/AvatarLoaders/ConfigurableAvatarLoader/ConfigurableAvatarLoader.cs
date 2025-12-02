using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Genies.Avatars;
using Genies.Refs;
using UnityEngine;

namespace Genies.Avatars.Context
{
    /// <summary>
    /// <see cref="IAvatarLoader"/> implementation that offers some generic configuration parameters.
    /// </summary>
    public sealed class ConfigurableAvatarLoader : IAvatarLoader
    {
        public string Lod;
        public IAvatarDefinitionSource DefinitionSource;
        public bool UseNonUmaGenie;
        public IEnumerable<IGenieComponentCreator> Components;
        public UmaGenie UmaGeniePrefabOverride;
        public EditableGenie EditableGeniePrefabOverride;
        public SpeciesAsset SpeciesAssetOverride;
        
        public async UniTask<IGenie> LoadAsync(Transform parent = null)
        {
            ISpeciesGenieController controller = await LoadControllerAsync(parent);
            return controller?.Genie;
        }

        public UniTask<Ref<IGeniePrefab>> LoadAsPrefabAsync()
        {
            Debug.LogError($"[{nameof(ConfigurableAvatarLoader)}] this loader does not support loading as prefab");
            return UniTask.FromResult<Ref<IGeniePrefab>>(default);
        }

        public async UniTask<ISpeciesGenieController> LoadControllerAsync(Transform parent = null)
        {
            if (DefinitionSource is null)
            {
                throw new Exception($"[{nameof(ConfigurableAvatarLoader)}] please provide an avatar definition source");
            }

            // get the definition from the configured source and try to get its species
            string definition = await DefinitionSource.GetDefinitionAsync();
            if (!AvatarsFactory.TryGetSpeciesFromDefinition(definition, out string species))
            {
                throw new Exception($"[{nameof(ConfigurableAvatarLoader)}] couldn't fetch the species from the returned avatar definition. Returned definition:\n{definition}");
            }

            // warn if we have a species asset override with a different species
            if (SpeciesAssetOverride is not null && SpeciesAssetOverride.Id != species)
            {
                Debug.LogWarning($"[{nameof(ConfigurableAvatarLoader)}] a species asset override was provided but it doesn't match the species from the returned definition. Species asset override: {SpeciesAssetOverride.Id} | Returned definition:\n{species}");
            }

            // instantiate the UmaGenie from the factory
            IEditableGenie umaGenie;
            if (SpeciesAssetOverride is null)
            {
                umaGenie = UseNonUmaGenie ?
                    await EditableGenieFactory.CreateAsync(species, parent : parent, umaGeniePrefab: EditableGeniePrefabOverride) :
                    await UmaGenieFactory.CreateAsync(species, parent, Lod, umaGeniePrefab: UmaGeniePrefabOverride);
            }
            else
            {
                Ref<SpeciesAsset> speciesAssetRef = CreateRef.FromAny(SpeciesAssetOverride);
                umaGenie = UseNonUmaGenie ?
                    await EditableGenieFactory.CreateAsync(speciesAssetRef, parent, umaGeniePrefab: EditableGeniePrefabOverride) :
                    await UmaGenieFactory.CreateAsync(speciesAssetRef, parent, Lod, umaGeniePrefab: UmaGeniePrefabOverride);
            }
            
            if (umaGenie is null || umaGenie.IsDisposed)
            {
                throw new Exception($"[{nameof(ConfigurableAvatarLoader)}] couldn't load {nameof(UmaGenie)}");
            }

            // create the genie controller from the UmaGenie
            ISpeciesGenieController controller = await AvatarsFactory.CreateGenieAsync(species, umaGenie, definition, Lod);
            if (controller is null)
            {
                umaGenie.Dispose();
                throw new Exception($"[{nameof(ConfigurableAvatarLoader)}] couldn't load genie controller");
            }
            
            // add components if any
            if (Components is null)
            {
                return controller;
            }

            foreach (IGenieComponentCreator creator in Components)
            {
                GenieComponent component = creator.CreateComponent();
                controller.Genie.Components.Add(component);
            }
            
            return controller;
        }
    }
}