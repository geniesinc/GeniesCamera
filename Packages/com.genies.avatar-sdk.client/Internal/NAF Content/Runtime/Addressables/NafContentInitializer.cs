using System.Collections.Generic;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Genies.Addressables.Naf;
using Genies.Addressables.Utils;
using Genies.Login.Native;
using Genies.ServiceManagement;

namespace Genies.Naf.Content
{
    public static class NafContentInitializer
    {
        public static bool IsInitialized { get; private set; }

        public static bool IncludeV1Inventory { get; set; }

        public static async UniTask Initialize()
        {
            if (IsInitialized)
            {
                return;
            }
            IsInitialized = true;

            // If this class can be disposed, make sure to unsubscribe.
            GeniesLoginSdk.UserLoggedIn -= OnGeniesUserLoggedIn;
            GeniesLoginSdk.UserLoggedIn += OnGeniesUserLoggedIn;

            await FetchLocationsAsync(); // Returns if user is not logged in.
        }

        private static async void OnGeniesUserLoggedIn()
        {
            await FetchLocationsAsync();
        }

        private static async Task FetchLocationsAsync()
        {
            if (GeniesLoginSdk.IsUserSignedIn() is false)
            {
                return;
            }
            
            GeniesAddressablesUtils.RegisterNewResourceProviderOnAddressables(new UniversalContentResourceProvider());
            var locationTasks = new List<UniTask>();
            
            // Fetch inventory locations for all external
            var locationsFromInventory = ServiceManager.Get<IInventoryNafLocationsProvider>();
            var loadInvLocationsTask = locationsFromInventory.AddCustomResourceLocationsFromInventory(IncludeV1Inventory);
            locationTasks.Add(loadInvLocationsTask);

            await UniTask.WhenAll(locationTasks);
        }
    }
}
