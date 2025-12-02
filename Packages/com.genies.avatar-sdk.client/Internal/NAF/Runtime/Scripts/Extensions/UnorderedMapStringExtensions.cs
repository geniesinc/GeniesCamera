using System.Collections.Generic;
using GnWrappers;

namespace Genies.Naf
{
    public static class UnorderedMapStringExtensions
    {
        public static UnorderedMapString AsUnorderedMapString(this Dictionary<string, string> dictionary, bool nullIfNullOrEmpty = false)
        {
            if (dictionary is null || dictionary.Count == 0)
            {
                return nullIfNullOrEmpty ? null : new UnorderedMapString();
            }

            var map = new UnorderedMapString();
            foreach ((string key, string value) in dictionary)
            {
                map.Add(key, value);
            }

            return map;
        }
    }
}