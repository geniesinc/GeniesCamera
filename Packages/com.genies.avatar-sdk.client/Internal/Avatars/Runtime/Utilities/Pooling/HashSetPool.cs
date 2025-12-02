using System;
using System.Collections.Generic;

namespace Genies.Avatars
{
    /// <summary>
    /// Simple thread-safe internal static pool of hash sets.
    /// </summary>
    internal static class HashSetPool<T>
    {
        [ThreadStatic] private static Stack<HashSet<T>> _pool;

        public static HashSet<T> Get()
        {
            _pool ??= new Stack<HashSet<T>>();
            return _pool.Count == 0 ? new HashSet<T>() : _pool.Pop();
        }

        public static void Release(HashSet<T> item)
        {
            item.Clear();
            _pool.Push(item);
        }
    }
}