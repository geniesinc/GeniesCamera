namespace Genies.Ugc
{
    /// <summary>
    /// Defines the contract for objects that can create deep copies of themselves.
    /// This interface enables safe copying of model objects to prevent unintended mutations
    /// when working with shared or referenced data structures.
    /// </summary>
    public interface ICopyable
    {
        /// <summary>
        /// Creates and returns a deep copy of this object as a generic object reference.
        /// The returned copy is completely independent of the original.
        /// </summary>
        /// <returns>A deep copy of this object.</returns>
        object DeepCopy();
        
        /// <summary>
        /// Performs a deep copy of this object's data into the specified destination object.
        /// This method enables copying without additional heap allocations when the destination already exists.
        /// </summary>
        /// <param name="destination">The destination object to copy data into.</param>
        void DeepCopy(object destination);
    }
    
    public interface ICopyable<T> : ICopyable
    {
        /// <summary>
        /// Creates and returns a deep copy of the definition instance. Muting the returned instance will not mute the current one.
        /// </summary>
        new T DeepCopy();
        
        /// <summary>
        /// Makes a deep copy of this instance into the destination instance, so no extra heap allocations are preformed.
        /// Muting the returned instance will not mute the current one.
        /// </summary>
        void DeepCopy(T destination);
    }
}
