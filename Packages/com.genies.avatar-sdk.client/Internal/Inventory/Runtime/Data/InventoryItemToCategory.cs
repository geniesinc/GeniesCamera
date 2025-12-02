using Toolbox.Core;
using UnityEngine;

namespace Genies.Inventory
{
#if GENIES_INTERNAL
    [CreateAssetMenu(fileName = "InventoryItemToCategory", menuName = "Genies/Inventory/InventoryItemToCategory")]
#endif
    public class InventoryItemToCategory : ScriptableObject
    {
        /// <summary>
        /// A dictionary which links a wearable type to the plural category it belongs to
        /// </summary>
        [SerializeField] private SerializedDictionary<string, string> _itemToCategory;

        public SerializedDictionary<string, string> ItemToCategory => _itemToCategory;
    }
}
