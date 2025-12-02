using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Genies.Assets.Services;
using Genies.Models;
using Genies.Refs;
using Genies.Utilities;

namespace Genies.Avatars.Context
{
    public sealed class SpeciesLoader : CachedAssetRefLoader<(string id, string lod), SpeciesAsset>, IAssetLoader<SpeciesAsset>
    {
        private static readonly Dictionary<string, string> EmptyMappedUmaIdentifiers = new();

        // dependencies
        private readonly IAssetsService _assetsService;

        public SpeciesLoader(IAssetsService assetsService)
        {
            _assetsService = assetsService;
        }

        public UniTask<Ref<SpeciesAsset>> LoadAsync(string assetId, string lod = AssetLod.Default)
        {
            return CachedLoadAssetAsync((assetId, lod));
        }

        protected override bool ValidateKey(ref (string id, string lod) key)
        {
            return !string.IsNullOrEmpty(key.id) && !string.IsNullOrEmpty(key.lod);
        }

        protected override async UniTask<Ref<SpeciesAsset>> LoadAssetAsync((string id, string lod) key)
        {
            // try to load the body container from the assets service
            Ref<BodyTypeContainer> containerRef = await LoadBodyTypeContainerAsync(key.id, key.lod);
            if (!containerRef.IsAlive)
            {
                return default;
            }

            // create a species asset from the body container that will own the ref
            SpeciesAsset asset = GetAssetFromContainer(key.id, key.lod, containerRef);
            return CreateRef.FromDisposable(asset);
        }

        private async UniTask<Ref<BodyTypeContainer>> LoadBodyTypeContainerAsync(string species, string lod = AssetLod.Default)
        {
            // TODO we should establish a proper convention for this. I don't think it is necessary to use enforce the race and gen6 labels

            // right now the dolls body type container is not following the rules that we had stablished with the "race" and "gen6" keys
            if (species == GenieSpecies.Dolls)
            {
                return await _assetsService.LoadAssetAsync<BodyTypeContainer>("DollGen1_RaceData_Container", lod:lod);
            }

            // load all containers matching the required keys
            var keys = new [] { species, "race", "gen6", lod };
            IList<Ref<BodyTypeContainer>> containers = await _assetsService.LoadUnpackedAssetsAsync<BodyTypeContainer>(keys, MergingMode.Intersection);

            //Get LOD container
            // find the first valid container and dispose the rest with lod label (we should always be receiving a single container but just in case)
            Ref<BodyTypeContainer> result = default;
            foreach (Ref<BodyTypeContainer> containerRef in containers)
            {
                if (result.IsAlive)
                {
                    containerRef.Dispose();
                    continue;
                }

                if (containerRef.IsAlive)
                {
                    result = containerRef;
                }
            }

            if (result.IsAlive)
            {
                return result;
            }

            // find the first valid container and dispose the rest (we should always be receiving a single container but just in case)
            keys = new [] { species, "race", "gen6" };
            containers = await _assetsService.LoadUnpackedAssetsAsync<BodyTypeContainer>(keys, MergingMode.Intersection);
            foreach (Ref<BodyTypeContainer> containerRef in containers)
            {
                if (result.IsAlive)
                {
                    containerRef.Dispose();
                    continue;
                }

                if (containerRef.IsAlive)
                {
                    result = containerRef;
                }
            }

            if (result.IsAlive)
            {
                return result;
            }

            // fallback to just trying to load the body type container with the given species string as key
            result = await _assetsService.LoadAssetAsync<BodyTypeContainer>(species, lod:lod);
            return result;
        }

        public static SpeciesAsset GetAssetFromContainer(string species, string lod, Ref<BodyTypeContainer> containerRef, string genieType = GenieTypeName.NonUma)
        {
            if (!containerRef.IsAlive)
            {
                return null;
            }

            BodyTypeContainer container = containerRef.Item;
            IDictionary<string, string> umaMaterialIdentifiersBySlotId = GetMappedUmaIdentifiers(species);
            var asset = new SpeciesAsset(
                species,
                genieType,
                lod,
                container.Race,
                container.DnaAsset,
                container.SlotDataAssets,
                container.Overlays,
                container.Extras.GetAssets<IGenieComponentCreator>(),
                umaMaterialIdentifiersBySlotId,
                containerRef
            );

            return asset;
        }

        private static IDictionary<string, string> GetMappedUmaIdentifiers(string species)
        {
            return species switch
            {
                GenieSpecies.Unified => UnifiedMaterialSlot.MappedUmaIdentifiers,
                GenieSpecies.Dolls => EmptyMappedUmaIdentifiers,
                _ => EmptyMappedUmaIdentifiers
            };
        }
    }
}
