using System.Collections.Generic;
using Genies.Utilities;
using UnityEngine;

namespace Genies.Customization.Framework.ItemPicker
{
    public static class ItemPickerCellPool
    {
        private static readonly Dictionary<ItemPickerCellView, Stack<Transform>> _pools = new Dictionary<ItemPickerCellView, Stack<Transform>>();
        private static readonly Dictionary<Transform, ItemPickerCellView> _componentCache = new Dictionary<Transform, ItemPickerCellView>();
        private static GameObject _poolParent;

        static ItemPickerCellPool()
        {
            _poolParent = new GameObject("Item Picker Pool");
            _poolParent.AddComponent<DontDestroyOnLoad>();
        }

        public static void Dispose()
        {
            //Clear pools
            foreach (var kvp in _pools)
            {
                var pool = kvp.Value;

                while (pool.Count > 0)
                {
                    var tf = pool.Pop();
                    Object.Destroy(tf.gameObject);
                }
            }

            _pools.Clear();

            //Clear cache
            _componentCache.Clear();
        }

        /// <summary>
        /// Get an instance from the pool
        /// </summary>
        /// <param name="prefab"> The prefab key </param>
        public static GameObject GetFromPool(ItemPickerCellView prefab)
        {
            GameObject newCell;

            if (!_pools.TryGetValue(prefab, out var pool))
            {
                pool = new Stack<Transform>();
                _pools.Add(prefab, pool);
            }

            if (pool.Count == 0)
            {
                var clone = Object.Instantiate(prefab, _poolParent.transform);
                _componentCache.Add(clone.transform, clone);
                newCell = clone.gameObject;
            }
            else
            {
                newCell = pool.Pop().gameObject;
                newCell.SetActive(true);
            }

            return newCell;
        }

        /// <summary>
        /// Return an instance to the pool
        /// </summary>
        /// <param name="prefabKey"> Key of the pool </param>
        /// <param name="instance"> Instance to return </param>
        public static void ReturnToPool(ItemPickerCellView prefabKey, GameObject instance)
        {
            var forTransform = instance.transform;

            forTransform.gameObject.SetActive(false);
            forTransform.SetParent(_poolParent.transform, false);

            if (!_pools.TryGetValue(prefabKey, out var pool))
            {
                Object.Destroy(instance);
                return;
            }

            pool?.Push(forTransform);
        }

        /// <summary>
        /// Returns the <see cref="ItemPickerCellView"/> component from the given transform. If it's cached,
        /// it won't need to do a full GetComponent call.
        /// </summary>
        /// <param name="fromTransform"></param>
        /// <returns></returns>
        public static ItemPickerCellView GetCachedItemCellViewComponent(Transform fromTransform)
        {
            if (_componentCache.TryGetValue(fromTransform, out var component))
            {
                return component;
            }

            component = fromTransform.GetComponent<ItemPickerCellView>();
            _componentCache.Add(fromTransform, component);
            return component;
        }
    }
}
