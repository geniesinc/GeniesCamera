using Genies.CloudSave;
using Genies.Avatars;
using Newtonsoft.Json;

namespace Genies.Avatars.Services.Flair
{
    public class FlairColorPresetCloudSaveJsonSerializer : ICloudSaveJsonSerializer<FlairColorPreset>
    {
        public string ToJson(FlairColorPreset data)
        {
            return JsonConvert.SerializeObject(data);
        }

        public FlairColorPreset FromJson(string json)
        {
            return JsonConvert.DeserializeObject<FlairColorPreset>(json);
        }

        public bool IsValidJson(string json)
        {
            return true;
        }
    }
}
