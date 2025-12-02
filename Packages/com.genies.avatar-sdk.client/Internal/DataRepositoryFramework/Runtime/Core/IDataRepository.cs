using System.Collections.Generic;
using Cysharp.Threading.Tasks;

namespace Genies.DataRepositoryFramework
{
    /// <summary>
    /// Describes logic for implementing CRUD operations on a data store.
    /// </summary>
    /// <typeparam name="T"> The type of data being handled </typeparam>
    public interface IDataRepository<T>
    {
        /// <summary>
        /// Gets the count of the existing records
        /// </summary>
        UniTask<int> GetCountAsync();

        /// <summary>
        /// Returns the all the record ids for a specific feature type
        /// </summary>
        UniTask<List<string>> GetIdsAsync();

        /// <summary>
        /// Returns all data
        /// </summary>
        UniTask<List<T>> GetAllAsync();

        /// <summary>
        /// Gets a single record matching the record id.
        /// </summary>
        /// <param name="id"> The requested id </param>
        UniTask<T> GetByIdAsync(string id);

        /// <summary>
        /// Creates a single record
        /// </summary>
        UniTask<T> CreateAsync(T data);

        /// <summary>
        /// Batch creates a list of records
        /// </summary>
        /// <param name="data"> list of records </param>
        UniTask<List<T>> BatchCreateAsync(List<T> data);

        /// <summary>
        /// Updates a record. If no record is found it will create a new one.
        /// </summary>
        /// <param name="data"> The new data </param>
        UniTask<T> UpdateAsync(T data);

        /// <summary>
        /// Batch updates a list of records
        /// </summary>
        /// <param name="data"> list of records </param>
        UniTask<List<T>> BatchUpdateAsync(List<T> data);

        /// <summary>
        /// Deletes a single record matching the id.
        /// </summary>
        /// <param name="id"> The id of the record to delete </param>
        UniTask<bool> DeleteAsync(string id);

        /// <summary>
        /// Batch delete a group of records.
        /// </summary>
        /// <param name="ids"> The records ids to delete </param>
        UniTask<bool> BatchDeleteAsync(List<string> ids);

        /// <summary>
        /// Delete all records.
        /// </summary>
        UniTask<bool> DeleteAllAsync();
    }
}
