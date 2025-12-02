using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.ResourceManagement.ResourceLocations;

namespace Genies.Addressables.CustomResourceLocation
{
    public sealed class CustomResourceLocator : IResourceLocator
    {
        public string LocatorId => "CustomResourceLocator"; // Unique identifier for the locator
        private readonly Dictionary<object, IList<IResourceLocation>> _locations = new();
        private readonly Dictionary<object, string[]> _locationLabels = new();

        public IEnumerable<object> Keys => _locations.Keys;
        
#if  UNITY_6000_0_OR_NEWER
        public IEnumerable<IResourceLocation> AllLocations { get; }
#endif
        
        public Dictionary<object, IList<IResourceLocation>> Locations => _locations;

        public bool Locate(object key, Type type, out IList<IResourceLocation> locations)
        {
            if (_locations.TryGetValue(key, out var allLocations))
            {
                locations = new List<IResourceLocation>();
                foreach (IResourceLocation location in allLocations)
                {
                    if (location.ResourceType == type)
                    {
                        locations.Add(location);
                    }
                }

                if (locations.Count > 0)
                {
                    return true;
                }

                // If no exact match found, check for assignable types
                foreach (IResourceLocation location in allLocations)
                {
                    if (type == null)
                    {
                        locations.Add(location);
                        continue;
                    }

                    if (type.IsAssignableFrom(location.ResourceType))
                    {
                        locations.Add(location);
                    }
                }

                return locations.Count > 0;
            }

            // Check if key is a label
            foreach (KeyValuePair<object, string[]> kvp in _locationLabels)
            {
                locations = new List<IResourceLocation>();
                if (kvp.Value.Contains(key.ToString()) && _locations.TryGetValue(kvp.Key, out IList<IResourceLocation> labelLocations))
                {
                    foreach (IResourceLocation location in labelLocations)
                    {
                        if (location.ResourceType == type)
                        {
                            locations.Add(location);
                        }
                    }

                    if (locations.Count > 0)
                    {
                        return true;
                    }

                    // If no exact match found, check for assignable types
                    foreach (IResourceLocation location in labelLocations)
                    {
                        if (type == null)
                        {
                            locations.Add(location);
                            continue;
                        }

                        if (type.IsAssignableFrom(location.ResourceType))
                        {
                            locations.Add(location);
                        }
                    }

                    return locations.Count > 0;
                }
            }

            locations = null;
            return false;
        }

        public void AddLocation(object key, IResourceLocation location, string[] labels = null)
        {
            if (!_locations.TryGetValue(key, out IList<IResourceLocation> list))
            {
                list = new List<IResourceLocation>();
                _locations[key] = list;
            }

            if (labels != null)
            {
                _locationLabels[key] = labels;
            }

            if (list.ToList().Exists(loc => loc.ResourceType == location.ResourceType))
            {
                return; // Avoid adding duplicates
            }

            list.Add(location);
        }
    }
}
