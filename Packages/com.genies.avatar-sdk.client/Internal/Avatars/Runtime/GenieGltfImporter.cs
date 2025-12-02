using System;
using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using Genies.Refs;
using Genies.Utilities;
using GLTF.Schema;
using Newtonsoft.Json.Linq;
using UnityEngine;
using GUnityGLTF;
using Object = UnityEngine.Object;

namespace Genies.Avatars
{
    public static partial class GenieGltfImporter
    {
        private const string DefaultAnimatorControllerPath = "Animations/Idle/noFaceHumanoid_idle_animator";

        private static readonly HandleCache<string, GenieGltfLodPrefab> PrefabCache = new();
        private static readonly Dictionary<string, UniTaskCompletionSource> LoadingOperations = new();

        /// <summary>
        /// Whether the given URL represents a genie that was imported and is still loaded in memory.
        /// </summary>
        public static bool IsLoaded(string url)
        {
            return !string.IsNullOrEmpty(url) && PrefabCache.IsHandleCached(url);
        }

        /// <summary>
        /// If the given URL is from an imported genie that is still loaded, this will force it to be unloaded even if
        /// it is still being used. Its highly discouraged to use this unless you know what you are doing.
        /// </summary>
        public static bool ForceUnload(string url)
        {
            if (string.IsNullOrEmpty(url) || !PrefabCache.TryGetHandle(url, out Handle<GenieGltfLodPrefab> handle))
            {
                return false;
            }

            handle.Dispose();
            PrefabCache.Release(url);
            return true;
        }

