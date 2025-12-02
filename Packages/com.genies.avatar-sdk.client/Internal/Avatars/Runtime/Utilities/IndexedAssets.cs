using System.Collections.Generic;
using Genies.Refs;
using UnityEngine;

namespace Genies.Avatars
{
    /// <summary>
    /// Utility for using a <see cref="GeniesAssetIndexer"/> without caring about having to save all the index refs.
    /// </summary>
    public sealed class IndexedAssets
    {
        private readonly GeniesAssetIndexer _indexer;
        private readonly List<Ref<Object>> _indexRefs;

        public IndexedAssets(GeniesAssetIndexer indexer = null)
        {
            _indexer = indexer ?? GeniesAssetIndexer.Instance;
            _indexRefs = new List<Ref<Object>>();
        }

        public void Index(Object asset)
        {
            Ref<Object> indexRef = _indexer.Index(asset);
            _indexRefs.Add(indexRef);
        }

        public void Index<T>(IEnumerable<T> assets)
            where T : Object
        {
            foreach (T asset in assets)
            {
                Index(asset);
            }
        }

        /// <summary>
        /// Release all assets from the index.
        /// </summary>
        public void ReleaseAll()
        {
            foreach (Ref<Object> indexRef in _indexRefs)
            {
                indexRef.Dispose();
            }

            _indexRefs.Clear();
        }
    }
}