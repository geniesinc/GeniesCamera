using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Genies.CrashReporting;
using UnityEngine;

namespace Genies.Utilities
{
    public static class ApiUtils
    {
        
        public static async Task PaginateRequest<T>(List<T> items, Func<List<T>, UniTask> func, int maxItemsPerPage = 50)
        {
            var pageCount = Mathf.CeilToInt((float)items.Count() / maxItemsPerPage);
            
            for (int i = 0; i < pageCount; i++)
            {
                try
                {
                    var maxItemsToRequest = items.GetRange(i * maxItemsPerPage, Mathf.Min(maxItemsPerPage, items.Count - i * maxItemsPerPage));
                    await func.Invoke(maxItemsToRequest);
                }
                catch (Exception e)
                {
                    CrashReporter.Log($"Exception on page {i}", LogSeverity.Error);
                    CrashReporter.LogHandledException(e);
                }
            }
        }
        
        public static async Task<List<K>> PaginateRequest<T, K>(List<T> items, Func<List<T>, Task<List<K>>> func, int maxItemsPerPage = 50)
        {
            var results = new List<K>();
            var pageCount = Mathf.CeilToInt((float)items.Count() / maxItemsPerPage);


            for (int i = 0; i < pageCount; i++)
            {
                try
                {
                    var maxItemsToGet = items.GetRange(i * maxItemsPerPage, Mathf.Min(maxItemsPerPage, items.Count - i * maxItemsPerPage));
                    var resultItems = await func.Invoke(maxItemsToGet);
                    results.AddRange(resultItems);
                }
                catch (Exception e)
                {
                    CrashReporter.Log($"Exception on page {i}", LogSeverity.Error);
                    CrashReporter.LogHandledException(e);
                }
            }

            return results;
        }

        //UniTask version of the same wrapper
        public static async UniTask<List<K>> PaginateRequest<T, K>(List<T> items, Func<List<T>, UniTask<List<K>>> func, int maxItemsPerPage = 50)
        {
            var results = new List<K>();
            var pageCount = Mathf.CeilToInt((float)items.Count() / maxItemsPerPage);


            for (int i = 0; i < pageCount; i++)
            {
                try
                {
                    var maxItemsToGet = items.GetRange(i * maxItemsPerPage, Mathf.Min(maxItemsPerPage, items.Count - i * maxItemsPerPage));
                    var resultItems = await func.Invoke(maxItemsToGet);
                    results.AddRange(resultItems);
                }
                catch (Exception e)
                {
                    CrashReporter.Log($"Exception on page {i}", LogSeverity.Error);
                    CrashReporter.LogHandledException(e);
                }
            }

            return results;
        }

        public static string FormatUrlToHttps(string url)
        {
            var uri = new System.UriBuilder(url)
            {
                Scheme = Uri.UriSchemeHttps,
                Port = -1
            };

            return uri.ToString();
        }
    }
}
