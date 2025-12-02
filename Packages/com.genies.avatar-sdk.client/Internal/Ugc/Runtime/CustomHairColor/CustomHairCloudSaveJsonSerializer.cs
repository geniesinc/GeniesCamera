using System;
using Genies.CloudSave;
using Genies.CrashReporting;
using Newtonsoft.Json;

namespace Genies.Ugc.CustomHair
{
    public class CustomHairCloudSaveJsonSerializer : ICloudSaveJsonSerializer<CustomHairColorData>
    {
        public string ToJson(CustomHairColorData data)
        {
            return JsonConvert.SerializeObject(data);
        }

        public CustomHairColorData FromJson(string json)
        {
            return JsonConvert.DeserializeObject<CustomHairColorData>(json);
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
