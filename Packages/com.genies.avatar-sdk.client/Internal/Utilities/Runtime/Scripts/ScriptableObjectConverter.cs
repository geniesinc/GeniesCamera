using System;
using System.Collections.Generic;
using System.IO;
using Genies.CrashReporting;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using Newtonsoft.Json.Bson;

namespace Genies.Utilities
{
    public sealed class ScriptableObjectConverter : JsonConverter
    {
        public List<UnityObjectRef> References;

        // Known optional component types that should not cause errors if missing
        private string[] _optionalComponentTypes =
        {
            "PlacedObjectsFeatureAsset",
            "Genies.Things",
            "Genies.Components.SDK.External.Things"
        };

        public override bool CanConvert(Type objectType)
        {
            return typeof(ScriptableObject).IsAssignableFrom(objectType);
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            if (value is ScriptableObject so)
            {
                JScriptableObject.FromObject(so, serializer).WriteTo(writer);
            }
            else
            {
                writer.WriteNull();
            }
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType is JsonToken.Null)
            {
                return null;
            }

            try
            {
                var token = new JScriptableObject(JObject.Load(reader));
                UnityObjectRef<ScriptableObject> reference = token.ToScriptableObject(objectType, serializer);
                References?.Add(reference);
                return reference.Object;
            }
            catch (Exception exception)
            {
                // Handle known optional component types gracefully
                if (IsOptionalComponentError(_optionalComponentTypes, exception.Message))
                {
                    CrashReporter.LogInternal($"[ScriptableObjectConverter] Skipping optional component: {GetCleanErrorMessage(exception.Message)}");
                }
                else
                {
                    CrashReporter.LogError($"Failed to deserialize scriptable object: {exception.Message}");
                }
                return null;
            }
        }

