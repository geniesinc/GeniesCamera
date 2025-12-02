using System;
using System.Collections.Generic;
using UnityEngine;

namespace Genies.Utilities
{
    public class RuntimeCache: IRuntimeCache
    {
        private readonly Dictionary<string, CacheRecord> _records = new Dictionary<string, CacheRecord>();

        public bool RegisterRecord<T>(string id, T record, int lifetimeSeconds)
        {
            if (_records.TryGetValue(id, out CacheRecord _))
            {
                Debug.LogError($"Record already registered {id}, you should call {nameof(TryGetValidRecord)} first ");
                return false;
            }

            _records.Add(id, new CacheRecord(record, DateTime.Now.AddSeconds(lifetimeSeconds)));
            return true;
        }

        public bool TryGetValidRecord<T>(string id, out T record)
        {
            //get the result and check if the lifetime is not expired
            if (_records.TryGetValue(id, out CacheRecord currentRecord))
            {
                if (NeedsToRefreshRecord(currentRecord))
                {
                    //invalid record for usage, removed from the control
                    _records.Remove(id);
                    record = default;
                    return false;
                }

                try
                {
                    record = (T)currentRecord.RawRecord;
                    return true;
                }
                catch (Exception)
                {
                    Debug.LogError($"Failed to process Record {id} ");
                    record = default;
                    return false;
                }

            }

            record = default;
            return false;
        }

        public void ClearRecords()
        {
            _records.Clear();
        }

        private bool NeedsToRefreshRecord(CacheRecord currentRecord)
        {
            //the record control has default value, so we have to refresh for the first time in the session
            if (currentRecord.Equals(default) || currentRecord.ExpiresIn == DateTime.MinValue)
            {
                return true;
            }

            //the record saved is expired so we have to to refresh
            if (currentRecord.ExpiresIn <= DateTime.Now)
            {
                return true;
            }

            return false;
        }

        private struct CacheRecord
        {
            public DateTime ExpiresIn;
            public object RawRecord;

            public CacheRecord(object rawRecord, DateTime expiresIn)
            {
                RawRecord = rawRecord;
                ExpiresIn = expiresIn;
            }
        }
    }
}
