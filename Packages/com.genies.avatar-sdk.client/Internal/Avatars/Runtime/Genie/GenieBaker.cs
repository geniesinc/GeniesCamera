using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Genies.Refs;
using Genies.Utilities;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Genies.Avatars
{
    /// <summary>
    /// Generic implementation that can bake or take snapshots of any <see cref="IGenie"/> instance.
    /// </summary>
    public sealed class GenieBaker
    {
        public readonly string Lod;
        public readonly MaterialBaker MegaSimpleBaker;
        public readonly MaterialBaker UrpBaker;

        public GenieBaker(string lod, MaterialBaker megaSimpleBaker, MaterialBaker urpBaker)
        {
            Lod = lod;
            MegaSimpleBaker = megaSimpleBaker ?? Resources.Load<MaterialBaker>("MegaSimpleBaker/MQ-MegaSimpleBaker");
            UrpBaker        = urpBaker        ?? Resources.Load<MaterialBaker>("URPBaker/MQ-URPBaker");
        }

        public UniTask<Genie> BakeAsync(IGenie genie, Transform parent = null, bool urpBake = false)
        {
            return BakeAsync(genie, urpBake ? UrpBaker : MegaSimpleBaker, Lod, parent);
        }

        public UniTask<IGenieSnapshot> TakeSnapshotAsync(IGenie genie, Transform parent = null, bool urpBake = false)
        {
            return TakeSnapshotAsync(genie, urpBake ? UrpBaker : MegaSimpleBaker, Lod, parent);
        }

        public IGenieSnapshot TakeSnapshot(IGenie genie, Transform parent = null, bool urpBake = false)
        {
            return TakeSnapshot(genie, urpBake ? UrpBaker : MegaSimpleBaker, Lod, parent);
        }

        /// <summary>
        /// Bakes any <see cref="IGenie"/> instance using the provided <see cref="MaterialBaker"/>.
        /// </summary>
        public static async UniTask<Genie> BakeAsync(IGenie genie, MaterialBaker materialBaker, string lod, Transform parent = null)
        {
            if (genie is null || genie.IsDisposed)
            {
                return null;
            }

            if (!materialBaker)
            {
                Debug.LogError($"[{nameof(GenieBaker)}] no material baker was given");
                return null;
            }

            await OperationQueue.EnqueueAsync(OperationCost.High);

            // clone the genie as a genie GO
            GameObject cloneGo = await CloneGenieAsync(genie);
            cloneGo.transform.SetParent(parent, worldPositionStays: false);

            // bake all resources
            materialBaker.ClearCache();
            Ref<List<SkinnedMeshRenderer>> bakedRenderersRef = await BakeRenderers(genie, cloneGo, materialBaker);
            Ref<UnityEngine.Avatar> avatarRef = BakeAvatar(genie.Animator);

            // setup the genie config
            string modelRootPath = genie.ModelRoot.transform.GetPathRelativeTo(genie.Root.transform);
            GameObject modelRoot = modelRootPath is null ? null : cloneGo.transform.Find(modelRootPath).gameObject;
            string skeletonRootPath = genie.SkeletonRoot.GetPathRelativeTo(genie.Root.transform);
            Transform skeletonRoot = skeletonRootPath is null ? null : cloneGo.transform.Find(skeletonRootPath);
            var animator = cloneGo.GetComponentInChildren<Animator>();
            animator.avatar = avatarRef.Item;

            // create the config for the genie
            var config = new Genie.Config
            {
                species                          = genie.Species,
                lod                              = lod,
                skeletonRoot                     = skeletonRoot,
                lods                             = new List<Genie.LodConfig> { new() { root = modelRoot } },
                autoGenerateSharedLodConfig      = true,
                autoGenerateSharedLodConfigIndex = 0,
            };

            // create a dispose callback that disposes all generated resources when the genie is disposed/destroyed
            Action onDisposeCallback = () =>
            {
                bakedRenderersRef.Dispose();
                avatarRef.Dispose();
            };

            // create the genie
            var bakedGenie = Genie.Create(cloneGo, config, onDisposeCallback);

            // copy all components from the source genie (except for animation features, which should be automatically added by the feature manager)
            foreach (GenieComponent component in genie.Components.All)
            {
                if (!component.IsAnimationFeature)
                {
                    bakedGenie.Components.Add(component.Copy());
                }
            }

            return bakedGenie;
        }

        /// <summary>
        /// Takes a snapshot of any <see cref="IGenie"/> instance using the provided <see cref="MaterialBaker"/>.
        /// </summary>
        public static IGenieSnapshot TakeSnapshot(IGenie genie, MaterialBaker materialBaker, string lod, Transform parent = null,
            bool recalculateNormals = true, bool recalculateTangents = true)
        {
            return TakeSnapshotAsync(genie, materialBaker, lod, parent, recalculateNormals, recalculateTangents, async: false).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Takes a snapshot of any <see cref="IGenie"/> instance using the provided <see cref="MaterialBaker"/>.
        /// </summary>
        public static UniTask<IGenieSnapshot> TakeSnapshotAsync(IGenie genie, MaterialBaker materialBaker, string lod,
            Transform parent = null, bool recalculateNormals = true, bool recalculateTangents = true)
        {
            return TakeSnapshotAsync(genie, materialBaker, lod, parent, recalculateNormals, recalculateTangents, async: true);
        }

        private static async UniTask<IGenieSnapshot> TakeSnapshotAsync(IGenie genie, MaterialBaker materialBaker, string lod,
            Transform parent, bool recalculateNormals, bool recalculateTangents, bool async)
        {
            if (genie is null || genie.IsDisposed)
            {
                return null;
            }

            if (!materialBaker)
            {
                Debug.LogError($"[{nameof(GenieBaker)}] no material baker was given");
                return null;
            }

            GameObject root;
            Action disposeCallback;

            // avoid creating child GameObjects if we only have to take the snapshot for one renderer
            if (genie.Renderers.Count == 1)
            {
                Ref<GameObject> snapshotRef = await TakeRendererSnapshotAsync(genie.Renderers[0], materialBaker, recalculateNormals, recalculateTangents, async);
                root = snapshotRef.Item;
                disposeCallback = () => snapshotRef.Dispose();
            }
            else
            {
                root = new GameObject();
                disposeCallback = await TakeRenderersSnapshotAsync(genie.Renderers, root, materialBaker, recalculateNormals, recalculateTangents, async);
            }

            // setup the GenieSnapshot component and return it
            root.name = $"{genie.Root.name} (Snapshot)";
            root.transform.SetParent(parent, worldPositionStays: false);
            var genieSnapshot = root.AddComponent<GenieSnapshot>();
            genieSnapshot.Initialize(genie.Species, root, disposeCallback);

            return genieSnapshot;
        }

        private static async UniTask<Ref<List<SkinnedMeshRenderer>>> BakeRenderers(IGenie genie, GameObject cloneGo, MaterialBaker materialBaker)
        {
            var cloneRenderers = new List<SkinnedMeshRenderer>(genie.Renderers.Count);
            var refs = new List<Ref>();

            await UniTask.WhenAll(genie.Renderers.Select(BakeRendererAsync));

            return CreateRef.FromDependentResource(cloneRenderers, refs);

            async UniTask BakeRendererAsync(SkinnedMeshRenderer renderer)
            {
                // find the renderer path in the clone GO
                string rendererPath = renderer.transform.GetPathRelativeTo(genie.Root.transform);
                if (rendererPath is null)
                {
                    return;
                }

                // find the renderer
                Transform cloneRendererTransform = cloneGo.transform.Find(rendererPath);
                var cloneRenderer = cloneRendererTransform.GetComponent<SkinnedMeshRenderer>();
                if (!cloneRenderer)
                {
                    return;
                }

                // bake mesh and materials
                Ref<Mesh> meshRef = BakeMesh(cloneRenderer, isSnapshot: false);
                Ref<Material[]> materialsRef = await materialBaker.SingleRefBakeAsync(cloneRenderer.sharedMaterials);
                cloneRenderer.sharedMesh = meshRef.Item;
                cloneRenderer.sharedMaterials = materialsRef.Item;

                // register clone renderer and refs
                cloneRenderers.Add(cloneRenderer);
                refs.Add(meshRef);
                refs.Add(materialsRef);
            }
        }

        private static async UniTask<Action> TakeRenderersSnapshotAsync(IEnumerable<SkinnedMeshRenderer> renderers, GameObject root,
            MaterialBaker materialBaker, bool recalculateNormals, bool recalculateTangents, bool async)
        {
            var refs = new List<Ref>();

            await UniTask.WhenAll(renderers.Select(TakeSnapshotAsync));

            // return an action that disposes all generated resources
            return () =>
            {
                foreach (Ref reference in refs)
                {
                    reference.Dispose();
                }
            };

            async UniTask TakeSnapshotAsync(SkinnedMeshRenderer renderer)
            {
                Ref<GameObject> snapshotRef = await TakeRendererSnapshotAsync(renderer, materialBaker, recalculateNormals, recalculateTangents, async);
                snapshotRef.Item.transform.SetParent(root.transform, worldPositionStays: false);
                snapshotRef.Item.transform.ResetLocalTransform();
                refs.Add(snapshotRef);
            }
        }

        private static async UniTask<Ref<GameObject>> TakeRendererSnapshotAsync(SkinnedMeshRenderer renderer,
            MaterialBaker materialBaker, bool recalculateNormals, bool recalculateTangents, bool async)
        {
            // manually create a game object and add mesh filter and renderer components
            var gameObject = new GameObject(renderer.name);
            var meshFilter = gameObject.AddComponent<MeshFilter>();
            var snapshotRenderer = gameObject.AddComponent<MeshRenderer>();

            // bake all resources
            materialBaker.ClearCache();
            Ref<Mesh> meshRef = BakeMesh(renderer, isSnapshot: true, recalculateNormals, recalculateTangents);
            Ref<Material[]> materialsRef;

            if (async)
            {
                materialsRef = await materialBaker.SingleRefBakeAsync(renderer.sharedMaterials);
            }
            else
            {
                materialsRef = materialBaker.SingleRefBake(renderer.sharedMaterials);
            }

            // setup components
            meshFilter.sharedMesh = meshRef.Item;
            snapshotRenderer.sharedMaterials = materialsRef.Item;

            return CreateRef.FromDependentResource(gameObject, meshRef, materialsRef);
        }

        private static Ref<Mesh> BakeMesh(SkinnedMeshRenderer renderer, bool isSnapshot,
            bool recalculateNormals = true, bool recalculateTangents = true)
        {
            Mesh bakedMesh;

            if (isSnapshot)
            {
                bakedMesh = new Mesh { name = $"{renderer.sharedMesh.name}_bake" };
                renderer.BakeMesh(bakedMesh, useScale: true);

                if (recalculateNormals)
                {
                    bakedMesh.RecalculateNormals();
                }

                if (recalculateTangents)
                {
                    bakedMesh.RecalculateTangents();
                }
            }
            else
            {
                bakedMesh = Object.Instantiate(renderer.sharedMesh);
            }

            Ref<Mesh> bakedMeshRef = CreateRef.FromUnityObject(bakedMesh);
            return bakedMeshRef;
        }

        private static Ref<UnityEngine.Avatar> BakeAvatar(Animator animator)
        {
            if (!animator || !animator.avatar)
            {
                return default;
            }

            UnityEngine.Avatar avatar = Object.Instantiate(animator.avatar);
            Ref<UnityEngine.Avatar> avatarRef = CreateRef.FromUnityObject(avatar);

            return avatarRef;
        }

        // makes a full clone of the source genie. This is the easiest way to get all animations working without much effort
        private static async UniTask<GameObject> CloneGenieAsync(IGenie genie)
        {
            var tmpRootGo = new GameObject("Tmp Genie Baker Root");

            /**
             * Disable the temporal parent GameObject and use it to clone the original GameObject directly parented to it
             * so it is also disabled at the moment of creation. If we don't do this, the UMA components will
             * automatically destroy the skinned mesh renderer.
             */
            tmpRootGo.SetActive(false);

            /**
             * Clone the genie GameObject and destroy any UMA specific components. Ideally we would
             * just create a new GameObject and add the renderer, animator and other components manaully
             * but cloning is currently the best way to keep animations and all skinned mesh renderer data
             * like the bone transforms. Once we migrate all bonus components to the GenieComponent approach
             * we can just create a new GameObject.
             */
            GameObject cloneGo = await GenieUtilities.CloneRootAsync(genie, tmpRootGo.transform);
            cloneGo.name = $"{genie.Root.name} (Bake)";

            // reset to default layer and transformation
            ResetLayer(cloneGo);
            cloneGo.transform.localPosition = Vector3.zero;
            cloneGo.transform.localRotation = Quaternion.identity;
            cloneGo.transform.localScale = Vector3.one;

            // remove UMA and Genie components if any
            GenieUtilities.RemoveUmaComponents(cloneGo);
            var genieComponent = cloneGo.GetComponent<Genie>();
            if (genieComponent)
            {
                Object.DestroyImmediate(genieComponent);
            }

            // move the clone object out of the tmp root
            cloneGo.transform.SetParent(null, worldPositionStays: false);
            Object.Destroy(tmpRootGo);

            return cloneGo;
        }

        private static void ResetLayer(GameObject gameObject)
        {
            gameObject.layer = 0;

            for (int i = 0; i < gameObject.transform.childCount; ++i)
            {
                ResetLayer(gameObject.transform.GetChild(i).gameObject);
            }
        }
    }
}