        /// <summary>
        /// Checks if an error message relates to a missing optional component
        /// </summary>
        /// <param name="optionalComponentTypes">Strings to search for in the error message
        /// to determine if it should be ignored</param>
        /// <param name="errorMessage">The error message to check</param>
        /// <returns>True if this is a known optional component error</returns>
        private static bool IsOptionalComponentError(string[] optionalComponentTypes, string errorMessage)
        {
            if (string.IsNullOrEmpty(errorMessage))
			{
                return false;
			}

            foreach (var componentType in optionalComponentTypes)
            {
                if (errorMessage.Contains(componentType))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Extracts a clean error message without the full stack trace.
        /// </summary>
        /// <param name="fullMessage">The full error message</param>
        /// <returns>A cleaned version of the error message</returns>
        private static string GetCleanErrorMessage(string fullMessage)
        {
            // Extract just the relevant part of the error message
            if (fullMessage.Contains("failed to resolve the type"))
            {
                int start = fullMessage.IndexOf("failed to resolve the type");
                int end = fullMessage.IndexOf(",", start);
                if (end > start)
                {
                    return fullMessage.Substring(start, end - start);
                }
            }

            return "missing optional component";
        }

        public static string Serialize(ScriptableObject asset, JsonSerializer serializer = null)
        {
            return JScriptableObject.FromObject(asset, serializer).ToString();
        }

        /// <summary>
        /// Deserializes the given json text into the given ScriptableObject type. It returns a reference to the
        /// deserialized asset that can be disposed to destroy the scriptable object and any scriptable objects created
        /// with it.
        /// </summary>
        public static UnityObjectRef<T> Deserialize<T>(string json, JsonSerializer serializer = null)
            where T : ScriptableObject
        {
            return JScriptableObject.Parse(json).ToScriptableObject<T>(serializer);
        }

        public static UnityObjectRef<T> Populate<T>(string json, T destination, JsonSerializer serializer = null)
            where T : ScriptableObject
        {
            return JScriptableObject.Parse(json).PopulateScriptableObject(destination, serializer);
        }

        public static UnityObjectRef<ScriptableObject> Deserialize(string json, JsonSerializer serializer = null)
        {
            return JScriptableObject.Parse(json).ToScriptableObject(serializer);
        }

        public static UnityObjectRef<ScriptableObject> Populate(string json, ScriptableObject destination, JsonSerializer serializer = null)
        {
            return JScriptableObject.Parse(json).PopulateScriptableObject(destination, serializer);
        }

        public static byte[] SerializeToBson(ScriptableObject asset, JsonSerializer serializer = null)
        {
            using var stream = new MemoryStream();
            SerializeToBson(asset, stream, serializer);
            return stream.ToArray();
        }

        public static void SerializeToBson(ScriptableObject asset, Stream stream, JsonSerializer serializer = null)
        {
            var token = JScriptableObject.FromObject(asset, serializer);
            using var writer = new BsonDataWriter(stream);
            (serializer ?? JsonSerializer.CreateDefault()).Serialize(writer, token);
        }

        public static UnityObjectRef<T> DeserializeFromBson<T>(byte[] bson, JsonSerializer serializer = null)
            where T : ScriptableObject
        {
            using var stream = new MemoryStream(bson);
            return DeserializeFromBson<T>(stream, serializer);
        }

        public static UnityObjectRef<T> PopulateFromBson<T>(byte[] bson, T destination, JsonSerializer serializer = null)
            where T : ScriptableObject
        {
            using var stream = new MemoryStream(bson);
            return PopulateFromBson(stream, destination, serializer);
        }

        public static UnityObjectRef<ScriptableObject> DeserializeFromBson(byte[] bson, JsonSerializer serializer = null)
        {
            using var stream = new MemoryStream(bson);
            return DeserializeFromBson(stream, serializer);
        }

        public static UnityObjectRef<ScriptableObject> PopulateFromBson(byte[] bson, ScriptableObject destination, JsonSerializer serializer = null)
        {
            using var stream = new MemoryStream(bson);
            return PopulateFromBson(stream, destination, serializer);
        }

        public static unsafe UnityObjectRef<T> DeserializeFromBson<T>(IntPtr bsonData, int dataSize, JsonSerializer serializer = null)
            where T : ScriptableObject
        {
            using var stream = new UnmanagedMemoryStream((byte*)bsonData, dataSize);
            return DeserializeFromBson<T>(stream, serializer);
        }

        public static unsafe UnityObjectRef<T> PopulateFromBson<T>(IntPtr bsonData, int dataSize, T destination, JsonSerializer serializer = null)
            where T : ScriptableObject
        {
            using var stream = new UnmanagedMemoryStream((byte*)bsonData, dataSize);
            return PopulateFromBson(stream, destination, serializer);
        }

        public static unsafe UnityObjectRef<ScriptableObject> DeserializeFromBson(IntPtr bsonData, int dataSize, JsonSerializer serializer = null)
        {
            using var stream = new UnmanagedMemoryStream((byte*)bsonData, dataSize);
            return DeserializeFromBson(stream, serializer);
        }

        public static unsafe UnityObjectRef<ScriptableObject> PopulateFromBson(IntPtr bsonData, int dataSize, ScriptableObject destination, JsonSerializer serializer = null)
        {
            using var stream = new UnmanagedMemoryStream((byte*)bsonData, dataSize);
            return PopulateFromBson(stream, destination, serializer);
        }

        public static UnityObjectRef<T> DeserializeFromBson<T>(Stream bsonStream, JsonSerializer serializer = null)
            where T : ScriptableObject
        {
            using var reader = new BsonDataReader(bsonStream);
            return JScriptableObject.Load(reader).ToScriptableObject<T>(serializer);
        }

        public static UnityObjectRef<T> PopulateFromBson<T>(Stream bsonStream, T destination, JsonSerializer serializer = null)
            where T : ScriptableObject
        {
            using var reader = new BsonDataReader(bsonStream);
            return JScriptableObject.Load(reader).PopulateScriptableObject(destination, serializer);
        }

        public static UnityObjectRef<ScriptableObject> DeserializeFromBson(Stream bsonStream, JsonSerializer serializer = null)
        {
            using var reader = new BsonDataReader(bsonStream);
            return JScriptableObject.Load(reader).ToScriptableObject(serializer);
        }
    }
}
