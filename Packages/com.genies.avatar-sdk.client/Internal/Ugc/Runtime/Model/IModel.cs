
namespace Genies.Ugc
{
    /// <summary>
    /// Establishes a pattern for creating model classes in the UGC system. Implementers must be reference types.
    /// This interface provides comparison and hashing methods separate from System.Object's Equals and GetHashCode
    /// to avoid disrupting behavior in hashing collections (dictionaries, hashsets, etc.) while still enabling
    /// value-based equality comparisons for model data.
    /// </summary>
    public interface IModel : ICopyable
    {
        /// <summary>
        /// Determines whether this model is equivalent to the specified object in terms of data content.
        /// This method provides value-based equality comparison without overriding System.Object.Equals.
        /// </summary>
        /// <param name="pattern">The object to compare with this model.</param>
        /// <returns>True if the models are equivalent in data content; otherwise, false.</returns>
        bool IsEquivalentTo(object pattern);
        
        /// <summary>
        /// Computes a hash code based on the model's data content.
        /// This method provides value-based hashing without overriding System.Object.GetHashCode.
        /// </summary>
        /// <returns>A hash code computed from the model's data content.</returns>
        int ComputeHash();
    }
    
    /// <summary>
    /// Generic version of <see cref="IModel"/> that provides strongly-typed equivalence comparison
    /// for models of a specific type, enhancing type safety and performance.
    /// </summary>
    /// <typeparam name="T">The specific type of model this interface applies to.</typeparam>
    public interface IModel<T> : IModel, ICopyable<T>
    {
        /// <summary>
        /// Determines whether this model is equivalent to the specified model of the same type in terms of data content.
        /// This strongly-typed version provides better performance than the object-based comparison.
        /// </summary>
        /// <param name="pattern">The model to compare with this model.</param>
        /// <returns>True if the models are equivalent in data content; otherwise, false.</returns>
        bool IsEquivalentTo(T pattern);
    }
}
