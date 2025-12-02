using System;
using Genies.CloudSave;
using Genies.CrashReporting;
using Newtonsoft.Json;

namespace Genies.Ugc.CustomSkin
{
    public class CustomSkinCloudSaveJsonSerializer : ICloudSaveJsonSerializer<SkinColorData>
    {
        public string ToJson(SkinColorData data)
        {
            return JsonConvert.SerializeObject(data);
        }

        public SkinColorData FromJson(string json)
        {
            return JsonConvert.DeserializeObject<SkinColorData>(json);
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
