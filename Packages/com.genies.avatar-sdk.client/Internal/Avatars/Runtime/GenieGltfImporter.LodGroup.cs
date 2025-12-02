using System;
using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using Genies.Refs;
using UnityEngine;

namespace Genies.Avatars
{
    public static partial class GenieGltfImporter
    {
        /// <summary>
        /// Imports the gltf/glb files from the given <see cref="LodGroupSource"/> as a LOD group genie instance.
        /// </summary>
        public static async UniTask<IGenie> ImportAsync(LodGroupSource source, Transform parent = null, Settings settings = null)
        {
            // load as genie prefab
            Ref<IGeniePrefab> prefabRef = await ImportAsPrefabAsync(source, settings);
            if (!prefabRef.IsAlive)
            {
                return null;
            }

            // instantiate a genie from the prefab
            IGenie genie = prefabRef.Item.Instantiate(parent);
            if (genie is null || genie.IsDisposed)
            {
                prefabRef.Dispose();
                return null;
            }
            
            // link the prefab ref to the genie so it is disposed when the genie instance is disposed/destroyed
            genie.Disposed += () => prefabRef.Dispose();
            
            return genie;
        }

        /// <summary>
        /// Imports the gltf/glb files from the given <see cref="LodGroupSource"/> as a LOD group genie prefab
        /// reference. You can create genie instances from the prefab and dispose the prefab reference to release all
        /// loaded resources. Any created genie instances left will not work when the prefab ref is disposed since the
        /// resources will have been destroyed.
        /// </summary>
        public static async UniTask<Ref<IGeniePrefab>> ImportAsPrefabAsync(LodGroupSource source, Settings settings = null)
        {
            var prefab = new GenieGltfLodGroupPrefab(source, settings);
            await prefab.LoadAsync();
            Ref<IGeniePrefab> prefabRef = CreateRef.FromAny<IGeniePrefab>(prefab, _ => prefab.Dispose());
            
            return prefabRef;
        }

        [Serializable]
        public struct LodGroupSource
        {
            public LODFadeMode     fadeMode;
            public bool            animateCrossFading;
            public List<LodSource> lods;
        }

        [Serializable]
        public struct LodSource
        {
            public string url;
            public float  screenRelativeTransitionHeight;
            public float  fadeTransitionWidth;
        }
    }
}
