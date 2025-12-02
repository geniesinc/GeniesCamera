using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Genies.Utilities;
using UnityEngine;

namespace Genies.Avatars
{
    /// <summary>
    /// Generic and extensible component implementation of <see cref="IGenie"/> for creating clones that
    /// are linked to the original genie instance.
    /// </summary>
    public class CloneGenie : MonoBehaviour, IGenie
    {
        public string                             Species      => _original.Species;
        public string                             SubSpecies   => _original.SubSpecies;
        public string                             Lod          => _original.Lod;
        public GameObject                         Root         => gameObject;
        public GameObject                         ModelRoot    { get; private set; }
        public Transform                          SkeletonRoot { get; private set; }
        public Animator                           Animator     { get; private set; }
        public IReadOnlyList<SkinnedMeshRenderer> Renderers    { get; private set; }
        public GenieComponentManager              Components   { get; private set; }
        public bool                               IsDisposed   { get; private set; }

#pragma warning disable CS0067
        public event Action RootRebuilt;
        public event Action Rebuilt;
#pragma warning restore CS0067
        public event Action Disposed;

        // dependencies
        private IGenie _original;

        public static UniTask<CloneGenie> CreateAsync(IGenie original, int onLayer = -1)
        {
            return CreateAsync<CloneGenie>(original, onLayer);
        }

        public static async UniTask<T> CreateAsync<T>(IGenie original, int onLayer = -1)
            where T : CloneGenie
        {
            GameObject cloneGo = await BuildCloneGoAsync(original, onLayer);
            var clone = cloneGo.AddComponent<T>();
            clone._original = original;

            // find the model root
            string modelRootPath = original.ModelRoot.transform.GetPathRelativeTo(original.Root.transform);
            clone.ModelRoot = modelRootPath is null ? null : cloneGo.transform.Find(modelRootPath).gameObject;

            // find the skeleton root
            string skeletonRootPath = original.SkeletonRoot.GetPathRelativeTo(original.Root.transform);
            clone.SkeletonRoot = skeletonRootPath is null ? null : cloneGo.transform.Find(skeletonRootPath);

            // get the animator component
            clone.Animator = cloneGo.GetComponentInChildren<Animator>();

            // get renderers
            var renderers = new List<SkinnedMeshRenderer>(original.Renderers.Count);
            clone.Renderers ??= renderers.AsReadOnly();
            foreach (SkinnedMeshRenderer renderer in original.Renderers)
            {
                string rendererPath = renderer.transform.GetPathRelativeTo(original.Root.transform);
                if (rendererPath is null)
                {
                    continue;
                }

                Transform cloneRendererTransform = cloneGo.transform.Find(rendererPath);
                var cloneRenderer = cloneRendererTransform.GetComponent<SkinnedMeshRenderer>();
                if (cloneRenderer)
                {
                    renderers.Add(cloneRenderer);
                }
            }

            // copy all components from original (excluding any animation feature)
            clone.Components = new GenieComponentManager(clone);
            if (original.Components is not null)
            {
                foreach (GenieComponent component in original.Components.All)
                {
                    if (!component.IsAnimationFeature)
                    {
                        clone.Components.Add(component.Copy());
                    }
                }
            }

            // add reference to the GameObject
            GenieReference.Create(clone, cloneGo, disposeOnDestroy: false);

            // dispose the clone on any changes done to the original genie
            original.Disposed += clone.Dispose;
            original.Rebuilt += clone.Dispose;
            original.RootRebuilt += clone.Dispose;

            return clone;
        }

        public virtual async UniTask<IGenie> CloneAsync(int onLayer = -1)
        {
            IGenie genie = IsDisposed ? null : await CreateAsync(_original, onLayer);
            return genie;
        }

        public virtual UniTask<IGenie> BakeAsync(Transform parent = null, bool urpBake = false)
        {
            return _original.BakeAsync(parent, urpBake);
        }

        public virtual UniTask<IGenieSnapshot> TakeSnapshotAsync(Transform parent = null, bool urpBake = false)
        {
            return _original.TakeSnapshotAsync(parent, urpBake);
        }

        public virtual void Dispose()
        {
            if (IsDisposed)
            {
                return;
            }

            IsDisposed = true;

            if (!_original.IsDisposed)
            {
                _original.Disposed -= Dispose;
                _original.Rebuilt -= Dispose;
                _original.RootRebuilt -= Dispose;
            }

            Components?.RemoveAll();
            Components = null;
            _original = null;

            if (gameObject)
            {
                Destroy(gameObject);
            }

            Disposed?.Invoke();
        }

        protected virtual void OnDestroy()
        {
            // make sure we are disposed automatically if destroyed
            Dispose();
        }

        private static async UniTask<GameObject> BuildCloneGoAsync(IGenie original, int onLayer = -1)
        {
            var tmpRootGo = new GameObject("Tmp Clone Genie Root");

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
            GameObject cloneGo = await GenieUtilities.CloneRootAsync(original, tmpRootGo.transform);
            cloneGo.name = $"{original.Root.name} (Clone)";

            // set to given layer
            if (onLayer != -1)
            {
                cloneGo.SetLayerRecursive(onLayer);
            }

            // remove UMA and Genie components if any
            GenieUtilities.RemoveUmaComponents(cloneGo);
            var genieComponent = cloneGo.GetComponent<Genie>();
            if (genieComponent)
            {
                DestroyImmediate(genieComponent);
            }

            // move the clone object out of the tmp root
            cloneGo.transform.SetParent(null, worldPositionStays: true);
            Destroy(tmpRootGo);

            return cloneGo;
        }
    }
}
