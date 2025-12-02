using System;
using System.Collections.Generic;

namespace Genies.Avatars
{
    /// <summary>
    /// Simple thread-safe internal static pool of lists.
    /// </summary>
    internal static class ListPool<T>
    {
        [ThreadStatic] private static Stack<List<T>> _pool;

        public static List<T> Get()
        {
            _pool ??= new Stack<List<T>>();
            return _pool.Count == 0 ? new List<T>() : _pool.Pop();
        }

        public static void Release(List<T> item)
        {
            item.Clear();
            _pool.Push(item);
        }
    }
}