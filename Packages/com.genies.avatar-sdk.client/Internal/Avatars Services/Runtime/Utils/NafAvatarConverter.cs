using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Genies.Utilities;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Genies.Avatars.Services
{
    public class NafAvatarDefinitionConverter : SimpleDefinitionConverter
    {
        private const string _versionKey = "JsonVersion";

        public NafAvatarDefinitionConverter() : base(
            new List<string>() { "1-0-0", "1-1-0", "1-1-1" },
            targetVersion: "2-0-0"
        )
        {
        }

        protected override UniTask<DefinitionToken> ConvertAsync(DefinitionToken definition)
        {
            Debug.Log("[NafAvatarDefinitionConverter] Starting conversion");

            var token = new JObject
            {
                [_versionKey] = TargetVersion,
            };

            var equippedAssetIds = new JArray();
            var colors = new JObject();
            var bodyAttributes = new JObject();
            var equippedTattooIds = new JObject();

            var oldToken = definition.Token;

            Debug.Log("[NafAvatarDefinitionConverter] Loaded old token");

            // Always add gen13 race container (prior to 2-0-0, this was not included, with combinable asset this is now required)
            equippedAssetIds.Add("Genie_Unified_gen13gp_Race_Container");
            Debug.Log("[NafAvatarDefinitionConverter] Added race container: Genie_Unified_gen13gp_Race_Container");

            // Outfits -> equippedAssetGuids
            if (oldToken.TryGetValue("Outfits", out var outfitsToken))
            {
                if (outfitsToken is JArray outerArray)
                {
                    Debug.Log($"[NafAvatarDefinitionConverter] Found Outfits array with {outerArray.Count} entries");

                    foreach (var innerArray in outerArray)
                    {
                        if (innerArray is JArray outfitArray)
                        {
                            Debug.Log($"[NafAvatarDefinitionConverter] Found inner outfit array with {outfitArray.Count} asset GUIDs");

                            foreach (var assetGuid in outfitArray)
                            {
                                if (assetGuid.Type == JTokenType.String && !string.IsNullOrEmpty(assetGuid.ToString()))
                                {
                                    equippedAssetIds.Add(assetGuid);
                                    Debug.Log($"[NafAvatarDefinitionConverter] Added outfit asset GUID: {assetGuid}");
                                }
                                else
                                {
                                    Debug.LogWarning($"[NafAvatarDefinitionConverter] Skipped non-string or empty outfit GUID: {assetGuid}");
                                }
                            }
                        }
                        else
                        {
                            Debug.LogWarning("[NafAvatarDefinitionConverter] Skipped non-array entry in Outfits");
                        }
                    }
                }
                else
                {
                    Debug.LogWarning("[NafAvatarDefinitionConverter] Outfits is not an array");
                }
            }
            else
            {
                Debug.LogWarning("[NafAvatarDefinitionConverter] No Outfits key found in old token");
            }

            // DNA blendshapes -> equippedAssetGuids, sliders -> bodyAttributes
            if (oldToken.TryGetValue("DNA", out var dnaToken) && dnaToken is JObject dnaObject)
            {
                Debug.Log($"[NafAvatarDefinitionConverter] Found DNA with {dnaObject.Count} entries");

                foreach (var kvp in dnaObject)
                {
                    var key = kvp.Key;
                    if (kvp.Value.Type == JTokenType.Float || kvp.Value.Type == JTokenType.Integer)
                    {
                        var value = kvp.Value.ToObject<float>();

                        if (key.Contains("male_"))
                        {
                            equippedAssetIds.Add($"BlendShapeContainer_{key}");
                            Debug.Log($"[NafAvatarDefinitionConverter] Added blendshape asset: BlendShapeContainer_{key}");
                        }
                        else
                        {
                            bodyAttributes[key] = value;
                            Debug.Log($"[NafAvatarDefinitionConverter] Added body attribute: {key} = {value}");
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"[NafAvatarDefinitionConverter] Skipped DNA key with non-numeric value: {key}");
                    }
                }
            }
            else
            {
                Debug.LogWarning("[NafAvatarDefinitionConverter] No DNA key found or DNA is not an object");
            }

            // Tattoos -> equippedTattooGuids
            if (oldToken.SelectToken("AvatarFeatures.DecoratedSkin.Tattoos") is JArray tattoosArray)
            {
                Debug.Log($"[NafAvatarDefinitionConverter] Found Tattoos array with {tattoosArray.Count} entries");

                for (int i = 0; i < tattoosArray.Count; i++)
                {
                    var tattooGuid = tattoosArray[i];
                    if (tattooGuid.Type == JTokenType.String && !string.IsNullOrEmpty(tattooGuid.ToString()))
                    {
                        equippedTattooIds[i.ToString()] = tattooGuid;
                        Debug.Log($"[NafAvatarDefinitionConverter] Added tattoo GUID at slot {i}: {tattooGuid}");
                    }
                    else
                    {
                        Debug.Log($"[NafAvatarDefinitionConverter] No tattoo GUID at slot {i}");
                    }
                }
            }
            else
            {
                Debug.Log("[NafAvatarDefinitionConverter] No Tattoos found in DecoratedSkin");
            }

            token["equippedAssetIds"] = equippedAssetIds;
            token["colors"] = colors;
            token["bodyAttributes"] = bodyAttributes;
            token["equippedTattooIds"] = equippedTattooIds;

            Debug.Log("[NafAvatarDefinitionConverter] Finished assembling new token");

            var newDef = new DefinitionToken(token, _versionKey);
            Debug.Log("[NafAvatarDefinitionConverter] Returning new DefinitionToken");
            return UniTask.FromResult(newDef);
        }
    }
}
