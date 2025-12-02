using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Genies.Utilities
{
    /// <summary>
    /// <see cref="JObject"/> implementation for <see cref="ScriptableObject"/> serialization. It exposes properties for
    /// the ScriptableObject's name and type, and allows to deserialize into scriptable object references that will
    /// automatically destroy any created nested scriptable objects. It can also populate an existing scriptable object.
    /// It relies on the Unity Json converters package to be able to serialize Unity types (Vector3, Quaternion, etc...).
    /// </summary>
    public sealed class JScriptableObject : JObject
    {
        public const string NamePropertyKey = "__scriptableObject__name";
        public const string TypePropertyKey = "__scriptableObject__type";
        private static readonly Dictionary<string, Type> _typeCache = new();

        /// <summary>
        /// The name of the scriptable object.
        /// </summary>
        public string Name
        {
            get => TryGetValue(NamePropertyKey, out JToken token) ? token.Value<string>() : null;
            set => Add(NamePropertyKey, value);
        }

        /// <summary>
        /// The type of the scriptable object.
        /// </summary>
        public new Type Type
        {
            get
            {
                if (!TryGetValue(TypePropertyKey, out JToken token))
                {
                    return null;
                }

                string assemblyQualifiedName = token.Value<string>();
                var type = ResolveType(assemblyQualifiedName);
                if (type is null)
                {
                    throw new InvalidOperationException($"[{nameof(JScriptableObject)}] failed to resolve the type of the serialized ScriptableObject: {assemblyQualifiedName}");
                }

                return type;
            }

            private set => Add(TypePropertyKey, value.AssemblyQualifiedName);
        }

        public JScriptableObject()
            : base() { }
        public JScriptableObject(JObject obj)
            : base(obj) { }

        /// <summary>
        /// Resolves a type by its assembly-qualified name
        /// </summary>
        private static Type ResolveType(string assemblyQualifiedName)
        {
            if (string.IsNullOrEmpty(assemblyQualifiedName))
            {
                return null;
            }

            // Check cache first
            if (_typeCache.TryGetValue(assemblyQualifiedName, out var cachedType))
            {
                return cachedType;
            }

            Type type = null;

            // Try standard Type.GetType() for mscorlib / already loaded assemblies
            type = Type.GetType(assemblyQualifiedName);
            if (type != null)
            {
                _typeCache[assemblyQualifiedName] = type;
                return type;
            }

            // Parse assembly-qualified name
            int commaIndex = assemblyQualifiedName.IndexOf(',');
            string fullTypeName = commaIndex > 0
                ? assemblyQualifiedName.Substring(0, commaIndex).Trim()
                : assemblyQualifiedName;

            string assemblyName = null;
            if (commaIndex > 0)
            {
                int nextComma = assemblyQualifiedName.IndexOf(',', commaIndex + 1);
                assemblyName = nextComma > 0
                    ? assemblyQualifiedName.Substring(commaIndex + 1, nextComma - commaIndex - 1).Trim()
                    : assemblyQualifiedName.Substring(commaIndex + 1).Trim();
            }

            if (!string.IsNullOrEmpty(assemblyName) && !string.IsNullOrEmpty(fullTypeName))
            {
                try
                {
                    var assembly = Assembly.Load(assemblyName);
                    if (assembly != null)
                    {
                        type = assembly.GetType($"{assemblyName}.{fullTypeName}", throwOnError: false);
                    }
                }
                catch
                {
                    // Ignore assembly load/reflection errors
                }
            }

            // Cache result (even null to avoid repeated failed lookups)
            _typeCache[assemblyQualifiedName] = type;

            return type;
        }


        public static JScriptableObject FromObject(ScriptableObject asset, JsonSerializer serializer = null)
        {
            using var customSerializer = new CustomSerializer(serializer, null);
            Type type = asset.GetType();
            FieldInfo[] fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            var token = new JScriptableObject();
            token.Name = asset.name;
            token.Type = type;

            foreach (FieldInfo field in fields)
            {
                if (!IsSerializableField(field, customSerializer.Serializer))
                {
                    continue;
                }

                object value = field.GetValue(asset);
                if (value is null)
                {
                    continue;
                }

                var valueToken = JToken.FromObject(value, customSerializer.Serializer);
                token.Add(field.Name, valueToken);
            }

            return token;
        }

        public new static JScriptableObject Parse(string json)
        {
            return Parse(json, null);
        }

        public new static JScriptableObject Parse(string json, JsonLoadSettings settings)
        {
            return new JScriptableObject(JObject.Parse(json, settings));
        }

        public new static JScriptableObject Load(JsonReader reader)
        {
            return Load(reader, null);
        }

        public new static JScriptableObject Load(JsonReader reader, JsonLoadSettings settings)
        {
            return new JScriptableObject(JObject.Load(reader, settings));
        }

        public UnityObjectRef<T> ToScriptableObject<T>(JsonSerializer serializer = null)
            where T : ScriptableObject
        {
            Type type = ValidateTargetDeserializationType(typeof(T));

            // instantiate the ScriptableObject, deserialize the data into it and create a new ref including its generated dependencies
            T destination = ScriptableObject.CreateInstance(type) as T;
            if (!destination)
            {
                throw new InvalidOperationException($"[{nameof(JScriptableObject)}] failed to create a ScriptableObject instance of type: {type}");
            }

            destination.name = Name;
            var dependencies = new List<UnityObjectRef>();
            Populate(destination, dependencies, serializer);

            return new UnityObjectRef<T>(destination, dependencies);
        }

        public UnityObjectRef<ScriptableObject> ToScriptableObject(JsonSerializer serializer = null)
        {
            Type serializedType = Type;
            if (serializedType is null)
            {
                throw new InvalidOperationException($"[{nameof(JScriptableObject)}] the serialized ScriptableObject doesn't have type information");
            }

            return ToScriptableObject(serializedType, serializer);
        }

        /**
         * This method is useful when you want to deserialize a ScriptableObject into a type that is not known at compile time.
         */
        public UnityObjectRef<ScriptableObject> ToScriptableObject(Type type, JsonSerializer serializer = null)
        {
            type = ValidateTargetDeserializationType(type);

            // instantiate the ScriptableObject, deserialize the data into it and create a new ref including its generated dependencies
            var destination = ScriptableObject.CreateInstance(type);
            if (!destination)
            {
                throw new InvalidOperationException($"[{nameof(JScriptableObject)}] failed to create a ScriptableObject instance of type: {type}");
            }

            destination.name = Name;
            var dependencies = new List<UnityObjectRef>();
            Populate(destination, dependencies, serializer);

            return new UnityObjectRef<ScriptableObject>(destination, dependencies);
        }

        public UnityObjectRef<T> PopulateScriptableObject<T>(T destination, JsonSerializer serializer = null)
            where T : ScriptableObject
        {
            Type type = ValidateTargetDeserializationType(typeof(T));
            if (type != typeof(T))
            {
                throw new InvalidOperationException($"[{nameof(JScriptableObject)}] trying to populate a ScriptableObject with a different type than the one specified in the JSON:\nType: {type}\nDestination Type: {typeof(T)}");
            }

            var dependencies = new List<UnityObjectRef>();
            Populate(destination, dependencies, serializer);

            /**
             * Since this SO was given then we will assume that the caller will handle its life cycle, so we will set the
             * disposal behaviour to not destroy the object when the reference is no longer used. Any deeper scriptable
             * object references that may have been instantiated will be destroyed.
             */
            return new UnityObjectRef<T>(destination, dependencies, UnityObjectRef.DisposalBehaviour.DontDestroy);
        }

        public UnityObjectRef<ScriptableObject> PopulateScriptableObject(ScriptableObject destination, JsonSerializer serializer = null)
        {
            Type type = ValidateTargetDeserializationType(destination.GetType());
            if (type != destination.GetType())
            {
                throw new InvalidOperationException($"[{nameof(JScriptableObject)}] trying to populate a ScriptableObject with a different type than the one specified in the JSON:\nType: {type}\nDestination Type: {destination.GetType()}");
            }

            var dependencies = new List<UnityObjectRef>();
            Populate(destination, dependencies, serializer);

            return new UnityObjectRef<ScriptableObject>(destination, dependencies, UnityObjectRef.DisposalBehaviour.DontDestroy);
        }

        private void Populate(ScriptableObject destination, List<UnityObjectRef> dependencies, JsonSerializer serializer)
        {
            using var customSerializer = new CustomSerializer(serializer, dependencies);
            Type type = destination.GetType();
            FieldInfo[] fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            foreach (FieldInfo field in fields)
            {
                if (IsSerializableField(field, customSerializer.Serializer) && TryGetValue(field.Name, out JToken token))
                {
                    DeserializeField(field, token, destination, dependencies, customSerializer.Serializer);
                }
            }
        }

        private Type ValidateTargetDeserializationType(Type type)
        {
            if (type is null)
            {
                throw new InvalidOperationException($"[{nameof(JScriptableObject)}] trying to deserialize into an unknown type");
            }

            if (!typeof(ScriptableObject).IsAssignableFrom(type))
            {
                throw new InvalidOperationException($"[{nameof(JScriptableObject)}] trying to deserialize from a type that is not a ScriptableObject: {type}");
            }

            // all serialized scriptable objects should come with type, but just in case fallback to the given type
            Type serializedType = Type;
            if (serializedType is null)
            {
                return type;
            }

            if (!type.IsAssignableFrom(serializedType))
            {
                throw new InvalidOperationException($"[{nameof(JScriptableObject)}] trying to deserialize into a type that is not equal or base than the one specified in the JSON:\nType: {type}\nJson Type:{serializedType}");
            }

            return serializedType;
        }

        private static void DeserializeField(FieldInfo field, JToken token, object destination, List<UnityObjectRef> dependencies, JsonSerializer serializer)
        {
            // for any field that is not a ScriptableObject with just do normal deserialization
            if (!typeof(ScriptableObject).IsAssignableFrom(field.FieldType))
            {
                object value = token.ToObject(field.FieldType, serializer);
                field.SetValue(destination, value);
                return;
            }

            try
            {
                // if the field is a ScriptableObject, and it's a valid JObject token then deserialize the ScriptableObject
                if (token is not JObject objToken)
                {
                    return;
                }

                var soToken = new JScriptableObject(objToken);
                UnityObjectRef<ScriptableObject> valueRef;

                // if the destination already references an existing ScriptableObject then populate it
                if (field.GetValue(destination) is ScriptableObject valueDestination)
                {
                    valueRef = soToken.PopulateScriptableObject(valueDestination, serializer);
                }
                else
                {
                    valueRef = soToken.ToScriptableObject(field.FieldType, serializer);
                }

                field.SetValue(destination, valueRef.Object);
                dependencies.Add(valueRef);
            }
            catch (Exception exception)
            {
                Debug.LogError($"[{nameof(JScriptableObject)}] failed to deserialize ScriptableObject field {field.Name} of type {field.FieldType}: {exception.Message}");
            }
        }

        private static bool IsSerializableField(FieldInfo field, JsonSerializer serializer)
        {
            if (field.IsInitOnly)
            {
                return false;
            }

            if (!field.IsPublic && !field.IsDefined(typeof(SerializeField)))
            {
                return false;
            }

            if (field.IsDefined(typeof(NonSerializedAttribute)))
            {
                return false;
            }

            if (field.FieldType.IsSerializable)
            {
                return true;
            }

            foreach (JsonConverter converter in serializer.Converters)
            {
                if (converter.CanConvert(field.FieldType))
                {
                    return true;
                }
            }

            return false;
        }

        private sealed class CustomSerializer : IDisposable
        {
            public JsonSerializer Serializer { get; }

            private readonly ScriptableObjectConverter _converter;
            private readonly List<UnityObjectRef>      _prevReferences;
            private readonly bool                      _removeConverterOnDispose;

            public CustomSerializer(JsonSerializer serializer, List<UnityObjectRef> dependencies)
            {
                if (serializer is null)
                {
                    Serializer = JsonSerializer.CreateDefault();
                    _converter = new ScriptableObjectConverter { References = dependencies };
                    Serializer.Converters.Add(_converter);
                    _prevReferences = null;
                    _removeConverterOnDispose = false;
                    return;
                }

                Serializer = serializer;
                _removeConverterOnDispose = true;

                foreach (JsonConverter converter in serializer.Converters)
                {
                    if (converter is not ScriptableObjectConverter soConverter)
                    {
                        continue;
                    }

                    _removeConverterOnDispose = false;
                    _converter = soConverter;
                    _prevReferences = soConverter.References;
                    soConverter.References = dependencies;
                    break;
                }

                if (_converter is null)
                {
                    _converter = new ScriptableObjectConverter { References = dependencies };
                    Serializer.Converters.Add(_converter);
                }
            }

            public void Dispose()
            {
                if (_removeConverterOnDispose)
                {
                    Serializer.Converters.Remove(_converter);
                }
                else
                {
                    _converter.References = _prevReferences;
                }
            }
        }
    }
}
