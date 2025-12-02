using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Genies.DataRepositoryFramework;
using Genies.CrashReporting;
using Genies.Utilities;
using Newtonsoft.Json;

namespace Genies.Ugc
{
    /// <summary>
    /// Memory cached local state styles data repository
    /// </summary>
    public class DiskStyleDataRepository : MemoryCachedDataRepository<Style>
    {
        private DiskStyleDataRepository(IDataRepository<Style> dataSource) : base(dataSource, style => style.Id)
        {
        }

        public DiskStyleDataRepository(string storageId, GeniesAppStateManager appStateManager) : base(new LocalStyleDataSource(storageId, appStateManager), style => style.Id)
        {
        }


        /// <summary>
        /// Local state styles repository
        /// </summary>
        private class LocalStyleDataSource : IDataRepository<Style>
        {
            private readonly string _storageId;
            private readonly GeniesAppStateManager _appStateManager;
            private readonly LocalStoredStyleStates _loadedState;

            public LocalStyleDataSource(string storageId, GeniesAppStateManager appStateManager)
            {
                _storageId = storageId;
                _appStateManager = appStateManager;
                _loadedState = appStateManager.GetState<LocalStoredStyleStates>(storageId);

                if (_loadedState is null)
                {
                    _loadedState = new LocalStoredStyleStates();
                    appStateManager.SetPermanentState(storageId, _loadedState);
                }
            }

            public UniTask<int> GetCountAsync()
            {
                return UniTask.FromResult(_loadedState.Styles.Count);
            }

            public UniTask<List<string>> GetIdsAsync()
            {
                return UniTask.FromResult(_loadedState.Styles.Keys.Select(k => k.ToString()).ToList());
            }

            public UniTask<List<Style>> GetAllAsync()
            {
                if (_loadedState.Styles == null || _loadedState.Styles.Count == 0)
                {
                    return UniTask.FromResult(new List<Style>());
                }

                var records = _loadedState.Styles.Select(
                                                         kvp => JsonConvert.DeserializeObject<Style>(kvp.Value)
                                                        )
                                          .ToList();
                return UniTask.FromResult(records);
            }

            public UniTask<Style> GetByIdAsync(string recordId)
            {
                try
                {
                    if (_loadedState.Styles.TryGetValue(recordId, out var json))
                    {
                        var styleEntry = JsonConvert.DeserializeObject<Style>(json);
                        return UniTask.FromResult(styleEntry);
                    }
                }
                catch (Exception e)
                {
                    CrashReporter.LogHandledException(e);
                }

                return default;
            }

            public UniTask<Style> CreateAsync(Style createdDataRecord)
            {
                try
                {
                    var json = JsonConvert.SerializeObject(createdDataRecord);
                    if (_loadedState.Styles.TryGetValue(createdDataRecord.Id, out _))
                    {
                        _loadedState.Styles[createdDataRecord.Id] = json;
                        SyncStates();
                        return UniTask.FromResult(createdDataRecord);
                    }

                    _loadedState.Styles.Add(createdDataRecord.Id, json);
                    SyncStates();
                    return UniTask.FromResult(createdDataRecord);
                }
                catch (Exception e)
                {
                    CrashReporter.LogHandledException(e);
                }

                return UniTask.FromResult<Style>(null);
            }

            public async UniTask<List<Style>> BatchCreateAsync(List<Style> data)
            {
                try
                {
                    var tasks = data.Select(CreateAsync);
                    var createdData = await UniTask.WhenAll(tasks);
                    SyncStates();
                    return createdData.ToList();
                }
                catch (Exception e)
                {
                    CrashReporter.LogHandledException(e);
                    return null;
                }
            }

            public UniTask<Style> UpdateAsync(Style data)
            {
                try
                {
                    var json = JsonConvert.SerializeObject(data);
                    var id   = data.Id;
                    if (_loadedState.Styles.TryGetValue(id, out _))
                    {
                        _loadedState.Styles[id] = json;
                        return UniTask.FromResult(data);
                    }

                    _loadedState.Styles.Add(id, json);
                    SyncStates();
                    return UniTask.FromResult(data);
                }
                catch (Exception e)
                {
                    CrashReporter.LogHandledException(e);
                }

                return new UniTask<Style>(null);
            }

            public async UniTask<List<Style>> BatchUpdateAsync(List<Style> updatedRecords)
            {
                try
                {
                    var tasks = updatedRecords.Select(UpdateAsync);
                    var results = await UniTask.WhenAll(tasks);
                    return results.ToList();
                }
                catch (Exception e)
                {
                    CrashReporter.LogHandledException(e);
                    return null;
                }
            }

            public UniTask<bool> DeleteAsync(string id)
            {
                if (!_loadedState.Styles.TryGetValue(id, out _))
                {
                    return UniTask.FromResult(false);
                }

                _loadedState.Styles.Remove(id);
                SyncStates();
                return UniTask.FromResult(true);
            }

            public async UniTask<bool> BatchDeleteAsync(List<string> ids)
            {
                try
                {
                    var tasks = ids.Select(DeleteAsync);
                    await UniTask.WhenAll(tasks);
                    SyncStates();
                    return true;
                }
                catch (Exception e)
                {
                    CrashReporter.LogHandledException(e);
                    return false;
                }
            }

            public async UniTask<bool> DeleteAllAsync()
            {
                return await BatchDeleteAsync(await GetIdsAsync());
            }


            private void SyncStates()
            {
                _appStateManager.SetPermanentState(_storageId, _loadedState);
            }
        }
    }
}
