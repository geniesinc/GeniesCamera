using System;
using System.Collections.Generic;
using Genies.CrashReporting;

namespace Genies.DataRepositoryFramework.Caching
{
    /// <summary>
    /// In memory cache for cloud records, used to avoid redundant API calls.
    ///
    /// Note: If a user is using 2 different devices at the same time they might get out of sync/won't be showing real time updates across devices.
    /// this is a niche case and it's ok since this is not a disk cache.
    /// </summary>
    /// <typeparam name="T"> The data type for the record </typeparam>
    public class DataRepositoryMemoryCache<T>
    {
        private readonly Func<T, string> _getRecordId;

        /// <summary>
        /// Current available records.
        /// </summary>
        private readonly Dictionary<string, T> _records = new Dictionary<string, T>();

        public DataRepositoryMemoryCache(Func<T, string> getRecordId)
        {
            _getRecordId = getRecordId;
        }

        /// <summary>
        /// Gets the existing records count
        /// </summary>
        public int GetRecordsCount()
        {
            return _records.Count;
        }

        /// <summary>
        /// Returns true if any records have been stored.
        /// </summary>
        public bool HasRecords()
        {
            return _records.Count > 0;
        }

        /// <summary>
        /// Returns all record ids.
        /// </summary>
        public List<string> GetRecordsIds()
        {
            return !HasRecords() ? new List<string>() : new List<string>(_records.Keys);
        }

        /// <summary>
        /// Returns true if a record exist and outputs the record.
        /// </summary>
        /// <param name="recordId"> The id of the record to get </param>
        /// <param name="dataRecord"> The returned record </param>
        /// <returns> True if it was found </returns>
        public bool TryGetRecord(string recordId, out T dataRecord)
        {
            return _records.TryGetValue(recordId, out dataRecord);
        }

        /// <summary>
        /// Returns all the existing records in memory. Returns null if non exists.
        /// </summary>
        public List<T> GetRecords()
        {
            return !HasRecords() ? new List<T>() : new List<T>(_records.Values);
        }

        /// <summary>
        /// Batch cache records in memory.
        /// </summary>
        /// <param name="records"> Records to create </param>
        public void BatchCacheRecords(List<T> records)
        {
            if (records == null || records.Count == 0)
            {
                return;
            }

            foreach (var record in records)
            {
                CacheRecord(record);
            }
        }

        /// <summary>
        /// Cache a new record in memory.
        /// </summary>
        /// <param name="dataRecord"> The record to cache </param>
        public void CacheRecord(T dataRecord)
        {
            if (_getRecordId == null)
            {
                CrashReporter.LogHandledException(new DataRepositoryException("Invalid record id getter"));
                return;
            }

            var id = _getRecordId(dataRecord);

            if (TryGetRecord(id, out _))
            {
                _records[id] = dataRecord;
                return;
            }

            _records.Add(id, dataRecord);
        }

        /// <summary>
        /// Batch deletes records from memory cache
        /// </summary>
        /// <param name="recordIds"> Records to delete </param>
        public void BatchDeleteRecords(List<string> recordIds)
        {
            foreach (var record in recordIds)
            {
                DeleteRecord(record);
            }
        }

        /// <summary>
        /// Deletes a single record from memory cache.
        /// </summary>
        /// <param name="recordId"> The id of the record to delete </param>
        public void DeleteRecord(string recordId)
        {
            if (!TryGetRecord(recordId, out _))
            {
                return;
            }

            _records.Remove(recordId);
        }

        public bool HasRecord(string id)
        {
            return _records.ContainsKey(id);
        }

        public void Clear()
        {
            _records.Clear();
        }
    }
}
