using System;
using System.Collections.Generic;
using Genies.CrashReporting;

namespace Genies.Utilities
{
    public static class DictionaryUtils
    {
        /// <summary>
        /// Equivalent method of ToDictionary with key/value null and invalid handling as well as duplicate key handling
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <typeparam name="TKey">The type of elements of the dictionary key</typeparam>
        /// <typeparam name="TValue">The type of elements of the dictionary value</typeparam>
        /// <param name="source">The source data</param>
        /// <param name="keySelector">The key selector</param>
        /// <param name="valueSelector">Optional: a value selector</param>
        /// <param name="isValidKey">Optional: a key validator</param>
        /// <param name="isValidValue">Optional: a value validator</param>
        /// <returns>A resultant dictionary</returns>
        public static Dictionary<TKey, TValue> ToDictionaryGraceful<TSource, TKey, TValue>(
            IEnumerable<TSource> source,
            Func<TSource, TKey> keySelector,
            Func<TSource, TValue> valueSelector = null,
            Func<TKey, bool> isValidKey = null,
            Func<TValue, bool> isValidValue = null)
        {
            Dictionary<TKey, TValue> dictionary = new Dictionary<TKey, TValue>();

            foreach (TSource item in source)
            {
                TKey key = keySelector(item);
                TValue value = valueSelector != null ? valueSelector(item) : default;

                // Handle null keys
                if (key == null)
                {
                    Logger("Skipping item with null key.");
                    continue;
                }

                // Handle null values
                if (value == null)
                {
                    Logger($"Value for key {key} is null.");
                    continue;
                }

                // Handle duplicate keys
                if (dictionary.ContainsKey(key))
                {
                    Logger($"Duplicate key found: {key}. Skipping.");
                    continue;
                }

                // Handle key validation
                if (isValidKey != null && !isValidKey(key))
                {
                    Logger($"Invalid key: {key}. Skipping.");
                    continue;
                }

                // Handle value validation
                if (isValidValue != null && !isValidValue(value))
                {
                    Logger($"Invalid value for key {key}: {value}. Skipping.");
                    continue;
                }

                dictionary.Add(key, value);
            }

            return dictionary;
        }

        private static void Logger(string logMessage)
        {
            CrashReporter.Log($"[DictionaryUtils] {logMessage}", LogSeverity.Warning);
        }
    }
}
