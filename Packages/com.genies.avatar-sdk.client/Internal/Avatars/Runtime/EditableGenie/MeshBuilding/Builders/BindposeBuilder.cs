using System.Collections.Generic;

namespace Genies.Avatars
{
    public sealed class BindposeBuilder
    {
        /// <summary>
        /// The total number of unique bindposes.
        /// </summary>
        public int BindposeCount => _indicesByBindpose.Count;
        
        /// <summary>
        /// The total number of added bindposes (how many calls to <see cref="AddBindpose"/> where done).
        /// </summary>
        public int AddedBindposeCount => _indicesByAddOrder.Count;
        
        private readonly Dictionary<BindposeData, int> _indicesByBindpose = new();
        private readonly List<int>                     _indicesByAddOrder = new();

        public void AddBindpose(BindposeData bindpose, bool ignoreMatrix = false)
        {
            // If this bindpose's hash is already in the dictionary, let's reuse the one with the matching hash.
            if (ignoreMatrix)
            {
                foreach (BindposeData existing in _indicesByBindpose.Keys)
                {
                    if (existing.BoneHash == bindpose.BoneHash)
                    {
                        _indicesByAddOrder.Add(_indicesByBindpose[existing]);
                        return;
                    }
                }
            }
            
            if (!_indicesByBindpose.TryGetValue(bindpose, out int index))
            {
                index = _indicesByBindpose.Count;
                _indicesByBindpose.Add(bindpose, index);
            }
            
            _indicesByAddOrder.Add(index);
        }

        /// <summary>
        /// Gets the bindpose index for the bindpose that was added in the given index (order).
        /// </summary>
        public int GetBindposeIndexByAddedOrder(int addedIndex)
        {
            return _indicesByAddOrder[addedIndex];
        }

        public bool TryGetBindposeIndex(BindposeData bindpose, out int index)
        {
            return _indicesByBindpose.TryGetValue(bindpose, out index);
        }

        public BindposeData[] CreateBindposeArray()
        {
            var bindposes = new BindposeData[BindposeCount];

            foreach ((BindposeData bindpose, int index) in _indicesByBindpose)
            {
                bindposes[index] = bindpose;
            }

            return bindposes;
        }

        public void Clear()
        {
            _indicesByBindpose.Clear();
            _indicesByAddOrder.Clear();
        }
    }
}