        /// <summary>
        /// Loads the gltf/glb file from the given URL directly as a genie instance. Disposing/destroying the genie will
        /// release all the resources. Internal caching is performed so multiple calls with the same URL will load the
        /// resources one. This is the easiest and safest way to import avatars.
        /// </summary>
        public static async UniTask<IGenie> ImportAsync(string url, Transform parent = null, Settings settings = null)
        {
            // load as genie prefab
            Ref<IGeniePrefab> prefabRef = await ImportAsPrefabAsync(url, settings);
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
        /// Imports the gltf/glb file from the given URL as a genie prefab reference. You can create genie instances
        /// from the prefab and dispose the prefab reference to release all loaded resources. Any created genie
        /// instances left will not work when the prefab ref is disposed since the resources will have been destroyed.
        /// </summary>
        public static async UniTask<Ref<IGeniePrefab>> ImportAsPrefabAsync(string url, Settings settings = null)
        {
            Ref<GenieGltfLodPrefab> prefabRef = await ImportLodPrefabAsync(url, settings);
            if (!prefabRef.IsAlive)
            {
                return default;
            }

            return CreateRef.FromDependentResource<IGeniePrefab>(prefabRef.Item, prefabRef);
        }

        internal static async UniTask<Ref<GenieGltfLodPrefab>> ImportLodPrefabAsync(string url, Settings settings)
        {
            if (string.IsNullOrEmpty(url))
            {
                Debug.LogError($"[{nameof(GenieGltfImporter)}] cannot import avatar: the given url is null or empty");
                return default;
            }

            // if this URL is currently being loaded then await for that operation first
            UniTaskCompletionSource operation;
            while (LoadingOperations.TryGetValue(url, out operation))
            {
                await operation.Task;
            }

            // if this url was loaded before and it is still alive then return a new reference to the prefab
            if (PrefabCache.TryGetNewReference(url, out Ref<GenieGltfLodPrefab> prefabRef))
            {
                return prefabRef;
            }

            // start a new loading operation
            operation = new UniTaskCompletionSource();
            LoadingOperations[url] = operation;

            settings ??= new Settings();

            /**
             * Be really careful if you want to reuse the same ImportOptions object for every import, it can hit some
             * edge cases where loading from URLs with UnityWebRequest will mess up with the URL creation. It's better
             * to always create a new one for each import.
             */
            // create new gltf importer and load the url
            var importer = new GLTFSceneImporter(url, new ImportOptions())
            {
                IsMultithreaded            = settings.multithreadedImport,
                KeepCPUCopyOfMesh          = settings.keepCPUCopyOfMeshes,
                KeepCPUCopyOfTexture       = settings.keepCPUCopyOfTextures,
                GenerateMipMapsForTextures = settings.generateMipMapsForTextures,
            };

            try
            {
                await importer.LoadSceneAsync();

                // build the genie prefab reference, cache its handle and return it
                prefabRef = BuildGeniePrefab(importer);
                PrefabCache.CacheHandle(url, prefabRef);

                return prefabRef;
            }
            catch (Exception exception)
            {
                Debug.LogError($"[{nameof(GenieGltfImporter)}] something went wrong while importing glTF genie from {url}:\n{exception}");

                // its important to destroy this GameObject otherwise any loaded resources will not be released
                if (importer.LastLoadedScene)
                {
                    Object.Destroy(importer.LastLoadedScene);
                }

                return default;
            }
            finally
            {
                importer.Dispose();
                LoadingOperations.Remove(url);
                operation.TrySetResult();
            }
        }

        private static Ref<GenieGltfLodPrefab> BuildGeniePrefab(GLTFSceneImporter importer)
        {
            // validate the GameObject loaded by the GLTF importer
            GameObject importedGo = importer.LastLoadedScene;

            if (!importedGo)
            {
                throw new Exception("No loaded GameObject was found in the GLTF scene importer");
            }

            if (importedGo.transform.childCount == 0)
            {
                throw new Exception("A loaded GameObject was found in the GLTF scene importer but it has no children");
            }

            if (importedGo.transform.childCount > 1)
            {
                Debug.LogWarning("The GameObject loaded by the GLTF scene importer has more than one child. Exported genies should only have one child. Using the first one...");
            }

            // get the genie glTF extras
            GenieGltfExtras extras = GetGenieExtras(importer);

            // get the Genie GameObject and separate it from the imported GO containing the InstantiatedGLTFObject component
            GameObject root = importedGo.transform.GetChild(0).gameObject;
            root.transform.SetParent(null, worldPositionStays: false);

            // deactivate the imported GO and hide it in the hierarchy
            importedGo.SetActive(false);
            importedGo.hideFlags |= HideFlags.HideInHierarchy;
            Object.DontDestroyOnLoad(importedGo);

            // create Genie config
            Genie.Config config = CreateGenieConfig(root, extras);

            // build the human Avatar for the animator
            Animator animator = root.AddComponent<Animator>();
            Avatar avatar = null;
            if (extras.isHuman && extras.humanDescription is not null)
            {
                avatar = GenieGltfExtrasUtils.BuildHumanAvatar(root, extras.humanDescription);
                animator.avatar = avatar;
            }

            // TODO in a future were we start having more species variety other than humanoid we can safely remove this part or have a default controller map for each species
            // load the default animator controller
            Ref<RuntimeAnimatorController> animatorControllerRef = ResourcesUtility.LoadAsset<RuntimeAnimatorController>(DefaultAnimatorControllerPath);
            if (animatorControllerRef.IsAlive)
            {
                animator.runtimeAnimatorController = animatorControllerRef.Item;
            }

            // create a new gltf genie prefab component
            var geniePrefab = GenieGltfLodPrefab.Create(root, config);

            // create a dispose callback that releases all loaded resources
            Action<GenieGltfLodPrefab> disposeCallback = _ =>
            {
                Object.Destroy(root);
                Object.Destroy(importedGo); // this will destroy the InstantiatedGLTFObject component which will destroy all loaded assets
                animatorControllerRef.Dispose();

                if (avatar)
                {
                    Object.Destroy(avatar);
                }
            };

            // create and return the final ref with the dispose callback
            Ref<GenieGltfLodPrefab> prefabRef = CreateRef.FromAny(geniePrefab, disposeCallback);
            return prefabRef;
        }

        private static GenieGltfExtras GetGenieExtras(GLTFSceneImporter importer)
        {
            GLTFScene defaultScene = importer.Root?.GetDefaultScene();
            if (defaultScene is null)
            {
                throw new Exception($"Couldn't find the genie extras. The imported glTF has no default scene");
            }

            if (defaultScene.Nodes is null || defaultScene.Nodes.Count == 0)
            {
                throw new Exception($"Couldn't find the genie extras. The imported glTF has a default scene with no nodes");
            }

            JToken gltfExtras = defaultScene.Nodes[0].Value.Extras;
            JToken genieExtras = GenieGltfExtrasUtils.GetGenieExtrasFromGltfExtras(gltfExtras);

            try
            {
                return GenieGltfExtras.Deserialize(genieExtras);
            }
            catch (Exception exception)
            {
                throw new Exception($"Couldn't deserialize the genie extras. Check Genie Export options. Exception:\n{exception}");
            }
        }

        private static Genie.Config CreateGenieConfig(GameObject root, GenieGltfExtras extras)
        {
            // find model and skeleton roots
            GameObject modelRoot = root.transform.Find(extras.modelRootPath).gameObject;
            Transform skeletonRoot = root.transform.Find(extras.skeletonRootPath);

            // set the skeleton to the T-pose from the human description (this is important for the Skeleton Modifier component to work)
            SetTPoseFromHumanDescription(skeletonRoot, extras.humanDescription);

            // create the single lod config for the model root
            var lods = new List<Genie.LodConfig>(1);
            var lod = new Genie.LodConfig { root = modelRoot };
            lods.Add(lod);

            // create shared lod config from the only LOD that was loaded
            Genie.SharedLodConfig sharedLodConfig = Genie.CreateSharedLodConfig(modelRoot);

            // generate component creators from the serialized components
            List<IGenieComponentCreator> componentCreators = null;
            if (extras.components is not null)
            {
                componentCreators = new List<IGenieComponentCreator>(extras.components.Count);
                foreach (JToken token in extras.components)
                {
                    componentCreators.Add(new SerializedGenieComponentCreator(token));
                }
            }

            return new Genie.Config
            {
                species                            = extras.species,
                lod                                = extras.lod,
                skeletonRoot                       = skeletonRoot,
                automaticallyRecalculateObjectSize = true,
                lods                               = lods,
                autoGenerateSharedLodConfig        = false,
                sharedLodConfig                    = sharedLodConfig,
                ComponentCreators                  = componentCreators,
            };
        }

        private static void SetTPoseFromHumanDescription(Transform skeletonRoot, SerializableHumanDescription humanDescription)
        {
            if (humanDescription?.skeleton is null)
            {
                return;
            }

            Dictionary<string, Transform> bonesByName = skeletonRoot.GetChildrenByName(includeSelf: false); // root bone is not included, if this causes any issues we should include it
            foreach (SerializableSkeletonBone skeletonBone in humanDescription.skeleton)
            {
                if (!bonesByName.TryGetValue(skeletonBone.name, out Transform bone))
                {
                    continue;
                }

                bone.localPosition = skeletonBone.position;
                bone.localRotation = skeletonBone.rotation;
                bone.localScale = skeletonBone.scale;
            }
        }
    }
}
