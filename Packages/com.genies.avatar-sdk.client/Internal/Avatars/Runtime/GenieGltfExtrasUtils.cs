using System;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Genies.Avatars
{
    public static class GenieGltfExtrasUtils
    {
        public const string GenieExtrasKey = "genieExtras";

        public static bool TrySerializeExtras(GenieGltfExtras extras, out JToken token)
        {
            if (extras is null)
            {
                token = null;
                return false;
            }

            try
            {
                token = GenieGltfExtras.Serialize(extras);
                return token is not null;
            }
            catch (Exception exception)
            {
                Debug.LogError($"Something went wrong while serializing Genie glTF extras:\n{exception}");
                token = null;
                return false;
            }
        }

        public static JToken CreateGltfExtrasWithGenieExtras(JToken genieExtras)
        {
            return new JObject
            {
                { GenieExtrasKey, genieExtras }
            };
        }

        public static JToken GetGenieExtrasFromGltfExtras(JToken gltfExtras)
        {
            //Did you export the genie with the {nameof(GenieExportType.Full)} export type option?
            if (gltfExtras is null)
            {
                throw new Exception($"Couldn't find the genie extras. No glTF extras were provided.");
            }

            if (gltfExtras is not JObject jObject)
            {
                throw new Exception($"Couldn't find the genie extras. The provided glTF extras is not an object.");
            }

            if (!jObject.TryGetValue(GenieExtrasKey, out JToken genieExtras))
            {
                throw new Exception($"Couldn't find the genie extras. The provided glTF extras does not contain a {GenieExtrasKey} property.");
            }

            // this shouldn't happen but we will not assume that extras are always serialized as an object, so we just return
            if (genieExtras is not JObject genieExtrasObject)
            {
                return genieExtras;
            }

            // log warnings if the loaded extras doesn't contain a version property
            if (!genieExtrasObject.TryGetValue(GenieGltfExtras.VersionKey, out JToken versionToken))
            {
                Debug.LogWarning("The loaded genie contains genie extras without a version property");
                return genieExtras;
            }

            // log warning if the loaded extras mismatch the current version
            // we don't throw here because mismatching versions doesn't imply that it cannot be deserialized into the current class
            string version = versionToken.Value<string>();
            if (version != GenieGltfExtras.CurrentVersion)
            {
                Debug.LogWarning($"The loaded genie extras version mismatches current genie extras version:\nCurrent: {GenieGltfExtras.CurrentVersion}\nLoaded: {version}");
            }

            return genieExtras;
        }

        public static Avatar BuildHumanAvatar(GameObject go, SerializableHumanDescription serializableHumanDescription)
        {
            // convert to regular human description
            HumanDescription humanDescription = SerializableHumanDescription.Convert(serializableHumanDescription);
            // the first skeleton bone must always be the root GameObject (Unity things...)
            humanDescription.skeleton[0].name = go.name;
            // build avatar
            return AvatarBuilder.BuildHumanAvatar(go, humanDescription);
        }
    }
}
