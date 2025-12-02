using System.Collections.Generic;
using Genies.Utilities;
using UnityEngine;
using UnityEngine.UI;

namespace Genies.UI.Scroller
{
    /// <summary>
    /// A factory for creating <see cref="OptimizedScrollerCellHolder"/>
    /// </summary>
    public static class OptimizedScrollerFactory
    {
        private static readonly Stack<OptimizedScrollerCellHolder> _cellHolderPool = new Stack<OptimizedScrollerCellHolder>();
        private static GameObject _poolParent;
        private static OptimizedScrollerCellHolder _toClone;

        static OptimizedScrollerFactory()
        {
            _poolParent = new GameObject("Optimized Scroller Pool");
            _poolParent.AddComponent<DontDestroyOnLoad>();


            var go            = new GameObject("CellHolder");
            var rt            = go.AddComponent<RectTransform>();
            var layoutElement = go.AddComponent<LayoutElement>();

            _toClone = go.AddComponent<OptimizedScrollerCellHolder>();
            _toClone.rectTransform = rt;
            _toClone.layoutElement = layoutElement;

            _toClone.rectTransform.SetParent(_poolParent.transform);
        }

        public static void Dispose()
        {
            //Clear pools
            foreach (var holder in _cellHolderPool)
            {
                Object.Destroy(holder);
            }

            _cellHolderPool.Clear();
        }

        private static OptimizedScrollerCellHolder CreateCellHolder(Vector2 size, Transform cellParent = null )
        {
            var clone = Object.Instantiate(_toClone, cellParent, false);
            clone.layoutElement.preferredHeight = size.y;
            clone.layoutElement.preferredWidth = size.x;
            return clone;
        }

        /// <summary>
        /// Get an instance from the pool
        /// </summary>
        /// <param name="size"> The size of the cell holder </param>
        /// <param name="cellParent"> Parent of the cell </param>
        public static OptimizedScrollerCellHolder GetCellHolderFromPool(Vector2 size, Transform cellParent = null)
        {
            OptimizedScrollerCellHolder newCell;

            if (_cellHolderPool.Count <= 0)
            {
                newCell = CreateCellHolder(size, cellParent);
                return newCell;
            }

            newCell = _cellHolderPool.Pop();
            newCell.transform.SetParent(cellParent);
            newCell.layoutElement.preferredHeight = size.y;
            newCell.layoutElement.preferredWidth = size.x;
            newCell.gameObject.SetActive(true);

            return newCell;
        }

        /// <summary>
        /// Return an instance to the pool
        /// </summary>
        /// <param name="instance"> Instance to return </param>
        public static void ReturnToPool(OptimizedScrollerCellHolder instance)
        {
            instance.gameObject.SetActive(false);
            instance.rectTransform.SetParent(_poolParent.transform, false);

            _cellHolderPool.Push(instance);
        }
    }
}
