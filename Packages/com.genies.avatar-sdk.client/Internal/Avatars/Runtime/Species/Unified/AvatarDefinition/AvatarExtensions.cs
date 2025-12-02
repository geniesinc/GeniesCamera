using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Genies.Avatars.Definition.DataModels;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using Genies.CrashReporting;

namespace Genies.Avatars
{
    public static class AvatarExtensions
    {
        public static JsonSerializerSettings SerializerSettings = new JsonSerializerSettings { Formatting = Formatting.Indented };
        private static string _cachedDefaultDef;

        public static string SerializeDefinition(this AvatarDefinition def)
        {
            return JsonConvert.SerializeObject(def, SerializerSettings);
        }

        /// <summary>
        /// Serialize an avatar definition into a string over a few frames
        /// </summary>
        public static async UniTask<string> SerializeAsync(this AvatarDefinition avatarDefinition)
        {
            var task = Task.Run(() =>
            {
                lock (avatarDefinition)
                {
                    try
                    {
                        return JsonConvert.SerializeObject(avatarDefinition, SerializerSettings);
                    }
                    catch (Exception e)
                    {
                        CrashReporter.LogError($"Invalid avatar definition : {e}");
                        return null;
                    }
                }
            });

            while (!task.IsCompleted)
            {
                await UniTask.DelayFrame(1);
            }

            return task.Result;
        }

        /// <summary>
        /// Deserialize a string into an avatar definition over a few frames
        /// </summary>
        public static async UniTask<AvatarDefinition> DeserializeToAvatarDefinitionAsync(this string avatarDefinitionJson)
        {
            var task = Task.Run(() =>
            {
                try
                {
                    return JsonConvert.DeserializeObject<AvatarDefinition>(avatarDefinitionJson, SerializerSettings);
                }
                catch (Exception e)
                {
                    CrashReporter.LogError($"Invalid avatar definition : {e}");
                    return null;
                }
            });

            while (!task.IsCompleted)
            {
                await UniTask.DelayFrame(1);
            }

            return task.Result;
        }

        private static Dictionary<string, string[]> defaultOutfitMap = new Dictionary<string, string[]>
        {
            {
                "male", new string[]
                {
                    "recEjFaecbT1GNNv2", // underwearBottom-0001-boxers
                    "recHDAw1RgZaMG8lt", // underwearTop-0002-tankTop
                }
            },
            {
                "female", new string[]
                {
                    "recEjFaecbT1GNNv2", // underwearBottom-0001-boxers
                    "recHDAw1RgZaMG8lt", // underwearTop-0002-tankTop
                }
            },
            {
                "unified", new string[]
                {
                    "recEjFaecbT1GNNv2", // underwearBottom-0001-boxers
                    "recHDAw1RgZaMG8lt", // underwearTop-0002-tankTop
                }
            }
        };

        public static string BinaryGenderStringFromDna(this AvatarDefinition def)
        {
            string defaultgender = "male"; // can unified just have no deform applied ?
            if (def == null || def.DNA == null)
            {
                return defaultgender;
            }

            foreach (var dna in def.DNA)
            {
                if (dna.Value == 1.0f &&
                    dna.Key == "male" || dna.Key == "female" || dna.Key == "gap")
                {
                    return dna.Key;
                }
            }

            return defaultgender;
        }

        public static string[] FirstOutfit(this AvatarDefinition def)
        {
            string gender = def.BinaryGenderStringFromDna();
            if (def == null || def.Outfits == null)
            {
                return defaultOutfitMap[gender];
            }

            if (def.Outfits.Length == 0)
            {
                return defaultOutfitMap[gender];
            }

            return def.Outfits[0];
        }

        /// <summary>
        /// Grab any gSkel values in the DNA according to the gSkelData names
        /// </summary>
        public static Dictionary<string, float> GetBodyAttributesPreset(this AvatarDefinition def, BodyAttributesConfig config)
        {
            var res = new Dictionary<string, float>();

            if (!config || def?.DNA == null)
            {
                return res;
            }

            /**
             * Old definitions will come with the "Leg/torso ratio" attribute which was renamed to avoid some parsing
             * issues. We can probably remove this after a long time (this was set on June 6th 2024)
             */
            if (def.DNA.TryGetValue("Leg/torso ratio", out float value) && !def.DNA.ContainsKey("LegTorsoRatio"))
            {
                def.DNA["LegTorsoRatio"] = value;
            }

            foreach (BodyAttributesConfig.Attribute attribute in config.Attributes)
            {
                res[attribute.name] = def.DNA.GetValueOrDefault(attribute.name, 0.0f);
            }

            return res;
        }

