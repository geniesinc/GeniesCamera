using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace Genies.Avatars
{
    /// <summary>
    /// Manages data embedded into avatar definitions. Any service using UGC GUIDs for custom content (UGC wearables,
    /// custom hair colors, etc...) should use this class to register the custom data and use it as a fallback when not
    /// found by the API.
    /// <br/><br/>
    /// There are probably other more elegant solutions to this, but they would require more refactoring, and we are
    /// already planning to rewrite the avatar tech in standalone, so I don't think it is worth it.
    /// </summary>
    public static class AvatarEmbeddedData
    {
        public static int Count => Data.Count;
        
        private static readonly object _dataLock = new object();
        private static readonly Dictionary<string, JToken> Data = new();
        
        public static List<string> GetAllKeys()
        {
            var results = new List<string>(Data.Count);
            GetAllKeys(results);
            return results;
        }

        public static void GetAllKeys(ICollection<string> results)
        {
            foreach (string key in Data.Keys)
            {
                results.Add(key);
            }
        }
        
        public static List<string> GetAllKeys<T>()
        {
            var results = new List<string>(Data.Count);
            GetAllKeys<T>(results);
            return results;
        }

        public static void GetAllKeys<T>(ICollection<string> results)
        {
            foreach (string key in Data.Keys)
            {
                if (TryGetData<T>(key, out _))
                {
                    results.Add(key);
                }
            }
        }
        
        public static void SetData<T>(string key, T data)
        {
            if (data is null)
            {
                return;
            }

            try
            {
                JToken token = JToken.FromObject(data);
                SetData(key, token);
            }
            catch (Exception) { }
        }
        
        public static void SetData(string key, JToken data)
        {
            lock (_dataLock)
            {
                if (data is null)
                {
                    return;
                }

                Data[key] = data;
            }
        }

        public static bool TryGetData<T>(string key, out T data)
        {
            if (!Data.TryGetValue(key, out JToken token))
            {
                data = default;
                return false;
            }

            try
            {
                data = token.ToObject<T>();
                return data is not null;
            }
            catch (Exception)
            {
                data = default;
                return false;
            }
        }
        
        public static bool TryGetData(string key, out JToken data)
        {
            return Data.TryGetValue(key, out data);
        }

        public static void ClearData(string key)
        {
            lock (_dataLock)
            {
                Data.Remove(key);
            }
        }

        public static void ClearAllData()
        {
            lock (_dataLock)
            {
                Data.Clear();
            }
        }
        
        public static JObject Serialize()
        {
            Dictionary<string, JToken> dataClone;

            lock (_dataLock)
            {
                dataClone = Data.ToDictionary(entry => entry.Key, entry => entry.Value.DeepClone());
            }
            
            return dataClone.Count > 0 ? JObject.FromObject(dataClone) : null;
        }

        public static void Deserialize(JObject data)
        {
            if (data is null)
            {
                return;
            }

            try
            {
                var dataToAdd = data.ToObject<Dictionary<string, JToken>>();
                if (dataToAdd is null)
                {
                    return;
                }

                foreach ((string key, JToken token) in dataToAdd)
                {
                    SetData(key, token);
                }
            }
            catch (Exception) { }
        }
    }
}