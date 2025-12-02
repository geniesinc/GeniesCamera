using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Genies.Avatars;
using Genies.Refs;
using UnityEngine;

namespace Genies.Avatars.Context
{
    public sealed class LodManifestAvatarLoader : IAvatarLoader
    {
        /// <summary>
        /// URL to a json lod manifest.
        /// </summary>
        public string LodManifestUrl;
        
        /// <summary>
        /// Import settings to be used.
        /// </summary>
        public GenieGltfImporter.Settings Settings;
        
        /// <summary>
        /// How to filter what LODs from the manifest will be included on the load.
        /// </summary>
        public Filter LodsFilter = Filter.All;
        
        /// <summary>
        /// The LOD indices to be loaded when <see cref="LodsFilter"/> is set to <see cref="Filter.Indices"/>.
        /// </summary>
        public List<int> LodIndices;
        
        /// <summary>
        /// The LOD names to be loaded when <see cref="LodsFilter"/> is set to <see cref="Filter.Names"/>.
        /// </summary>
        public List<string> LodNames;
        
        public UniTask<IGenie> LoadAsync(Transform parent = null)
        {
            return LodsFilter switch
            {
                Filter.All     => AvatarsFactory.LoadFromLodManifestAsync(LodManifestUrl, parent, Settings),
                Filter.Indices => AvatarsFactory.LoadFromLodManifestAsync(LodManifestUrl, LodIndices, parent, Settings),
                Filter.Names   => AvatarsFactory.LoadFromLodManifestAsync(LodManifestUrl, LodNames, parent, Settings),
                _ => throw new ArgumentOutOfRangeException(),
            };
        }

        public UniTask<Ref<IGeniePrefab>> LoadAsPrefabAsync()
        {
            return LodsFilter switch
            {
                Filter.All     => AvatarsFactory.LoadFromLodManifestAsPrefabAsync(LodManifestUrl, Settings),
                Filter.Indices => AvatarsFactory.LoadFromLodManifestAsPrefabAsync(LodManifestUrl, LodIndices, Settings),
                Filter.Names   => AvatarsFactory.LoadFromLodManifestAsPrefabAsync(LodManifestUrl, LodNames, Settings),
                _ => throw new ArgumentOutOfRangeException(),
            };
        }

        public UniTask<ISpeciesGenieController> LoadControllerAsync(Transform parent = null)
        {
            Debug.LogError($"[{nameof(LodManifestAvatarLoader)}] glTF avatars are readonly so a controller cannot be instantiated from it");
            return UniTask.FromResult<ISpeciesGenieController>(null);
        }
        
        public enum Filter
        {
            All     = 0,
            Indices = 3,
            Names   = 4,
        }
    }
}