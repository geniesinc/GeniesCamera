using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;
using Genies.CrashReporting;

namespace Genies.DataRepositoryFramework.Caching
{
    /// <summary>
    /// Stores data on device disk.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class DataRepositoryDiskStorage<T>
    {
        private readonly Func<T, string> _getRecordId;
        private readonly string _storagePath;
        private readonly string _storageJsonFile;

        /// <summary>
        /// Current available records.
        /// </summary>
        private Dictionary<string, T> _records = new Dictionary<string, T>();

        public DataRepositoryDiskStorage(string storageKey, Func<T, string> getRecordId)
        {
            _getRecordId = getRecordId;
            _storagePath = Path.Combine(Application.persistentDataPath, storageKey);
            _storageJsonFile = Path.Combine(_storagePath,               "data.json");

            LoadOrCreateDiskStorage();
        }

        private void LoadOrCreateDiskStorage()
        {
            //Make sure the directory is created
            Directory.CreateDirectory(_storagePath);

            if (!File.Exists(_storageJsonFile))
            {
                _records = new Dictionary<string, T>();
                return;
            }

            try
            {
                using var streamReader = new StreamReader(_storageJsonFile);
                var       json         = streamReader.ReadToEnd();
                _records = JsonConvert.DeserializeObject<Dictionary<string, T>>(json);
            }
            catch (Exception e)
            {
                CrashReporter.LogHandledException(e);
                Clear();
            }
        }

        /// <summary>
        /// Saves the <see cref="_records"/> as a json to disk
        /// </summary>
        private void SaveRecordsToDisk()
        {
            var jsonString = JsonConvert.SerializeObject(_records, Formatting.Indented);
            File.WriteAllText(_storageJsonFile, jsonString);
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
        /// Returns all the existing records in disk. Returns null if non exists.
        /// </summary>
        public List<T> GetRecords()
        {
            return !HasRecords() ? new List<T>() : new List<T>(_records.Values);
        }

        /// <summary>
        /// Batch cache records in disk.
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
                CacheRecord(record, writeToDisk: false);
            }

            SaveRecordsToDisk();
        }

        /// <summary>
        /// Cache a new record on disk.
        /// </summary>
        /// <param name="dataRecord"> The record to cache </param>
        /// <param name="writeToDisk"> If true will write records to disk </param>
        public void CacheRecord(T dataRecord, bool writeToDisk = true)
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
            }
            else
            {
                _records.Add(id, dataRecord);
            }

            if (writeToDisk)
            {
                SaveRecordsToDisk();
            }
        }

        /// <summary>
        /// Batch deletes records from disk
        /// </summary>
        /// <param name="recordIds"> Records to delete </param>
        public void BatchDeleteRecords(List<string> recordIds)
        {
            foreach (var record in recordIds)
            {
                DeleteRecord(record, writeToDisk: false);
            }

            SaveRecordsToDisk();
        }

        /// <summary>
        /// Deletes a single record from disk.
        /// </summary>
        /// <param name="recordId"> The id of the record to delete </param>
        /// <param name="writeToDisk"> If true will write the records to disk </param>
        public void DeleteRecord(string recordId, bool writeToDisk = true)
        {
            if (!TryGetRecord(recordId, out _))
            {
                return;
            }

            _records.Remove(recordId);

            if (writeToDisk)
            {
                SaveRecordsToDisk();
            }
        }

        public bool HasRecord(string id)
        {
            return _records.ContainsKey(id);
        }

        public void Clear()
        {
            _records.Clear();
            SaveRecordsToDisk();
        }
    }
}
