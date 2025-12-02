using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;

namespace Genies.Utilities
{
    public static class AddressableRuntimeUtils
    {
        public static void UnloadAllAddressables()
        {
            var handles = GetAllAsyncOperationHandles();
            ReleaseAsyncOperationHandles(handles);
        }

        public static void ClearAddressablesCache()
        {
            UnloadAllAddressables();
            Resources.UnloadUnusedAssets();
            List<IResourceLocation> allLocations = GetAllAddressableLocations();
            Addressables.ClearDependencyCacheAsync(allLocations);
        }

        public static List<IResourceLocation> GetAllAddressableLocations()
        {
            var allLocations = new List<IResourceLocation>();
            foreach (var resourceLocator in Addressables.ResourceLocators)
            {
                if (resourceLocator is ResourceLocationMap map)
                {
                    foreach (var locations in map.Locations.Values)
                    {
                        allLocations.AddRange(locations);
                    }
                }
            }

            return allLocations;
        }
        private static List<AsyncOperationHandle> GetAllAsyncOperationHandles()
        {
            // Workaround for problems:
            // https://discussions.unity.com/t/843869

            var handles = new List<AsyncOperationHandle>();

            var resourceManagerType = Addressables.ResourceManager.GetType();
            var dictionaryMember = resourceManagerType.GetField("m_AssetOperationCache", BindingFlags.NonPublic | BindingFlags.Instance);
            var dictionary = dictionaryMember.GetValue(Addressables.ResourceManager) as IDictionary;

            foreach (var asyncOperationInterface in dictionary.Values)
            {
                if (asyncOperationInterface == null)
                {
                    continue;
                }

                var handle = typeof(AsyncOperationHandle).InvokeMember(nameof(AsyncOperationHandle),
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.CreateInstance,
                    null, null, new object[] { asyncOperationInterface });

                handles.Add((AsyncOperationHandle)handle);
            }

            return handles;
        }

        private static void ReleaseAsyncOperationHandles(List<AsyncOperationHandle> handles)
        {
            foreach (var handle in handles)
            {
                if (!handle.IsDone)
                {
                    Debug.LogWarning($"AsyncOperationHandle {handle} not completed yet. Releasing anyway!");
                }

                while (handle.IsValid())
                {
                    Addressables.ResourceManager.Release(handle);
                }
            }
        }
    }
}
