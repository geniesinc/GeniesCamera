using System.Collections.Generic;

namespace Genies.Utilities
{
    /// <summary>
    /// Generic HashSet collection extension with some utility methods to track changes between two states of the set.
    /// <br/><br/>
    /// You must call <see cref="BeginTracking"/> to record the current state, perform changes and then call
    /// <see cref="FinishTracking"/> to calculate the changed items. The <see cref="AddedItems"/>, <see cref="RemovedItems"/>
    /// and <see cref="UnchangedItems"/> sets will contain those changes. The <see cref="Modified"/> property will return
    /// true if there were any changed items.
    /// </summary>
    public sealed class TrackedHashSet<T> : HashSet<T>
    {
        public bool IsTracking { get; private set; }
        public bool Modified => AddedItems.Count > 0 || RemovedItems.Count > 0;
        
        public readonly HashSet<T> AddedItems;
        public readonly HashSet<T> RemovedItems;
        public readonly HashSet<T> UnchangedItems;
        
        public TrackedHashSet()
        {
            AddedItems = new HashSet<T>(Comparer);
            RemovedItems = new HashSet<T>(Comparer);
            UnchangedItems = new HashSet<T>(Comparer);
        }

        public TrackedHashSet(IEqualityComparer<T> comparer)
            : base(comparer)
        {
            AddedItems = new HashSet<T>(Comparer);
            RemovedItems = new HashSet<T>(Comparer);
            UnchangedItems = new HashSet<T>(Comparer);
        }

        public TrackedHashSet(IEnumerable<T> collection)
          : base(collection)
        {
            AddedItems = new HashSet<T>(Comparer);
            RemovedItems = new HashSet<T>(Comparer);
            UnchangedItems = new HashSet<T>(Comparer);
        }

        public TrackedHashSet(IEnumerable<T> collection, IEqualityComparer<T> comparer)
            : base(collection, comparer)
        {
            AddedItems = new HashSet<T>(Comparer);
            RemovedItems = new HashSet<T>(Comparer);
            UnchangedItems = new HashSet<T>(Comparer);
        }

        /// <summary>
        /// Records the current state of the set. Any changes performed until <see cref="FinishTracking"/> is called
        /// will be recorded.
        /// </summary>
        public void BeginTracking()
        {
            // set the added and removed sets to the current state of this HashSet
            AddedItems.Clear();
            RemovedItems.Clear();
            UnchangedItems.Clear();
            AddedItems.UnionWith(this);
            RemovedItems.UnionWith(this);
            UnchangedItems.UnionWith(this);
            
            IsTracking = true;
        }

        /// <summary>
        /// If <see cref="BeginTracking"/> has been called before, this will will reflect any performed changes in
        /// the <see cref="Modified"/>, <see cref="AddedItems"/>, <see cref="RemovedItems"/> and <see cref="UnchangedItems"/>
        /// properties.
        /// </summary>
        public void FinishTracking()
        {
            // BeginTracking has not been called before so we have to reflect as if there has no been changes
            if (!IsTracking)
            {
                AddedItems.Clear();
                RemovedItems.Clear();
                UnchangedItems.Clear();
                UnchangedItems.UnionWith(this);
                return;
            }
            
            // union previous and current items except for the intersection between both. This is the actual diff which includes only the added and removed items
            AddedItems.SymmetricExceptWith(this);
            // remove the previous items (which includes the removed items) so we stay only with the added items
            AddedItems.ExceptWith(RemovedItems);
            
            // remove the current items from the previous ones, resulting in the removed items
            RemovedItems.ExceptWith(this);
            
            // compute the intersection between current and previous items, resulting in the unchanged items
            UnchangedItems.IntersectWith(this);
            
            IsTracking = false;
        }

        /// <summary>
        /// Replaces current items with the given items collection and tracks the changes of the operation.
        /// </summary>
        public void TrackAndSet(IEnumerable<T> items)
        {
            BeginTracking();
            
            Clear();
            UnionWith(items);
            
            FinishTracking();
        }
    }
}
