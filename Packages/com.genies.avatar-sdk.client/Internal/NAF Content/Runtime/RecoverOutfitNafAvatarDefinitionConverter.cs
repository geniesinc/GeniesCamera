using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Genies.Naf.Content
{
    /// <summary>
    /// Temporary JsonConverter that attempts to recover equipped asset IDs from legacy avatar definitions
    /// (pre-2.0.0) when deserializing to Genies.Naf.AvatarDefinition.
    /// Not a full schema converter; only patches "equippedAssetIds" from "Outfits" when possible.
    /// </summary>
    public sealed class RecoverOutfitNafAvatarDefinitionConverter : JsonConverter<Genies.Naf.AvatarDefinition>
    {
        public override Genies.Naf.AvatarDefinition ReadJson(
            JsonReader reader,
            Type objectType,
            Genies.Naf.AvatarDefinition existingValue,
            bool hasExistingValue,
            JsonSerializer serializer)
        {
            if (reader == null)
            {
                throw new ArgumentNullException(nameof(reader));
            }

            // If the token isn't an object, let default handling try (or return default)
            if (reader.TokenType == JsonToken.Null)
            {
                return null;
            }

            if (reader.TokenType != JsonToken.StartObject)
            {
                // Fall back to default deserialization for unexpected shapes
                return serializer.Deserialize<Genies.Naf.AvatarDefinition>(reader);
            }

            // Load into JObject for inspection/mutation
            var jObj = JObject.Load(reader);

            // Only attempt recovery if "equippedAssetIds" is missing
            if (jObj["equippedAssetIds"] == null)
            {
                // Case-insensitive lookups for compatibility
                if (jObj.TryGetValue("Outfits", StringComparison.OrdinalIgnoreCase, out var outfitsTok)
                    && outfitsTok is JArray outfitsArr
                    && outfitsArr.Count > 0)
                {
                    // Two common legacy shapes:
                    // 1) Outfits: [ [guid1, guid2, ...], ... ]
                    // 2) Outfits: [ { equippedAssetIds: [guid1, guid2, ...], ... }, ... ]
                    JToken recovered = null;

                    var first = outfitsArr[0];

                    if (first is JArray firstOutfitArray)
                    {
                        recovered = firstOutfitArray.DeepClone();
                    }
                    else if (first is JObject firstOutfitObj &&
                             firstOutfitObj.TryGetValue("equippedAssetIds", StringComparison.OrdinalIgnoreCase, out var eidsTok) &&
                             eidsTok is JArray eidsArr)
                    {
                        recovered = eidsArr.DeepClone();
                    }

                    if (recovered != null)
                    {
                        jObj["equippedAssetIds"] = recovered;
                    }
                }
            }

            // Populate into the target instance using the same serializer to avoid re-entry on the root converter.
            var target = hasExistingValue && existingValue != null
                ? existingValue
                : (Genies.Naf.AvatarDefinition)Activator.CreateInstance(typeof(Genies.Naf.AvatarDefinition));

            using (var subReader = jObj.CreateReader())
            {
                serializer.Populate(subReader, target);
            }

            return target;
        }

        // Read-only converter: prevent writes
        public override bool CanWrite => false;

        public override void WriteJson(JsonWriter writer, Genies.Naf.AvatarDefinition value, JsonSerializer serializer)
        {
            throw new NotSupportedException($"{nameof(RecoverOutfitNafAvatarDefinitionConverter)} is read-only.");
        }
    }
}
