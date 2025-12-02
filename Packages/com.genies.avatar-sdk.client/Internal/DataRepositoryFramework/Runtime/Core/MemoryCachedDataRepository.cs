using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Genies.DataRepositoryFramework.Caching;
using static Genies.CrashReporting.CrashReporter;

namespace Genies.DataRepositoryFramework
{
    /// <summary>
    /// Decorated data repository that caches the data in memory to avoid redundant calls to data stores.
    /// </summary>
    public class MemoryCachedDataRepository<T> : IDataRepository<T>
    {
        protected IDataRepository<T> _dataSource;
        private readonly DataRepositoryMemoryCache<T> _memoryCache;

        public MemoryCachedDataRepository(IDataRepository<T> dataSource, Func<T, string> getRecordId)
        {
            _dataSource = dataSource;
            _memoryCache = new DataRepositoryMemoryCache<T>(getRecordId);
        }

        /// <summary>
        /// Gets the records count
        /// </summary>
        /// <returns></returns>
        public async UniTask<int> GetCountAsync()
        {
            if (_memoryCache.HasRecords())
            {
                return _memoryCache.GetRecordsCount();
            }

            var recordIds = await GetIdsAsync();
            return recordIds.Count;
        }

        public async UniTask<List<string>> GetIdsAsync()
        {
            try
            {
                //Return from memory if records exist
                if (_memoryCache.HasRecords())
                {
                    return _memoryCache.GetRecordsIds();
                }

                //If no cached records, fetch records
                var records = await _dataSource.GetAllAsync();

                //Cache records
                _memoryCache.BatchCacheRecords(records);

                return _memoryCache.GetRecordsIds();
            }
            catch (Exception e)
            {
                LogHandledException(new DataRepositoryException(e.Message, e));
            }

            return new List<string>();
        }

        /// <inheritdoc />
        public async UniTask<List<T>> GetAllAsync()
        {
            try
            {
                //Return from memory if they exist
                if (_memoryCache.HasRecords())
                {
                    return _memoryCache.GetRecords();
                }

                //Fetch records
                var records = await _dataSource.GetAllAsync();

                //Cache records
                _memoryCache.BatchCacheRecords(records);

                return records;
            }
            catch (Exception e)
            {
                LogHandledException(new DataRepositoryException(e.Message, e));
            }

            return new List<T>();
        }

        public async UniTask<T> GetByIdAsync(string id)
        {
            try
            {
                //Return from memory if they exist
                if (_memoryCache.TryGetRecord(id, out var record))
                {
                    return record;
                }

                //Fetch from data store otherwise
                var fetchedRecord = await _dataSource.GetByIdAsync(id);

                //Cache record
                _memoryCache.CacheRecord(fetchedRecord);

                return fetchedRecord;
            }
            catch (Exception e)
            {
                LogHandledException(new DataRepositoryException(e.Message, e));
            }

            return default;
        }

        public async UniTask<T> CreateAsync(T createdDataRecord)
        {
            try
            {
                //Create the record
                var data = await _dataSource.CreateAsync(createdDataRecord);

                //Cache the record
                _memoryCache.CacheRecord(data);

                return data;
            }
            catch (Exception e)
            {
                LogHandledException(new DataRepositoryException(e.Message, e));
            }

            return default;
        }

        public async UniTask<List<T>> BatchCreateAsync(List<T> recordsData)
        {
            try
            {
                //Create the record
                var data = await _dataSource.BatchCreateAsync(recordsData);

                //Cache the record
                _memoryCache.BatchCacheRecords(data);

                return data;
            }
            catch (Exception e)
            {
                LogHandledException(new DataRepositoryException(e.Message, e));
            }

            return default;
        }

        public async UniTask<T> UpdateAsync(T updatedDataRecord)
        {
            try
            {
                //Update the record
                var updatedData = await _dataSource.UpdateAsync(updatedDataRecord);

                //Cache the record
                _memoryCache.CacheRecord(updatedData);

                return updatedData;
            }
            catch (Exception e)
            {
                LogHandledException(new DataRepositoryException(e.Message, e));
            }

            return default;
        }

        public async UniTask<List<T>> BatchUpdateAsync(List<T> updatedRecords)
        {
            try
            {
                //Update the records
                var updatedData = await _dataSource.BatchUpdateAsync(updatedRecords);

                //Cache the record
                _memoryCache.BatchCacheRecords(updatedData);

                return updatedData;
            }
            catch (Exception e)
            {
                LogHandledException(new DataRepositoryException(e.Message, e));
            }

            return default;
        }

        public async UniTask<bool> DeleteAsync(string id)
        {
            try
            {
                //Delete the record
                await _dataSource.DeleteAsync(id);

                //Delete Cache record
                _memoryCache.DeleteRecord(id);

                return true;
            }
            catch (Exception e)
            {
                LogHandledException(new DataRepositoryException(e.Message, e));
            }

            return false;
        }

        public async UniTask<bool> BatchDeleteAsync(List<string> ids)
        {
            try
            {
                //Delete the records
                await _dataSource.BatchDeleteAsync(ids);

                //Delete Cache records
                _memoryCache.BatchDeleteRecords(ids);

                return true;
            }
            catch (Exception e)
            {
                LogHandledException(new DataRepositoryException(e.Message, e));
            }

            return false;
        }

        public async UniTask<bool> DeleteAllAsync()
        {
            return await BatchDeleteAsync(await GetIdsAsync());
        }

        public void ClearCache()
        {
            _memoryCache.Clear();
        }
    }
}
