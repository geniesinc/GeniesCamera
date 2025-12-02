using System;
using System.Collections.Generic;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.ResourceLocations;

namespace Genies.Utilities
{
    /// <summary>
    /// This class is needed to avoid conflicts between different transformers, unity only allows registering a single transformer,
    /// what we do here is allow registering multiple and the first one that succeeds we return its result.
    /// </summary>
    public static class AddressableTransformFuncUtility
    {
        public delegate bool TransformLocationIdHandler(IResourceLocation location, out string transformedId);

        private static HashSet<TransformLocationIdHandler> _transformers = new HashSet<TransformLocationIdHandler>();

        static AddressableTransformFuncUtility()
        {
            Addressables.InternalIdTransformFunc = InternalIdTransformFunc;
        }

        public static void RegisterTransformer(TransformLocationIdHandler handler)
        {
            _transformers.Add(handler);
        }

        /// <summary>
        /// run through the list of registered transformers and return the id of
        /// the first one that works.
        /// </summary>
        private static string InternalIdTransformFunc(IResourceLocation arg)
        {
            var id = arg.InternalId;

            if (_transformers == null || _transformers.Count == 0)
            {
                return id;
            }

            foreach (var t in _transformers)
            {
                if (t != null && t.Invoke(arg, out var transformedId))
                {
                    return transformedId;
                }
            }

            return id;
        }
    }
}
