using System.Collections.Generic;
using UnityEngine;

namespace Genies.Avatars.Sdk
{
    internal sealed class AvatarOverlayController
    {
        private readonly IGenie _genie;
        private readonly Dictionary<AvatarOverlay, OverlayInstance> _overlays;
        private Transform _genieRoot;

        public AvatarOverlayController(IGenie genie)
        {
            _genie = genie;

            _overlays = new Dictionary<AvatarOverlay, OverlayInstance>();

            _genie.RootRebuilt += OnGenieRootRebuilt;
            OnGenieRootRebuilt();
        }

        public void Add(AvatarOverlay overlay)
        {
            if (!overlay || !_genieRoot || _overlays.ContainsKey(overlay))
            {
                return;
            }

            var instance = new OverlayInstance(overlay);
            instance.Rebuild(_genieRoot);
            _overlays[overlay] = instance;
        }

        public bool Remove(AvatarOverlay overlay)
        {
            if (!overlay || !_overlays.TryGetValue(overlay, out OverlayInstance instance))
            {
                return false;
            }

            instance.Destroy();
            _overlays.Remove(overlay);
            return true;
        }

        public void RemoveAll()
        {
            foreach (OverlayInstance instance in _overlays.Values)
            {
                instance.Destroy();
            }

            _overlays.Clear();
        }

        private void OnGenieRootRebuilt()
        {
            _genieRoot = GetGenieRoot();
            if (!_genieRoot)
            {
                Debug.LogError($"[{nameof(AvatarOverlayController)}] could not find the Avatar root for overlays");
                return;
            }

            foreach (OverlayInstance instance in _overlays.Values)
            {
                instance.Rebuild(_genieRoot);
            }
        }

        private Transform GetGenieRoot()
        {
            Transform genieTransform = _genie.Root.transform;

            for (int i = 0; i < genieTransform.childCount; ++i)
            {
                Transform child = genieTransform.GetChild(i);
                if (child.name == "Root")
                {
                    return child;
                }
            }

            return null;
        }

        private sealed class OverlayInstance
        {
            public readonly string Name;

            private readonly AvatarOverlay _overlay;
            private readonly List<GameObject> _gameObjects;

            public OverlayInstance(AvatarOverlay overlay)
            {
                Name = overlay.name;

                _overlay = overlay;
                _gameObjects = new List<GameObject>();
            }

            public void Rebuild(Transform destinationRoot)
            {
                Destroy();

                if (!_overlay.TryGetRoot(out Transform overlayRoot))
                {
                    Debug.LogError($"[{nameof(AvatarOverlayController)}] avatar overlay has no root transform: {Name}");
                    return;
                }

                Transform root = Object.Instantiate(overlayRoot);
                AddTree(root, destinationRoot);
            }

            public void Destroy()
            {
                foreach (GameObject gameObject in _gameObjects)
                {
                    if (gameObject)
                    {
                        Object.Destroy(gameObject);
                    }
                }

                _gameObjects.Clear();
            }

            private void AddTree(Transform root, Transform destination)
            {
                _gameObjects.Add(root.gameObject);
                root.name = $"[Ov] {Name}";
                root.SetParent(destination, worldPositionStays: false);
                root.localPosition = Vector3.zero;
                root.localRotation = Quaternion.identity;
                root.localScale = Vector3.one;

                // iterate over the root children and find destination children with the same name, in that case add the tree recursively
                for (int i = 0; i < root.childCount; ++i)
                {
                    Transform child = root.GetChild(i);

                    if (TryGetDirectChildByName(destination, child.name, out Transform parentChild))
                    {
                        AddTree(child, parentChild);
                        --i;
                    }
                }
            }

            private static bool TryGetDirectChildByName(Transform transform, string name, out Transform child)
            {
                for (int i = 0; i < transform.childCount; ++i)
                {
                    child = transform.GetChild(i);
                    if (child.name == name)
                    {
                        return true;
                    }
                }

                child = null;
                return false;
            }
        }
    }
}
