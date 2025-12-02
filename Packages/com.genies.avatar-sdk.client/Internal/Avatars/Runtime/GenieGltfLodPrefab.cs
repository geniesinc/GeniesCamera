using System.Collections.Generic;
using Genies.Utilities;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Genies.Avatars
{
    /// <summary>
    /// Internal implementation of <see cref="IGeniePrefab"/> used by the <see cref="GenieGltfImporter"/> to import
    /// single LODs.
    /// </summary>
    [AddComponentMenu("")] // hides this MonoBehaviour from the editor so it can only be added from code
    [DisallowMultipleComponent]
    internal sealed class GenieGltfLodPrefab : MonoBehaviour, IGeniePrefab
    {
        // we make this one serializable so Unity automatically handles relative references when instantiating new prefabs
        [SerializeField] private Genie.Config config;
        
        // this one is not serialized so only the original prefab has it
        private RuntimePrefab<GenieGltfLodPrefab> _runtimePrefab;
        private readonly HashSet<Genie> _createdGenies = new();

        // assumes the config contains one LOD only
        public static GenieGltfLodPrefab Create(GameObject gameObject, Genie.Config config)
        {
            var prefab = gameObject.AddComponent<GenieGltfLodPrefab>();
            prefab.config = config;
            prefab._runtimePrefab = new RuntimePrefab<GenieGltfLodPrefab>(prefab);
            
            return prefab;
        }

        public GameObject InstantiateLodRoot(Transform parent)
            => Object.Instantiate(config.lods[0].root, parent, worldPositionStays: false);
        public Genie InstantiateGenie()
            => CreateGenie(_runtimePrefab.Instantiate());
        public Genie InstantiateGenie(Transform parent)
            => CreateGenie(_runtimePrefab.Instantiate(parent));
        public Genie InstantiateGenie(Transform parent, bool worldPositionStays)
            => CreateGenie(_runtimePrefab.Instantiate(parent, worldPositionStays));
        public Genie InstantiateGenie(Vector3 position, Quaternion rotation)
            => CreateGenie(_runtimePrefab.Instantiate(position, rotation));
        public Genie InstantiateGenie(Vector3 position, Quaternion rotation, Transform parent)
            => CreateGenie(_runtimePrefab.Instantiate(position, rotation, parent));
        
#region IGenie implementation
        public IGenie Instantiate()
            => InstantiateGenie();
        public IGenie Instantiate(Transform parent)
            => InstantiateGenie(parent);
        public IGenie Instantiate(Transform parent, bool worldPositionStays)
            => InstantiateGenie(parent, worldPositionStays);
        public IGenie Instantiate(Vector3 position, Quaternion rotation)
            => InstantiateGenie(position, rotation);
        public IGenie Instantiate(Vector3 position, Quaternion rotation, Transform parent)
            => InstantiateGenie(position, rotation, parent);
#endregion

        private void OnDestroy()
        {
            // since disposing the genies will remove them from the _createdGenies hashset we need to do the foreach iteration with another collection
            var geniesToDispose = new List<Genie>(_createdGenies);
            foreach (Genie genie in geniesToDispose)
            {
                genie.Dispose();
            }
        }

        private Genie CreateGenie(GenieGltfLodPrefab newInstance)
        {
            // get the config automatically updated by Unity instancing methods
            Genie.Config newConfig = newInstance.config;
            
            // manually copy non-serialized fields
            newConfig.ComponentCreators = config.ComponentCreators;
            
            // destroy the GltfGeniePrefab component from the new instance
            GameObject newInstanceGo = newInstance.gameObject;
            DestroyImmediate(newInstance);
            
            // create a new Genie instance with the processed config
            var genie = Genie.Create(newInstanceGo, newConfig);
            genie.Disposed += () => _createdGenies.Remove(genie);
            _createdGenies.Add(genie);
            
            return genie;
        }
    }
}