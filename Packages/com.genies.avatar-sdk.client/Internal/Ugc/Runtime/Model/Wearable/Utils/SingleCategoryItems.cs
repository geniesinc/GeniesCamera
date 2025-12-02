using System.Collections.Generic;
using System.Linq;

namespace Genies.Ugc
{
    /// <summary>
    /// Helper implementation of ICategorizedItems for when you need to provide one but you only have a single collection of items with no categories.
    /// </summary>
    public class SingleCategoryItems<T> : ICategorizedItems<T>
    {
        public List<string> Categories { get; } = new List<string>(0);
        public string DefaultCategory => null;
        
        private readonly List<T> _items;
        
        public SingleCategoryItems(IEnumerable<T> items = null)
        {
            _items = new List<T>();
            SetItems(items);
        }

        public void SetItems(IEnumerable<T> items)
        {
            _items.Clear();
            
            if (items != null)
            {
                _items.AddRange(items);
            }
        }

        public IReadOnlyList<T> GetItems(string category = null)
        {
            if (category is null)
            {
                return _items;
            }

            return null;
        }

        public int GetItemCount(string category = null)
        {
            return GetItems(category)?.Count ?? 0;
        }

        public bool ContainsItem(T item, string category = null)
        {
            return GetItems(category)?.Contains(item) ?? false;
        }

        public string GetItemDisplayName(T item)
        {
            return null;
        }

        public string GetItemCategory(T item)
        {
            return DefaultCategory;
        }

        public T GetDefaultItem(string category = null)
        {
            return _items.FirstOrDefault();
        }
    }
}
