using Genies.Models;
using System.Linq;
using Genies.Addressables.Naf;
using Genies.CrashReporting;
using UnityEngine.ResourceManagement.ResourceLocations;
using UnityAddressables = UnityEngine.AddressableAssets.Addressables;

namespace Genies.Addressables.Universal
{
    public static class UniversalContentResourceLocationUtils
    {
        /// <summary>
        /// Add custom locations to UnityAddressables.
        /// Internally Creates or Updates a UniversalContentResourceLocator for UnityAddressables containing
        /// IResourceLocations built from ResourceLocationMetadata.
        /// </summary>
        /// <param name="locationMetadata"></param>
        public static void AddUniversalLocations(ResourceLocationMetadata locationMetadata)
        {
            if (locationMetadata == null)
            {
                CrashReporter.LogWarning("Attempted to add a Null ResourceLocationMetadata.");
                return;
            }

            var updateLocator = false;
            var newLocator = false;

            // Check if CustomLocator already exist in UnityAddressables
            UniversalContentResourceLocator customResourceLocator = UnityAddressables
                .ResourceLocators
                .OfType<UniversalContentResourceLocator>()
                .FirstOrDefault();

            if (customResourceLocator == null)
            {
                customResourceLocator = new UniversalContentResourceLocator();
                newLocator = true;
            }

            // Check if asset location already exists (needed in case we need to update dependencies or dependant assets)
            IResourceLocation existingAssetLocation = customResourceLocator.Locations
                .SelectMany(kvp => kvp.Value)
                .OfType<ResourceLocationBase>()
                .FirstOrDefault(loc =>
                    loc.ProviderId == UniversalContentResourceProvider.CustomProviderId &&
                    loc.PrimaryKey == locationMetadata.Address &&
                    loc.InternalId == locationMetadata.InternalId &&
                    loc.ResourceType == locationMetadata.Type
                );

            if (existingAssetLocation == null)
            {
                updateLocator = true;
                // Create resource location for NAF Asset
                var assetLocation = new ResourceLocationBase(
                    locationMetadata.Address, // Key is the GUID (or primary label)
                    locationMetadata.InternalId, // Internal ID (URI for NAF asset)
                    UniversalContentResourceProvider.CustomProviderId, // Uses NafContentProviderId
                    locationMetadata.Type
                );

                customResourceLocator.AddLocation(locationMetadata.Address, assetLocation, locationMetadata.Labels);
            }

            // Register locator with Addressables if new locations were added
            if (customResourceLocator.Keys.Any())
            {
                if (!updateLocator)
                {
                    return;
                }

                if (!newLocator) // remove the old locator before adding it again with updated locations.
                {
                    UnityAddressables.RemoveResourceLocator(customResourceLocator);
                }
                UnityAddressables.AddResourceLocator(customResourceLocator);
            }
        }
    }
}
