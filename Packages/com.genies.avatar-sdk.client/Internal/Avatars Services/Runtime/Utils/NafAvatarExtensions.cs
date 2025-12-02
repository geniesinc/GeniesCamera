using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Genies.Avatars.Definition.DataModels;
using Genies.CrashReporting;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Genies.Avatars.Services
{
    public static class NafAvatarExtensions
    {
        public static JsonSerializerSettings SerializerSettings = new JsonSerializerSettings { Formatting = Formatting.Indented };
        private static string _cachedDefaultDef;

        public static string SerializeDefinition(this Naf.AvatarDefinition def)
        {
            return JsonConvert.SerializeObject(def, SerializerSettings);
        }

        /// <summary>
        /// Serialize an avatar definition into a string over a few frames
        /// </summary>
        public static async UniTask<string> SerializeAsync(this Naf.AvatarDefinition avatarDefinition)
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
        public static async UniTask<Naf.AvatarDefinition> DeserializeToAvatarDefinitionAsync(this string avatarDefinitionJson)
        {
            var task = Task.Run(() =>
            {
                try
                {
                    return JsonConvert.DeserializeObject<Naf.AvatarDefinition>(avatarDefinitionJson, SerializerSettings);
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

        public static string BinaryGenderStringFromDna(this Naf.AvatarDefinition def)
        {
            return def.equippedAssetIds.Contains("BlendShapeContainer_body_female") ? "female" : "male";
        }

        public static string[] FirstOutfit(this Naf.AvatarDefinition def)
        {
            // Current NAF doesnt really have a concept of outfits so just return the equipped GUIDs
            return def.equippedAssetIds.ToArray();
        }

        public static void SetAvatarFeature(this Naf.AvatarDefinition definition, AvatarFeatureDefinition featureDefinition)
        {
            var featureKey = featureDefinition.FeatureKey;
            JToken definitionToken = JToken.FromObject(featureDefinition);

            /*
            if (definition.AvatarFeatures.TryGetValue(featureKey, out _))
            {
                definition.AvatarFeatures[featureKey] = definitionToken;
            }
            else
            {
                definition.AvatarFeatures.Add(featureKey, definitionToken);
            }
            */
        }

        public static HashSet<string> GetOutfitAsHashSet(this Naf.AvatarDefinition def)
        {
            return new HashSet<string>(def.FirstOutfit());
        }

        public static string[] FaceVarBlendShapesFromDna(this Naf.AvatarDefinition def)
        {
            List<string> shapes = new List<string>();
            /*
            if (def == null || def.DNA == null)
                return new string[0];
            ;

            foreach (var dna in def.DNA)
            {
                if (dna.Key.Contains("_var") || dna.Key.Contains("faceshape"))
                    shapes.Add(dna.Key);
            }
            */

            return shapes.ToArray();
        }

        public static Naf.AvatarDefinition DefaultDefinition()
        {
            return GetDefaultDefinitionForGender(UnifiedBodyVariation.Female);
        }

        public static Naf.AvatarDefinition GetDefaultDefinitionForGender(string gender)
        {
            string binaryFallback = gender;
            if (gender != "male" && gender != "female")
            {
                binaryFallback = "male";
            }

            string defaultDefinitionString =
                "{\"JsonVersion\":\"2-0-0\",\"equippedAssetIds\":[\"Static/Genie_Unified_gen13gp_Race_Container\",\"Static/BlendShapeContainer_body_male\",\"Static/SkinMaterialData_skin0000\"],\"colors\":{},\"bodyAttributes\":{\"WeightLowerTorso\":0.436695457,\"Waist\":0.272225857,\"Belly\":0.50475204,\"WeightHeadNeck\":0.565346,\"ShoulderSize\":0.147455692,\"WeightArms\":0.718472958,\"HipSize\":0.328939557,\"WeightLegs\":0.6805649,\"WeightUpperTorso\":0.294911385},\"equippedTattooIds\":{}}";

            var def = JsonConvert.DeserializeObject<Naf.AvatarDefinition>(defaultDefinitionString);
            /*
            var def = new AvatarDefinition
            {
                Species = "unified",
                Outfits = new string[1][] { defaultOutfitMap[gender] },
                DNA = new Dictionary<string, float> { { binaryFallback, 1.0f }, },
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
            */
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
    }
}
