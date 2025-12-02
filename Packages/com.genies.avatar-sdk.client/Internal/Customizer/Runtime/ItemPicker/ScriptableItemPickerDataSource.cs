using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Toolbox.Core;
using UnityEngine;

namespace Genies.Customization.Framework.ItemPicker
{
    public abstract class ScriptableItemPickerDataSource : ScriptableObject, IItemPickerDataSource
    {
        [Title("Layout Config")]
        [SerializeField]
        private ItemPickerLayoutConfig _layoutConfig;

        /// <summary>
        /// Default cell view prefab to use when none is specified by child classes
        /// </summary>
        [SerializeField]
        protected ItemPickerCellView _defaultCellView;

        /// <summary>
        /// Default cell size for item picker cells
        /// </summary>
        [SerializeField]
        protected Vector2 _defaultCellSize = new Vector2(56, 56);

        protected List<string> _ids = new();

        public abstract void Dispose();

        public abstract ItemPickerCtaConfig GetCtaConfig();

        public virtual ItemPickerLayoutConfig GetLayoutConfig()
        {
            return _layoutConfig;
        }

        public abstract int GetCurrentSelectedIndex();
        public virtual bool ItemSelectedIsValidForProcessCTA()
        {
            return true;
        }

        public abstract UniTask<int> InitializeAndGetCountAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Default implementation returns the default cell prefab.
        /// Override to provide custom cell prefabs per index.
        /// </summary>
        public virtual ItemPickerCellView GetCellPrefab(int index)
        {
            return _defaultCellView;
        }

        /// <summary>
        /// Default implementation returns the default cell size.
        /// Override to provide custom cell sizes per index.
        /// </summary>
        public virtual Vector2 GetCellSize(int index)
        {
            return _defaultCellSize;
        }

        /// <summary>
        /// Helper to get the currently selected index using a predicate.
        /// Consolidates repeated pattern of finding equipped items.
        /// </summary>
        protected virtual int GetCurrentSelectedIndexBase(Func<string, bool> isEquippedPredicate)
        {
            if (_ids == null || _ids.Count == 0)
            {
                return -1;
            }

            var equippedId = _ids.FirstOrDefault(isEquippedPredicate);
            return string.IsNullOrEmpty(equippedId) ? -1 : _ids.IndexOf(equippedId);
        }

        public abstract UniTask<bool> OnItemClickedAsync(int index, ItemPickerCellView clickedCell, bool wasSelected, CancellationToken cancellationToken);
        public abstract UniTask<bool> InitializeCellViewAsync(ItemPickerCellView view, int index, bool isSelected, CancellationToken cancellationToken);

        public virtual void DisposeCellViewAsync(ItemPickerCellView view, int index)
        {
            if (view != null)
            {
                view.Dispose();
            }
        }

        // Default pagination implementation - can be overridden by derived classes
        public virtual UniTask<bool> LoadMoreItemsAsync(CancellationToken cancellationToken)
        {
            return UniTask.FromResult(false);
        }

        public virtual bool HasMoreItems => false;
        public virtual bool IsLoadingMore => false;
    }
}
