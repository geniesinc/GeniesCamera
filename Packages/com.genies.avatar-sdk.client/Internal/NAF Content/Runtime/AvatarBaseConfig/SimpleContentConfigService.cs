using System;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;

namespace Genies.Inventory
{
    /// <summary>
    /// Simplified implementation of IContentConfigService to fetch remote config using UnityWebRequests
    /// </summary>
    public class SimpleContentConfigService : IContentConfigService
    {
        /// <summary>
        /// Fetches the remote JSON config from S3.
        /// Returns parsed data or null on any failure.
        /// </summary>
        public async UniTask<RootConfig> FetchConfig(string configId)
        {
            try
            {
                using UnityWebRequest request = UnityWebRequest.Get(configId);
                var operation = request.SendWebRequest();

                while (!operation.isDone)
                {
                    await Task.Yield();
                }

                if (request.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogWarning($"[SimpleContentConfigService] Failed to fetch config: {request.error}");
                    return null;
                }

                string json = request.downloadHandler.text;

                if (string.IsNullOrEmpty(json))
                {
                    Debug.LogWarning("[SimpleContentConfigService] Config file is empty or null.");
                    return null;
                }

                var config = JsonConvert.DeserializeObject<RootConfig>(json);
                return config;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[SimpleContentConfigService] Exception fetching config: {ex.Message}");
                return null;
            }
        }
    }
}