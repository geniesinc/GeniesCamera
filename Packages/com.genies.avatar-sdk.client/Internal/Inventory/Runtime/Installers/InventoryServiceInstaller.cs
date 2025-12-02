using Genies.FeatureFlags;
using Genies.ServiceManagement;
using UnityEngine;
using VContainer;

namespace Genies.Inventory.Installers
{
    [AutoResolve]
    public class InventoryServiceInstaller: IGeniesInstaller
    {
        [Header("Optional Override")]
        [SerializeField] private InventoryItemToCategory _inventoryItemToCategory;
        [SerializeField] private string _partyId;

        public bool IncludeV1Inventory;

        public InventoryItemToCategory InventoryItemToCategoryDep
        {
            get => _inventoryItemToCategory;
            set => _inventoryItemToCategory = value;
        }

        public string PartyId
        {
            get => _partyId;
            set => _partyId = value;
        }

        public InventoryServiceInstaller()
        {
        }

        public InventoryServiceInstaller(string partyId)
        {
            PartyId = partyId;
        }

        public void Install(IContainerBuilder builder)
        {
            if (IncludeV1Inventory)
            {
                builder.Register<IInventoryService, InventoryService>(Lifetime.Singleton)
                    .WithParameter(InventoryItemToCategoryDep)
                    .WithParameter(PartyId).AsSelf();
            }

            builder.Register<IDefaultInventoryService, DefaultInventoryService>(Lifetime.Singleton);
        }
    }
}
