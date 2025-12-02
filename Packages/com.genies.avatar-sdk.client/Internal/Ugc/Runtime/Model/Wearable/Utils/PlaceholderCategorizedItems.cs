using System.Collections.Generic;

namespace Genies.Ugc
{
    public class PlaceholderCategorizedItems<T> : ICategorizedItems<T>
    {
        public static PlaceholderCategorizedItems<T> Instance => _instance ??= new PlaceholderCategorizedItems<T>();
        
        private static PlaceholderCategorizedItems<T> _instance;
        private static readonly IReadOnlyList<T> EmptyItems = new List<T>(0).AsReadOnly();

        public List<string> Categories { get; } = new List<string>();
        public string DefaultCategory => null;
        
        public IReadOnlyList<T> GetItems(string category = null)
            => EmptyItems;
        public int GetItemCount(string category = null)
            => 0;
        public bool ContainsItem(T item, string category = null)
            => false;
        public string GetItemDisplayName(T item)
            => null;
        public string GetItemCategory(T item)
            => null;
        public T GetDefaultItem(string category = null)
            => default;
    }
}
