using System.Collections.Generic;
using UnityEngine;

namespace Genies.Utilities.Internal
{
    /// <summary>
    /// Provides utility methods for UI.
    /// </summary>
    public static class UIUtils
    {
        /// <summary>
        /// Instantiates/removes enough prefabs to match amount
        /// </summary>
        /// <param name="prefab">The prefab to instantiate or remove.</param>
        /// <param name="amount">The desired amount of prefabs to be instantiated or removed.</param>
        /// <param name="parent">The Transform that the instantiated prefabs will be parented to.</param>
        public static void BalanceChildPrefabs<T>(T prefab, int amount, Transform parent) where T : Object
        {
            // instantiate until amount
            for (var i = parent.childCount; i < amount; ++i)
            {
                Object.Instantiate(prefab, parent);
            }

            // delete everything that's too much
            // (backwards loop because Destroy changes childCount)
            for (var i = parent.childCount-1; i >= amount; --i)
            {
                Object.Destroy(parent.GetChild(i).gameObject);
            }
        }

        /// <summary>
        /// Instantiates/removes prefabs of type T and maintains them in the given list.
        /// </summary>
        /// <param name="prefab">The prefab to instantiate.</param>
        /// <param name="amount">The desired amount of prefabs to be instantiated.</param>
        /// <param name="parent">The Transform that the instantiated prefabs will be parented to.</param>
        /// <param name="prefabList">The list that maintains the amount of instantiated prefab of type T.</param>
        /// <returns>A list of instantiated prefabs of type T.</returns>
        public static void BalanceListOfChildPrefabs<T>(T prefab, int amount, Transform parent, List<T> prefabList) where T : Object
        {
            if (prefabList == null)
            {
                Debug.LogError("prefabList parameter is null.");
                return;
            }

            // instantiate prefabs and add them to the list
            for (var i = prefabList.Count; i < amount; ++i)
            {
                prefabList.Add(Object.Instantiate(prefab, parent));
            }

            // remove and destroy everything that's too much
            for (var i = prefabList.Count - 1; i >= amount; --i)
            {
                Transform child = parent.GetChild(i);

                // Remove the component from the prefabList if it exists
                if (child.TryGetComponent<T>(out T component))
                {
                    prefabList.Remove(component);
                }

                // Destroy the extra child GameObject
                Object.Destroy(child.gameObject);
            }
        }
    }
}
