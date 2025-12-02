using System.Linq;
using Genies.CrashReporting;
using Genies.Models;
using UnityEngine;
using UnityEngine.ResourceManagement.ResourceLocations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityAddressables = UnityEngine.AddressableAssets.Addressables;

namespace Genies.Addressables.CustomResourceLocation
{
    public static class CustomResourceLocationUtils
    {
        public static void AddCustomLocator(ResourceLocationMetadata locationMetadata)
        {
            if (locationMetadata == null)
            {
                CrashReporter.LogWarning("Attempted to add a null ResourceLocationMetadata.");
                return;
            }

            var updateLocator = false;
            var newLocator = false;

            CustomResourceLocator customResourceLocator = UnityAddressables
                .ResourceLocators
                .OfType<CustomResourceLocator>()
                .FirstOrDefault();

            if (customResourceLocator == null)
            {
                customResourceLocator = new CustomResourceLocator();
                newLocator = true;
            }

            var requestOptions = new AssetBundleRequestOptions
            {
                UseCrcForCachedBundle = false, // Enable if CRC validation is needed
                ChunkedTransfer = true, // Helps with streaming large bundles
                RedirectLimit = -1, // Allow infinite redirects
                RetryCount = 3, // Retry on failure
                Timeout = 30, // Network timeout (seconds)
            };

            // Check if bundle location already exists
            IResourceLocation bundleLocation = customResourceLocator.Locations
                .SelectMany(kvp => kvp.Value)
                .OfType<ResourceLocationBase>()
                .FirstOrDefault(loc =>
                    loc.ProviderId == CustomAssetBundleProvider.CustomProviderId &&
                    loc.PrimaryKey == locationMetadata.BundleKey &&
                    loc.InternalId == locationMetadata.RemoteUrl &&
                    loc.ResourceType == typeof(AssetBundle));

            if (bundleLocation == null)
            {
                updateLocator = true;
                // 📦 Create new AssetBundle Location (Remote)
                bundleLocation = new ResourceLocationBase(
                    locationMetadata.BundleKey, // Key for the AssetBundleProvider
                    locationMetadata.RemoteUrl, // Remote URL
                    CustomAssetBundleProvider.CustomProviderId, // Uses AssetBundleProvider
                    typeof(AssetBundle)
                );

                // Use a method to set the data if available, or use a custom implementation
                if (bundleLocation is ResourceLocationBase resourceLocationBase)
                {
                    resourceLocationBase.Data = requestOptions; // Attach request options
                }

                customResourceLocator.AddLocation(locationMetadata.BundleKey, bundleLocation);
            }

            // Check if asset location already exists
            IResourceLocation existingAssetLocation = customResourceLocator.Locations
                .SelectMany(kvp => kvp.Value)
                .OfType<ResourceLocationBase>()
                .FirstOrDefault(loc =>
                    loc.ProviderId == CustomBundledAssetProvider.CustomProviderId &&
                    loc.PrimaryKey == locationMetadata.Address &&
                    loc.InternalId == locationMetadata.InternalId &&
                    loc.ResourceType == locationMetadata.Type
                );

            if (existingAssetLocation == null)
            {
                updateLocator = true;
                // 🏷 Create Asset inside the AssetBundle (References the GUID)
                var assetLocation = new ResourceLocationBase(
                    locationMetadata.Address, // Key is the GUID (or primary label)
                    locationMetadata.InternalId, // Internal ID
                    CustomBundledAssetProvider.CustomProviderId, // Uses BundledAssetProvider
                    locationMetadata.Type, // Asset type
                    bundleLocation // Dependency on the bundle
                );

                customResourceLocator.AddLocation(locationMetadata.Address, assetLocation, locationMetadata.Labels);
            }

            // 🔄 Register locator with Addressables if new locations were added
            if (customResourceLocator.Keys.Any())
            {
                if (!updateLocator)
                {
                    return;
                }

                if (!newLocator)
                {
                    UnityAddressables.RemoveResourceLocator(customResourceLocator);
                }

                UnityAddressables.AddResourceLocator(customResourceLocator);
            }
        }
    }
}
