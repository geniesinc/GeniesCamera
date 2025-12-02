using System;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Genies.Utilities
{
    /// <summary>
    /// Represents a valid serialized definition exposing its version and <see cref="JObject"/> token.
    /// </summary>
    public readonly struct DefinitionToken
    {
        public const string DefaultVersionKey = "Version";
        
        public readonly string Version;
        public readonly JObject Token;

        public DefinitionToken(JToken token, string versionKey = DefaultVersionKey)
        {
            // validate that the given token is a valid definition token (it must be an object and contain the string version property)
            if (token is not JObject objectToken)
            {
                throw new Exception("Cannot create a definition token from a non object json token");
            }

            if (!objectToken.TryGetValue(versionKey, out JToken versionToken))
            {
                throw new Exception($"The given json token does not contain a {versionKey} key");
            }

            if (versionToken.Type is not JTokenType.String)
            {
                throw new Exception($"The {versionKey} property in the given json token is not a string");
            }

            Version = versionToken.Value<string>();
            Token = objectToken;
        }
        
        public static DefinitionToken Parse(string definitionJson, string versionKey = DefaultVersionKey)
        {
            return new DefinitionToken(JToken.Parse(definitionJson), versionKey);
        }

        public static bool TryParse(string definitionJson, out DefinitionToken definitionToken, string versionKey = DefaultVersionKey)
        {
            try
            {
                JToken token = JToken.Parse(definitionJson);
                definitionToken = new DefinitionToken(token, versionKey);
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogError($"[{nameof(definitionToken)}] exception thrown when parsing definition json:\n{exception}");
                definitionToken = default;
                return false;
            }
        }

        public static bool TryCreate(JToken token, out DefinitionToken definitionToken, string versionKey = DefaultVersionKey)
        {
            try
            {
                definitionToken = new DefinitionToken(token, versionKey);
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogError($"[{nameof(definitionToken)}] exception thrown when creating definition token:\n{exception}");
                definitionToken = default;
                return false;
            }
        }
    }
}
