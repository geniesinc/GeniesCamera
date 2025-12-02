using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;

namespace Genies.Inventory
{
    /// <summary>
    /// Robust implementation of IContentConfigService to fetch remote config using .net HttpClient in separate thread with retries and caching.
    /// </summary>
    public class ContentConfigService : IContentConfigService
    {
        private const int MaxRetries = 10;
        private readonly HttpClient _httpClient = new HttpClient();
        private readonly object _configLock = new object();
        private readonly Dictionary<string, RootConfig> _uriToConfigCache = new Dictionary<string, RootConfig>();


        public UniTask<RootConfig> FetchConfig(string configId)
        {
            return FetchRemoteConfigWithRetry(configId, MaxRetries);
        }
        
        public async UniTask<RootConfig> FetchRemoteConfigWithRetry(string configUri, int retryAttempts = 1)
        {
            // Return cached value if available (thread-safe check)
            lock (_configLock)
            {
                if (_uriToConfigCache.ContainsKey(configUri))
                {
                    return _uriToConfigCache[configUri];
                }
            }

            RunDownloadOnThreadPool(configUri, retryAttempts);

            // Use thread-safe access in the WaitUntil condition
            await UniTask.WaitUntil(() =>
            {
                lock (_configLock)
                {
                    return _uriToConfigCache != null && _uriToConfigCache.ContainsKey(configUri);
                }
            });

            lock (_configLock)
            {
                if (_uriToConfigCache.TryGetValue(configUri, out var config))
                {
                    if (config == null)
                    {
                        Debug.LogWarning($"[ContentConfigService] Could not load config {configUri} due to internet connection issue.");
                        return null;
                    }
                    return config;
                }
                return null;
            }
        }

        private void RunDownloadOnThreadPool(string configUri, int retryAttempts = 1)
        {
            //Running operation on the thread pool
            //since creating new threads is expensive
            ThreadPool.QueueUserWorkItem(FetchRemoteConfig, new object[] {configUri, retryAttempts});
        }

        private async void FetchRemoteConfig(object state)
        {
            var args = (object[]) state;
            var configUri = Convert.ToString(args[0]);
            var retryAttempts = Convert.ToInt32(args[1]);
            var json = string.Empty;

            if (Uri.IsWellFormedUriString(configUri, UriKind.Absolute) &&
                (configUri.StartsWith("http://") || configUri.StartsWith("https://")))
            {
                for (var i = 0; i < retryAttempts; i++)
                {
                    try
                    {
                        HttpResponseMessage response = await _httpClient.GetAsync(configUri);
                        if (response.IsSuccessStatusCode)
                        {
                            json = await response.Content.ReadAsStringAsync();
                        }

                        if (string.IsNullOrEmpty(json))
                        {
                            continue; // next attempt
                        }

                        var config = SafeParse(json);
                        if (config == null)
                        {
                            continue; // next attempt
                        }

                        lock (_configLock)
                        {
                            _uriToConfigCache[configUri] = config;
                        }
                        return;
                    }
                    catch (Exception e)
                    {
                        Debug.LogWarning($"[ContentConfigService] Failed to fetch content config: {e.Message}");
                        if (retryAttempts > 0)
                        {
                            Debug.Log($"[ContentConfigService] Retrying ({i + 1}/{retryAttempts})...");
                            await Task.Delay(2000);
                        }
                    }
                }
            }

            lock (_configLock)
            {
                // Store placeholder entry to mark error
                _uriToConfigCache[configUri] = null;
            }

            Debug.LogWarning($"[ContentConfigService] Failed to load content config after {retryAttempts} attempts.");
        }

        private RootConfig SafeParse(string json)
        {
            try
            {
                return string.IsNullOrWhiteSpace(json) ? null : JsonConvert.DeserializeObject<RootConfig>(json);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[ContentConfigService] Json parse error: {ex.Message}");
                return null;
            }
        }
    }
}