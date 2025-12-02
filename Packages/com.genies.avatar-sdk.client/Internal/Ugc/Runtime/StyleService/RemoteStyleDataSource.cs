using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Genies.CloudSave;
using Genies.CrashReporting;
using Genies.DataRepositoryFramework;
using Genies.Utilities;
using Newtonsoft.Json;

namespace Genies.Ugc
{
    public class RemoteStyleDataRepository : IDataRepository<Style>
    {
        private readonly ICloudFeatureSaveService<Style> _styleCloudSaveService;
        private readonly string _storageId;
        private readonly GeniesAppStateManager _appStateManager;
        private LocalStoredStyleStates _loadedState;
        private UniTaskCompletionSource _migrationTaskSource;

        public RemoteStyleDataRepository(ICloudFeatureSaveService<Style> styleCloudSaveService, string storageId, GeniesAppStateManager appStateManager)
        {
            _styleCloudSaveService = styleCloudSaveService;
            _storageId = storageId;
            _appStateManager = appStateManager;

            _loadedState = appStateManager.GetState<LocalStoredStyleStates>(storageId);
        }

        public async UniTask<int> GetCountAsync()
        {
            await MigrateLocalState();
            return await _styleCloudSaveService.GetCountAsync();
        }

        public async UniTask<List<string>> GetIdsAsync()
        {
            await MigrateLocalState();
            return await _styleCloudSaveService.GetIdsAsync();
        }

        public async UniTask<List<Style>> GetAllAsync()
        {
            await MigrateLocalState();
            return await _styleCloudSaveService.GetAllAsync();
        }

        public async UniTask<Style> GetByIdAsync(string recordId)
        {
            await MigrateLocalState();
            return await _styleCloudSaveService.GetByIdAsync(recordId);
        }

        public async UniTask<Style> CreateAsync(Style createdDataRecord)
        {
            return await _styleCloudSaveService.CreateAsync(createdDataRecord);
        }

        public async UniTask<List<Style>> BatchCreateAsync(List<Style> data)
        {
            return await _styleCloudSaveService.BatchCreateAsync(data);
        }

        public async UniTask<Style> UpdateAsync(Style data)
        {
            return await _styleCloudSaveService.UpdateAsync(data);
        }

        public async UniTask<List<Style>> BatchUpdateAsync(List<Style> updatedRecords)
        {
            return await _styleCloudSaveService.BatchUpdateAsync(updatedRecords);
        }

        public async UniTask<bool> DeleteAsync(string id)
        {
            return await _styleCloudSaveService.DeleteAsync(id);
        }

        public async UniTask<bool> BatchDeleteAsync(List<string> ids)
        {
            return await _styleCloudSaveService.BatchDeleteAsync(ids);
        }

        public async UniTask<bool> DeleteAllAsync()
        {
            return await _styleCloudSaveService.DeleteAllAsync();
        }

        private async UniTask MigrateLocalState()
        {

            if (_migrationTaskSource != null)
            {
                await _migrationTaskSource.Task;
                return;
            }

            _migrationTaskSource = new UniTaskCompletionSource();

            //Migrate local state
            try
            {
                if (_loadedState != null && _loadedState.Styles != null && _loadedState.Styles.Count > 0)
                {
                    var records = new List<Style>();
                    foreach (var style in _loadedState.Styles)
                    {
                        var data = JsonConvert.DeserializeObject<Style>(style.Value);
                        records.Add(data);
                    }

                    //Create records
                    await BatchCreateAsync(records);

                    //Clear disk cache
                    _appStateManager.RemovePermanentState(_storageId);
                    _loadedState.Styles.Clear();
                    _loadedState = null;
                }
            }
            catch (Exception e)
            {
                CrashReporter.LogHandledException(new CloudSaveException("Failed to migrate local state", e));
            }

            _migrationTaskSource.TrySetResult();
            _migrationTaskSource = null;
        }
    }
}
