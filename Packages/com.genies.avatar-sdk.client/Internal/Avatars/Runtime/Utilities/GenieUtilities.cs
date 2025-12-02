using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Genies.Utilities;
using UMA;
using UMA.CharacterSystem;
using UnityEngine;

namespace Genies.Avatars
{
    public static class GenieUtilities
    {
        public static Bounds GetRendererBounds(IGenie genie)
        {
            if (genie.Renderers.Count == 0)
            {
                return default;
            }

            Bounds bounds = GetRendererBounds(genie.Renderers[0], genie.Root.transform);
            for (int i = 1; i < genie.Renderers.Count; ++i)
            {
                bounds.Encapsulate(GetRendererBounds(genie.Renderers[i], genie.Root.transform));
            }

            return bounds;
        }

        public static Bounds GetRendererBounds(SkinnedMeshRenderer renderer, Transform root = null)
        {
            /**
             * This commented coud was the previous method to get the bounds, relying on Unity's SkinnedMeshRenderer
             * implementation, which was buggy and unreliable, so we implemented our own bounds calculation algorithm
             * levaraging parallel for jobs and the burst compiler for performance.
             */
            // bool updateWhenOffScreen = renderer.updateWhenOffscreen;
            // renderer.updateWhenOffscreen = true;
            // Bounds bounds = renderer.bounds;
            // renderer.updateWhenOffscreen = updateWhenOffScreen;
            // return bounds;

            return renderer.GetPoseBounds(root);
        }

        public static Bounds GetMeshBounds(IGenie genie)
        {
            if (genie.Renderers.Count == 0)
            {
                return default;
            }

            Bounds bounds = genie.Renderers[0].sharedMesh.bounds;
            for (int i = 1; i < genie.Renderers.Count; ++i)
            {
                bounds.Encapsulate(genie.Renderers[i].sharedMesh.bounds);
            }

            return bounds;
        }

        /// <summary>
        /// Recommended way to clone the Root GameObject from any <see cref="IGenie"/> instance. It handles
        /// the components properly and removes genie reference components from the tree.
        /// </summary>
        public static async UniTask<GameObject> CloneRootAsync(IGenie genie, Transform parent = null)
        {
            if (genie.IsDisposed)
            {
                return null;
            }

            // silently remove all components temporarily from the genie so we can do a clean clone of the root
            List<GenieComponent> components = GetAllGenieComponentsExceptAnimationFeatures(genie);
            genie.Components.RemoveAll(notify: false);

            // wait for a couple of frames since some removed components may have removed MonoBehaviours that will not take effect until next frame
            await UniTask.DelayFrame(2);

            // clone root
            GameObject clone = Object.Instantiate(genie.Root, parent);

            // silently restore removed components
            genie.Components.Add(components, notify: false);

            // remove any genie reference components from the cloned GameObject tree
            RemoveGenieReferenceComponents(clone);

            return clone;
        }

        public static List<GenieComponent> GetAllGenieComponentsExceptAnimationFeatures(IGenie genie)
        {
            var components = new List<GenieComponent>(genie.Components.All);
            if (!genie.Components.TryGet(out AnimationFeatureManager animationFeatureManager))
            {
                return components;
            }

            // remove all animation feature components
            for (int i = components.Count - 1; i >= 0; --i)
            {
                if (components[i].IsAnimationFeature)
                {
                    components.RemoveAt(i);
                }
            }

            return components;
        }

        public static void RemoveGenieReferenceComponents(GameObject gameObject)
        {
            RemoveComponentsInChildren<GenieReference>(gameObject);
            RemoveComponentsInChildren<EditableGenieReference>(gameObject);
            RemoveComponentsInChildren<SpeciesGenieControllerReference>(gameObject);
        }

        public static void RemoveUmaComponents(GameObject gameObject)
        {
            RemoveComponentsInChildren<UmaGenie>(gameObject);
            RemoveComponentsInChildren<DynamicCharacterAvatar>(gameObject);
            RemoveComponentsInChildren<UMAData>(gameObject);
        }

        private static void RemoveComponentsInChildren<T>(GameObject gameObject)
            where T : Component
        {
            if (!gameObject)
            {
                return;
            }

            T[] components = gameObject.GetComponentsInChildren<T>();
            foreach (T component in components)
            {
                Object.DestroyImmediate(component);
            }
        }
    }
}
