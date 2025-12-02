using System.Collections.Generic;

namespace Genies.Ugc
{
    /// <summary>
    /// Provides utility methods for working with model objects in the UGC system.
    /// This static class includes methods for computing hash codes, comparing collections,
    /// and performing common operations on model data structures.
    /// </summary>
    public static class ModelUtils
    {
        /// <summary>
        /// Computes a combined hash code for a collection of model objects.
        /// This method provides a convenient way to hash multiple models together.
        /// </summary>
        /// <param name="collection">The array of models to compute a combined hash for.</param>
        /// <returns>A combined hash code representing all models in the collection.</returns>
        public static int ComputeModelsCombinedHash(params IModel[] collection)
        {
            return collection.ComputeModelsCollectionHash();
        }
        
        /// <summary>
        /// Computes a hash code for a collection of model objects using a consistent algorithm.
        /// This extension method provides efficient hash computation for collections of models.
        /// </summary>
        /// <param name="collection">The collection of models to compute a hash for.</param>
        /// <returns>A hash code representing the entire collection, or 0 if the collection is null or empty.</returns>
        public static int ComputeModelsCollectionHash(this IEnumerable<IModel> collection)
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

            int hashCode = enumerator.Current?.ComputeHash() ?? 17;
        
            unchecked
            {
                while (enumerator.MoveNext())
                {
                    hashCode = (hashCode * 397) ^ (enumerator.Current?.ComputeHash() ?? 17);
                }
            }
        
            return hashCode;
        }
        
        /// <summary>
        /// Whether the given collection is null or empty.
        /// </summary>
        public static bool IsNullOrEmpty<T>(ICollection<T> collection)
            => collection is null || collection.Count == 0;
        
        /// <summary>
        /// Special string comparison method that also returns true when comparing null with an empty string.
        /// </summary>
        public static bool AreEqual(string left, string right)
            => left == right || string.IsNullOrEmpty(left) && string.IsNullOrEmpty(right);
    }
}
