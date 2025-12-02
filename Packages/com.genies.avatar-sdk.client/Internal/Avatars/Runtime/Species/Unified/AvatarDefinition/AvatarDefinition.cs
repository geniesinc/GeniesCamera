using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using Genies.Avatars;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

/*
 *   If changes are made to this file, please advance the JsonVersion number, and
 *   make the corresponding changes to the AvatarDefinitionSchema.json
 */

namespace Genies.Avatars
{

    public class AvatarDefinition : IEquatable<AvatarDefinition>
    {
        [JsonProperty("JsonVersion", Required = Required.Always)]
        public readonly string JsonVersion = "1-1-1";

        [JsonProperty("Species", Required = Required.Always)]
        public string Species;

        [JsonProperty("Outfits")]
        public string[][] Outfits;

        [JsonProperty("DNA")]
        public Dictionary<string, float> DNA;

        [JsonProperty("HairMaterial")]
        public string HairMaterial;

        [JsonProperty("SkinMaterial")]
        public string SkinMaterial;

        [JsonProperty("EyeMaterial")]
        public string EyeMaterial;

        #region NEW PROPERTIES ADDED ON 1-1-0 VERSION

        [JsonProperty("EyebrowGear")]
        public string EyebrowGear;

        [JsonProperty("EyelashGear")]
        public string EyelashGear;

        [JsonProperty("EyebrowFlair")]
        public string EyebrowFlair;

        [JsonProperty("EyebrowColorPreset")]
        public string EyebrowColorPreset;

        [JsonProperty("EyebrowColors")]
        public string[] EyebrowColors;

        [JsonProperty("EyelashFlair")]
        public string EyelashFlair;

        [JsonProperty("EyelashColorPreset")]
        public string EyelashColorPreset;

        [JsonProperty("EyelashColors")]
        public string[] EyelashColors;

        #endregion

        #region NEW PROPERTIES ADDED ON 1-1-1 VERSION (GAP AVATAR SUPPORT)
        
        [JsonProperty("SubSpecies")]
        public string SubSpecies;
        
        #endregion

        [JsonProperty("FacialhairMaterial")]
        public string FacialhairMaterial;

        [JsonProperty("AvatarFeatures")]
        public Dictionary<string, JToken> AvatarFeatures = new();

        [JsonProperty("EmbeddedData", NullValueHandling = NullValueHandling.Ignore)]
        private JObject _embeddedData;

        public override bool Equals(object obj) => this.Equals(obj as AvatarDefinition);

        public static bool OutfitsEquals(ref string[][] fit1, ref string[][] fit2)
        {
            if (fit1 == null || fit2 == null)
            {
                return false;
            }

            if (fit1.Rank != fit2.Rank)
            {
                return false;
            }

            for (int i = 0; i < fit1.Rank; i++)
            {
                if (fit1.GetLength(i) != fit2.GetLength(i))
                {
                    return false;
                }
            }
            for (int i = 0; i < fit1.Length; i++)
            {
                if (fit1[i].Length != fit2[i].Length)
                {
                    return false;
                }

                for (int j = 0; j < fit1[i].Length; j++)
                {
                    if (!String.Equals(fit1[i][j], fit2[i][j]))
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        public bool Equals(AvatarDefinition ad)
        {
            if (ad is null)
            {
                return false;
            }

            if (DNA == null || ad.DNA == null)
            {
                return false;
            }

            if (Object.ReferenceEquals(this, ad))
            {
                return true;
            }

            if (this.GetType() != ad.GetType())
            {
                return false;
            }

            return (
                JsonVersion == ad.JsonVersion &&
                Species == ad.Species &&
                SubSpecies == ad.SubSpecies &&
                OutfitsEquals(ref Outfits, ref ad.Outfits) &&
                (DNA.Count == ad.DNA.Count && !DNA.Except(ad.DNA).Any()) &&
                String.Equals(HairMaterial, ad.HairMaterial) &&
                String.Equals(SkinMaterial, ad.SkinMaterial) &&
                String.Equals(EyeMaterial, ad.EyeMaterial) &&
                String.Equals(FacialhairMaterial, ad.FacialhairMaterial) &&
                String.Equals(EyelashFlair, ad.EyelashFlair) &&
                String.Equals(EyebrowFlair, ad.EyebrowFlair) &&
                String.Equals(EyebrowColorPreset, ad.EyebrowColorPreset) &&
                String.Equals(EyelashColorPreset, ad.EyelashColorPreset)
                );
        }

        public override int GetHashCode()
        {
            return (JsonVersion, Species, SubSpecies, Outfits, DNA, HairMaterial,
                SkinMaterial, EyeMaterial,
                FacialhairMaterial, EyelashFlair, EyebrowFlair, EyebrowColorPreset, EyelashColorPreset).GetHashCode();
        }

        // automatically serialize current embedded data into this avatar definition
        [OnSerializing]
        private void OnSerializingMethod(StreamingContext context)
        {
            lock (this)
            {
                _embeddedData = AvatarEmbeddedData.Serialize();
            }
        }

        // automatically deserialize any embedded data coming with this definition
        [OnDeserialized]
        private void OnDeserializedMethod(StreamingContext context)
        {
            AvatarEmbeddedData.Deserialize(_embeddedData);
        }

    }
}