        public static bool TryGetAvatarFeature<T>(this AvatarDefinition definition, string featureKey, out T featureDefinition) where T : AvatarFeatureDefinition
        {
            try
            {
                if (definition.AvatarFeatures.TryGetValue(featureKey, out JToken definitionToken))
                {
                    // old avatar feature format
                    if (definitionToken.Type is JTokenType.String)
                    {
                        featureDefinition = JsonConvert.DeserializeObject<T>(definitionToken.ToObject<string>());
                        return true;
                    }

                    featureDefinition = definitionToken.ToObject<T>();
                    return true;
                }
            }
            catch (Exception exception)
            {
                Debug.LogError($"Exception thrown when trying to deserialize avatar feature (key: {featureKey})\n{exception}");
            }

            featureDefinition = default;
            return false;
        }

        public static void SetAvatarFeature(this AvatarDefinition definition, AvatarFeatureDefinition featureDefinition)
        {
            var featureKey = featureDefinition.FeatureKey;
            JToken definitionToken = JToken.FromObject(featureDefinition);

            if (definition.AvatarFeatures.TryGetValue(featureKey, out _))
            {
                definition.AvatarFeatures[featureKey] = definitionToken;
            }
            else
            {
                definition.AvatarFeatures.Add(featureKey, definitionToken);
            }
        }

        public static HashSet<string> GetOutfitAsHashSet(this AvatarDefinition def)
        {
            return new HashSet<string>(def.FirstOutfit());
        }

        public static string[] FaceVarBlendShapesFromDna(this AvatarDefinition def)
        {
            if (def == null || def.DNA == null)
            {
                return new string[0];
            };
            List<string> shapes = new List<string>();
            foreach (var dna in def.DNA)
            {
                if (dna.Key.Contains("_var") || dna.Key.Contains("faceshape"))
                {
                    shapes.Add(dna.Key);
                }
            }

            return shapes.ToArray();
        }

        public static AvatarDefinition DefaultDefinition()
        {
            return GetDefaultDefinitionForGender(UnifiedBodyVariation.Female);
        }

        public static AvatarDefinition GetDefaultDefinitionForGender(string gender)
        {
            string binaryFallback = gender;
            if (gender != "male" && gender != "female")
            {
                binaryFallback = "male";
            }

            var def = new AvatarDefinition
            {
                Species = "unified",
                Outfits = new string[1][] { defaultOutfitMap[gender] },
                DNA = new Dictionary<string, float> { { binaryFallback, 1.0f } },
                EyeMaterial = "EyeMaterialData_NewBrown",
                SkinMaterial = "SkinMaterialData_skin0007",
                HairMaterial = "HairMaterialData_RegBrownDark",
                EyebrowGear = UnifiedDefaults.DefaultEyebrowGear,
                EyelashGear = UnifiedDefaults.DefaultEyelashGear,
                EyebrowFlair = UnifiedDefaults.DefaultEyebrowTexturePreset,
                EyelashFlair = UnifiedDefaults.DefaultEyelashTexturePreset,
                EyebrowColors = UnifiedDefaults.DefaultEyebrowColors,
                EyelashColors = UnifiedDefaults.DefaultEyelashColors,
                EyebrowColorPreset = UnifiedDefaults.DefaultEyebrowColorPreset,
                EyelashColorPreset = UnifiedDefaults.DefaultEyelashColorPreset,
            };
            return def;
        }

        // gets the default definition serialized
        public static string SerializedDefaultDefinition()
        {
            if (String.IsNullOrEmpty(_cachedDefaultDef))
            {
                _cachedDefaultDef = DefaultDefinition().SerializeDefinition();
            }

            return _cachedDefaultDef;
        }

        // Returns (-1, -1) if not found
        public static (int, int) AnyOutfitContains(this AvatarDefinition def, string value)
        {
            if (def.Outfits != null)
            {
                for (int i = 0; i < def.Outfits.Length; i++)
                {
                    if (def.Outfits[i].Contains(value))
                    {
                        int j = Array.IndexOf(def.Outfits[i], value);
                        return (i, j);
                    }
                }
            }

            return (-1, -1);
        }
    }
}
