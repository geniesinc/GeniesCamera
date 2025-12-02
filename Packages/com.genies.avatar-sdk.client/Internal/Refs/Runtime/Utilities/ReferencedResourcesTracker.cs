using System;

namespace Genies.Refs
{
    /// <summary>
    /// Globally tracks any resources that are referenced using <see cref="Ref{T}"/> or <see cref="Handle{T}"/>.
    /// If you have access to a resource but don't know if it is using the refs or not you can try to get its handle
    /// or a new reference to it using this class.
    /// </summary>
    public static class ReferencedResourcesTracker
    {
        private static class HandleRegistry<T>
        {
            public static readonly HandleCache<string, T> Cache = new();

            // use a delegate instead of a normal method to avoid heap allocations when subscribing to the released event of the handles
            public static readonly Action<T> ReleaseCachedHandle = resource =>
            {
                string key = GenerateKey(resource);
                Cache.Release(key);
            };
        }

        public static bool TryGetHandle<T>(T resource, out Handle<T> handle)
        {
            string key = GenerateKey(resource);
            return HandleRegistry<T>.Cache.TryGetHandle(key, out handle);
        }

        public static bool TryGetNewReference<T>(T resource, out Ref<T> reference)
        {
            string key = GenerateKey(resource);
            return HandleRegistry<T>.Cache.TryGetNewReference(key, out reference);
        }
        
        public static bool IsResourceTracked<T>(T resource)
        {
            string key = GenerateKey(resource);
            return HandleRegistry<T>.Cache.IsHandleCached(key);
        }
        
        public static int GetCount<T>()
        {
            return HandleRegistry<T>.Cache.Count;
        }

        internal static void TrackHandle<T>(IGenerationalResourceHandle<T> resourceHandle)
        {
            if (resourceHandle.Resource is null)
            {
                return;
            }

            // automatically remove the cached handle entry when it gets released
            var handle = new Handle<T>(resourceHandle);
            handle.Releasing += HandleRegistry<T>.ReleaseCachedHandle;
            string key = GenerateKey(resourceHandle.Resource);
            HandleRegistry<T>.Cache.CacheHandle(key, handle);
        }

        // <summary>
        // generates a string from the key to avoid directly using handles of resources as keys
        // </summary>
        private static string GenerateKey<T>(T key)
        {
            if (key == null)
            {
                return string.Empty;
            }

            // combine GetHashCode with ToString for better uniqueness
            return $"{key.GetHashCode()}-{key.ToString()}";
        }
    }
}
