using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;

namespace Genies.Avatars
{
    /// <summary>
    /// Handles persisting body deform results between sessions.
    /// </summary>
    public sealed class AvatarDeformationDataSource
    {
        private static readonly string DataFilePath = Path.Combine(Application.persistentDataPath, "avatar-deformation-data.json");
        
        private readonly DeformDataMap _runtimeDeformData;
        
        public AvatarDeformationDataSource()
        {
            _runtimeDeformData = new DeformDataMap();
            LoadData();
        }

        public bool HasLatestDriverData(UtilityVector vector, UtilMeshName meshName)
        {
            string deformKey = vector.Name;
            
            return _runtimeDeformData.dataMap.ContainsKey(deformKey) &&
                   IsVersionCurrent(vector)  &&
                   _runtimeDeformData.dataMap[deformKey].meshDeformDriverDataMap.ContainsKey(meshName);
        }
        public bool IsVersionCurrent(UtilityVector vector)
        {
            // Vector doesn't exist
            if (!_runtimeDeformData.dataMap.ContainsKey(vector.Name))
            {
                return false;
            }

            // Vector has no data
            if (_runtimeDeformData.dataMap[vector.Name].meshDeformDriverDataMap.Count == 0)
            {
                return false;
            }

            // Version is outdated
            if (_runtimeDeformData.dataMap[vector.Name].version != vector.Version)
            {
                return false;
            }

            return true;
        }

        public bool IsDeformationReady(string deformKey)
        {
            return _runtimeDeformData.dataMap.TryGetValue(deformKey, out var data) && data.wasProcessed;
        }

        
        public DeformDriverData GetDriverData(string deformKey, UtilMeshName meshName)
        {
            if (!(_runtimeDeformData.dataMap.ContainsKey(deformKey) && _runtimeDeformData.dataMap[deformKey].meshDeformDriverDataMap.ContainsKey(meshName)))
            {
                return null;
            }
            
            return _runtimeDeformData.dataMap[deformKey].meshDeformDriverDataMap[meshName];
        }

        public void SetDriverData(string deformKey, UtilMeshName meshName, DeformDriverData data)
        {
            if (!_runtimeDeformData.dataMap.ContainsKey(deformKey))
            {
                _runtimeDeformData.dataMap.Add(deformKey, new DeformData());
            }

            var meshDeformDriverDataMap = _runtimeDeformData.dataMap[deformKey].meshDeformDriverDataMap;
            if (!meshDeformDriverDataMap.ContainsKey(meshName))
            {
                meshDeformDriverDataMap.Add(meshName, data);
            }
            else
            {
                meshDeformDriverDataMap[meshName] = data;
            }
        }

        public void MarkDeformKeyProcessed(string deformKey, bool wasProcessed)
        {
            if (!_runtimeDeformData.dataMap.ContainsKey(deformKey))
            {
                Debug.LogError($"[{nameof(AvatarDeformationDataSource)}] can't set deform key processed if it has no deform data associated with it");
                return;
            }

            _runtimeDeformData.dataMap[deformKey].wasProcessed = wasProcessed;
        }

        public void SetVersion(string deformKey, string version)
        {
            if (!_runtimeDeformData.dataMap.ContainsKey(deformKey))
            {
                Debug.LogError($"[{nameof(AvatarDeformationDataSource)}] Deform key {deformKey} not found. Aborting.");
                return;
            }

            _runtimeDeformData.dataMap[deformKey].version = version;
        }

        public string GetVersion(string deformKey)
        {
            if (!_runtimeDeformData.dataMap.ContainsKey(deformKey))
            {
                Debug.LogError($"[{nameof(AvatarDeformationDataSource)}] Deform key {deformKey} not found.");
                return null;
            }

            return _runtimeDeformData.dataMap[deformKey].version;
        }

        public bool HasDeformData(string deformKey)
        {
            return _runtimeDeformData.dataMap.ContainsKey(deformKey);
        }
        
        public void LoadData()
        {
            if (!File.Exists(DataFilePath))
            {
                return;
            }

            try
            {
                string serializedData = File.ReadAllText(DataFilePath);
                JsonConvert.PopulateObject(serializedData, _runtimeDeformData);
            }
            catch (Exception exception)
            {
                Debug.LogError($"[{nameof(AvatarDeformationDataSource)}] failed to load cached deform data\n{exception}");
            }
        }

        public void PersistData()
        {
            try
            {
                string serializedData = JsonConvert.SerializeObject(_runtimeDeformData);
                File.WriteAllText(DataFilePath, serializedData);
            }
            catch (Exception exception)
            {
                Debug.LogError($"[{nameof(AvatarDeformationDataSource)}] failed to save deform data\n{exception}");
            }

        }

        [Serializable]
        private class DeformDataMap
        {
            public Dictionary<string, DeformData> dataMap = new Dictionary<string, DeformData>();
        }
        
        [Serializable]
        private class DeformData
        {
            public Dictionary<UtilMeshName, DeformDriverData> meshDeformDriverDataMap = new Dictionary<UtilMeshName, DeformDriverData>();
            public bool wasProcessed = false;
            public string version = null;
        }
    }
}
