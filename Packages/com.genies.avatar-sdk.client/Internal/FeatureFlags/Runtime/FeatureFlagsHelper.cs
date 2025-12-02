using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Genies.FeatureFlags
{
    public class FeatureFlagsHelper
    {
        private static readonly List<string> _cachedFlags;
        private static readonly List<string> _cachedKeys;
        private static readonly Dictionary<string, string> _keyToValueMap;
        private static readonly Dictionary<string, string> _valueToKeyMap;

        static FeatureFlagsHelper()
        {
            _cachedFlags = new List<string>();
            _cachedKeys = new List<string>();
            _keyToValueMap = new Dictionary<string, string>();
            _valueToKeyMap = new Dictionary<string, string>();

            var featureFlagContainers = AppDomain.CurrentDomain.GetAssemblies()
                                                 .SelectMany(assembly => assembly.GetTypes()
                                                                                 .Where(t => t.GetCustomAttribute<FeatureFlagsContainerAttribute>() != null))
                                                 .OrderBy(t => t.GetCustomAttribute<FeatureFlagsContainerAttribute>().Order)
                                                 .ToList();

            foreach (var container in featureFlagContainers)
            {
                var fields = container.GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);

                foreach (var field in fields)
                {
                    if (field.IsLiteral && !field.IsInitOnly && field.FieldType == typeof(string))
                    {
                        var key   = field.Name;
                        var value = (string)field.GetValue(null);

                        _cachedFlags.Add(value);
                        _cachedKeys.Add(key);

                        _keyToValueMap[key] = value;
                        _valueToKeyMap[value] = key;
                    }
                }
            }
        }

        public static IReadOnlyList<string> AllFlagValues => _cachedFlags;

        public static IReadOnlyList<string> AllFlagKeys => _cachedKeys;

        public static string GetFlagValue(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                return null;
            }
            
            return _keyToValueMap.TryGetValue(key, out var value) ? value : null;
        }

        public static string GetFlagKeyFromValue(string flagValue)
        {
            if (string.IsNullOrEmpty(flagValue))
            {
                return null;
            }
            
            return _valueToKeyMap.TryGetValue(flagValue, out var key) ? key : null;
        }
        
        public static void OverrideFlags(List<string> cachedFlags, List<string> cachedValues)
        {
            _cachedFlags.Clear();
            _cachedKeys.Clear();

            foreach (var cachedFlag in cachedFlags)
            {
                _cachedFlags.Add(cachedFlag);
                _cachedKeys.Add(cachedFlag);
            }
        }
    }
}
