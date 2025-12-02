using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Genies.DataRepositoryFramework.Caching;

namespace Genies.DataRepositoryFramework
{
    /// <summary>
    /// Data repository that will save data to local disk
    /// </summary>
    /// <typeparam name="T"> Data type </typeparam>
    public class LocalDiskDataRepository<T> : IDataRepository<T>
    {
        private readonly SetId _idSetter;
        private readonly Func<T, string> _idGetter;

        public delegate void SetId(T data, string id);
        private readonly DataRepositoryDiskStorage<T> _storage;

        public LocalDiskDataRepository(string storageKey, Func<T, string> getRecordId, SetId idSetter = null)
        {
            _idSetter = idSetter;
            _idGetter = getRecordId;
            _storage = new DataRepositoryDiskStorage<T>(storageKey, getRecordId);
        }

        private async UniTask EnsureValidDataIdAsync(T data)
        {
            var id = _idGetter.Invoke(data);
            if (string.IsNullOrEmpty(id) && _idSetter != null)
            {
                var validId = await GenerateValidGuid();
                _idSetter.Invoke(data, validId);
            }
        }

        private async UniTask EnsureValidDataIdAsync(List<T> data)
        {
            await UniTask.WhenAll(data.Select(EnsureValidDataIdAsync));
        }

        private async UniTask<string> GenerateValidGuid()
        {
            var guid       = Guid.NewGuid().ToString();
            var currentIds = await GetIdsAsync();

            while (currentIds.Contains(guid))
            {
                guid = Guid.NewGuid().ToString();
            }

            return guid;
        }


        public UniTask<int> GetCountAsync()
        {
            return UniTask.FromResult(_storage.GetRecordsCount());
        }

        public UniTask<List<string>> GetIdsAsync()
        {
            return UniTask.FromResult(_storage.GetRecordsIds());
        }

        public UniTask<List<T>> GetAllAsync()
        {
            return UniTask.FromResult(_storage.GetRecords());
        }

        public UniTask<T> GetByIdAsync(string id)
        {
            var didFetch = _storage.TryGetRecord(id, out var record);

            if (!didFetch)
            {
                return default;
            }

            return UniTask.FromResult(record);
        }

        public async UniTask<T> CreateAsync(T createdDataRecord)
        {
            await EnsureValidDataIdAsync(createdDataRecord);
            _storage.CacheRecord(createdDataRecord);
            return createdDataRecord;
        }

        public async UniTask<List<T>> BatchCreateAsync(List<T> data)
        {
            await EnsureValidDataIdAsync(data);
            _storage.BatchCacheRecords(data);
            return data;
        }

        public UniTask<T> UpdateAsync(T data)
        {
            _storage.CacheRecord(data);
            return UniTask.FromResult(data);
        }

        public UniTask<List<T>> BatchUpdateAsync(List<T> updatedRecords)
        {
            _storage.BatchCacheRecords(updatedRecords);
            return UniTask.FromResult(updatedRecords);
        }

        public UniTask<bool> DeleteAsync(string id)
        {
            _storage.DeleteRecord(id);
            return UniTask.FromResult(true);
        }

        public UniTask<bool> BatchDeleteAsync(List<string> ids)
        {
            _storage.BatchDeleteRecords(ids);
            return UniTask.FromResult(true);
        }

        public UniTask<bool> DeleteAllAsync()
        {
            _storage.Clear();
            return UniTask.FromResult(true);
        }
    }
}
