using System;
using Genies.CloudSave;
using Genies.CrashReporting;
using Newtonsoft.Json;

namespace Genies.Ugc
{
    public class StyleCloudSaveJsonSerializer : ICloudSaveJsonSerializer<Style>
    {
        public string ToJson(Style data)
        {
            return JsonConvert.SerializeObject(data);
        }

        public Style FromJson(string json)
        {
            return JsonConvert.DeserializeObject<Style>(json);
        }

        public bool IsValidJson(string json)
        {
            try
            {
                JsonConvert.DeserializeObject<Style>(json);
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
