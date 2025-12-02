using System.Collections;
using UnityEngine;

namespace Genies.Utilities
{
    /// <summary>
    /// Some utility method to hash collections.
    /// </summary>
    public static class HashingUtils
    {
        public static int GetCombinedHashCode(params object[] collection)
        {
            return collection.GetCollectionHashCode();
        }

        public static int GetCombinedHashCode(this IEnumerable collection)
        {
            return collection.GetCollectionHashCode();
        }

        private static int GetCollectionHashCode(this IEnumerable collection)
        {
            if (collection is null)
            {
                return 0;
            }

            var enumerator = collection.GetEnumerator();
            if (!enumerator.MoveNext())
            {
                return 0;
            }

            int hashCode = enumerator.Current?.GetHashCode() ?? 17;

            unchecked
            {
                while (enumerator.MoveNext())
                {
                    var current = enumerator.Current;

                    //Due to floating point precision when unity serializes/deserializes colors, we should use their
                    //html representation when calculating their hashcode so that its consistent.
                    //Ex:
                    //When using UnityColorConverter and serializing a color to json then deserializing it, the hashcode will be different.
                    if (current is Color asColor)
                    {
                        var htmlColor = ColorUtility.ToHtmlStringRGBA(asColor);
                        hashCode = (hashCode * 397) ^ (htmlColor?.GetHashCode() ?? 17);
                        continue;
                    }

                    hashCode = (hashCode * 397) ^ (enumerator.Current?.GetHashCode() ?? 17);
                }

            }

            return hashCode;
        }
    }
}
