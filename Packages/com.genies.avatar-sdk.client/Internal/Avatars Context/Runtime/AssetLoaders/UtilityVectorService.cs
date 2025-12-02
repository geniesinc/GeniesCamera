using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Genies.Assets.Services;
using Genies.Avatars;
using Genies.Models;
using Genies.Refs;
using UnityEngine.ResourceManagement.ResourceLocations;
using UtilMesh = Genies.Models.UtilMesh;
using UtilMeshName = Genies.Models.UtilMeshName;
using UtilMeshRegion = Genies.Models.UtilMeshRegion;

namespace Genies.Avatars.Context
{
    public sealed class UtilityVectorService : IUtilityVectorService
    {
        // dependencies
        private readonly IAssetsService _assetsService;

        // state
        private readonly Dictionary<string, IResourceLocation> _locationsByVectorId;
        private IList<IResourceLocation> _locations;

        public UtilityVectorService(IAssetsService assetsService)
        {
            _assetsService = assetsService;

            _locationsByVectorId = new Dictionary<string, IResourceLocation>();

            LoadAllResourceLocations().Forget();
        }

        public async UniTask<UtilityVector> LoadAsync(string vectorId)
        {
            if (string.IsNullOrEmpty(vectorId))
            {
                return default;
            }

            await UniTask.WaitWhile(() => _locations is null);
            if (!TryGetResourceLocation(vectorId, out IResourceLocation location))
            {
                return null;
            }

            using Ref<UtilityVectorContainer> containerRef = await _assetsService.LoadAssetAsync<UtilityVectorContainer>(location);
            if (!containerRef.IsAlive)
            {
                return null;
            }

            return GetUtilityVectorFromContainer(containerRef.Item);
        }

        private UtilityVector GetUtilityVectorFromContainer(UtilityVectorContainer container)
        {
            var utilMeshes = container.utilMeshes.Select(utilMesh =>
            {
                var regions = utilMesh.uMeshRegions
                    .Select(region => new Genies.Avatars.UtilMeshRegion((RegionType)(int)region.region, region.uniquePoints))
                    .ToList().AsReadOnly();

                return new Genies.Avatars.UtilMesh((Genies.Avatars.UtilMeshName)(int)utilMesh.utilityMesh, regions);
            }).ToList().AsReadOnly();

            return new UtilityVector(container.vectorName, container.version, utilMeshes);
        }

        private async UniTaskVoid LoadAllResourceLocations()
        {
            _locations = await _assetsService.LoadResourceLocationsAsync<UtilityVectorContainer>(new [] { "utilityvector", "gen6" }, MergingMode.Intersection);
        }

        private bool TryGetResourceLocation(string vectorId, out IResourceLocation result)
        {
            if (_locationsByVectorId.TryGetValue(vectorId, out result))
            {
                return true;
            }

            string primaryKey = $"gen6Unified_{vectorId}_UtilityVectorContainer";
            foreach (IResourceLocation location in _locations)
            {
                if (location.PrimaryKey != primaryKey)
                {
                    continue;
                }

                result = location;
                _locationsByVectorId[vectorId] = location;
                return true;
            }

            return false;
        }

        public Genies.Avatars.UtilMeshName GetUtilityMeshFromAssetCategory(OutfitAsset asset)
        {
            return UtilityMeshConverter.GetUtilityMeshFromAssetCategory(asset);
        }
    }
    }

