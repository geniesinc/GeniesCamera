using System;
using Genies.CloudSave;
using Genies.CrashReporting;
using Newtonsoft.Json;

namespace Genies.Ugc.CustomPattern
{
    public class PatternCloudSaveJsonSerializer : ICloudSaveJsonSerializer<Pattern>
    {
        public string ToJson(Pattern data)
        {
            return JsonConvert.SerializeObject(data);
        }

        public Pattern FromJson(string json)
        {
            return JsonConvert.DeserializeObject<Pattern>(json);
        }

        public bool IsValidJson(string json)
        {
            try
            {
                JsonConvert.DeserializeObject<Pattern>(json);
                return true;
            }
            catch (Exception e)
            {
                CrashReporter.LogHandledException(e);
                return false;
            }
        }
    }
}
