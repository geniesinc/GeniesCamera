namespace Genies.Utilities
{
    public interface IRuntimeCache
    {
        /// <summary>
        ///  Register a new record in the cache
        /// </summary>
        /// <param name="id">unique key to save the record</param>
        /// <param name="record">the object itself for saving in the runtime cache</param>
        /// <param name="lifetimeSeconds"> the lifetime of a valid cache</param>
        /// <typeparam name="T">type of the record to save</typeparam>
        /// <returns></returns>
        bool RegisterRecord<T>(string id, T record, int lifetimeSeconds);

        /// <summary>
        /// If wwe have valid record live in the lifetime criteria, we will return
        /// </summary>
        /// <param name="id"></param>
        /// <param name="record"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        bool TryGetValidRecord<T>(string id, out T record);

        /// <summary>
        ///  Consumers can update any type of data so the cache saved will be dirty
        ///  to avoid that, we can call this method to clear the invalid data
        /// </summary>
        /// <returns></returns>
        void ClearRecords();
    }
}
