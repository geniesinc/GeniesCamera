using System;
using System.Collections.Generic;
using System.Reflection;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Genies.Utilities
{
    /// <summary>
    /// Capable of serializing/deserializing any type that declares the <see cref="SerializableAsAttribute"/> as
    /// its declared generic type.
    /// </summary>
    public static class SerializerAs<T>
    {
        // reflection config for Serialize/Deserialize methods
        private const BindingFlags SerializeMethodBindingFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        private const BindingFlags DeserializeMethodBindingFlags = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
        private const string SerializeMethodName = "Serialize";
        private const string DeserializeMethodName = "Deserialize";
        private delegate JToken SerializeDelegate(object instance);
        private delegate T DeserializeDelegate(JToken token);
        
        // config for the json token
        private const string IdKey = "id";
        private const string DataKey = "data";
        
        // we have some data redundancy to ease the access by ID or type
        private static readonly Dictionary<Type, SerializeDelegate>     SerializersByType;
        private static readonly Dictionary<string, DeserializeDelegate> DeserializersById;

        static SerializerAs()
        {
            // initialize readonly redundant data structs
            DeserializersById = new Dictionary<string, DeserializeDelegate>();
            SerializersByType = new Dictionary<Type, SerializeDelegate>();
            
            // automatically register generic serializable types
            RegisterGenericSerializableTypes();
        }

        public static bool IsSerializable(T instance)
        {
            return IsSerializable(instance.GetType());
        }

        public static bool IsSerializable<TAnother>()
        {
            return IsSerializable(typeof(TAnother));
        }

        public static bool IsSerializable(Type type)
        {
            return SerializersByType.ContainsKey(type);
        }

        public static bool TrySerialize(object instance, out JToken token)
        {
            if (!SerializersByType.TryGetValue(instance.GetType(), out SerializeDelegate serialize))
            {
                token = null;
                return false;
            }

            try
            {
                token = serialize(instance);
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogError($"Failed to serialize instance {instance}:\n{exception}");
                token = null;
                return false;
            }
        }
        
        public static bool TryDeserialize(JToken token, out T instance)
        {
            instance = default;
            
            if (token is not JObject obj)
            {
                Debug.LogError("Serialized instance token is not a Json object");
                return false;
            }
            
            if (!obj.TryGetValue(IdKey, out JToken idToken) || idToken is not JValue idValue)
            {
                Debug.LogError("Serialized instance ID token no found");
                return false;
            }
            
            string id = idValue.Value<string>();
            if (string.IsNullOrEmpty(id))
            {
                Debug.LogError("Serialized instance ID is null or empty");
                return false;
            }

            if (!obj.TryGetValue(DataKey, out JToken data))
            {
                Debug.LogError("Serialized instance data token no found");
                return false;
            }
            
            if (!DeserializersById.TryGetValue(id, out DeserializeDelegate deserialize))
            {
                Debug.LogError($"Deserializer not found for ID: {id}");
                return false;
            }

            try
            {
                instance = deserialize(data);
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogError($"Failed to deserialize instance {id}:\n{exception}");
                return false;
            }
        }

        private static void RegisterGenericSerializableTypes()
        {
            // this significantly reduces the time it takes to find all generic serializable types
            List<Assembly> assemblies = GetAssembliesThatCanDeclareGenericSerializableTypes();
            foreach (Assembly assembly in assemblies)
            {
                foreach (Type type in assembly.GetTypes())
                {
                    RegisterType(type);
                }
            }
        }
        
        // this method returns assemblies that references the attribute assembly, so we can avoid iterating over all types on the application domain
        private static List<Assembly> GetAssembliesThatCanDeclareGenericSerializableTypes()
        {
            Assembly attributeAssembly = typeof(SerializableAsAttribute).Assembly;
            Assembly[] allAssemblies = AppDomain.CurrentDomain.GetAssemblies();
            var assemblies = new List<Assembly>(allAssemblies.Length + 1);
            assemblies.Add(attributeAssembly);

            foreach (Assembly assembly in allAssemblies)
            {
                foreach (AssemblyName reference in assembly.GetReferencedAssemblies())
                {
                    if (reference.FullName != attributeAssembly.FullName)
                    {
                        continue;
                    }

                    assemblies.Add(assembly);
                    break;
                }
            }

            return assemblies;
        }

        private static void RegisterType(Type type)
        {
            // look for all SerializableAs attributes in the type and register them
            IEnumerable<SerializableAsAttribute> attributes = type.GetCustomAttributes<SerializableAsAttribute>(inherit: false);
            foreach (SerializableAsAttribute attribute in attributes)
            {
                RegisterAttribute(type, attribute);
            }
        }

        private static void RegisterAttribute(Type type, SerializableAsAttribute attribute)
        {
            // the attribute AsType must match with this serializer
            if (attribute.AsType != typeof(T))
            {
                return;
            }

            if (string.IsNullOrEmpty(attribute.Id))
            {
                Debug.LogError($"Type <color=yellow>{type.FullName}</color> is declared to be serializable as <color=yellow>{typeof(T).FullName}</color> but with a null or empty ID");
                return;
            }
            
            // check if the attribute id is already in use
            if (!attribute.SerializeOnly && DeserializersById.ContainsKey(attribute.Id))
            {
                Debug.LogError($"Type <color=yellow>{type.FullName}</color> is declared to be serializable as <color=yellow>{typeof(T).FullName}</color> with an ID already in use: {attribute.Id}\nEnable the {nameof(SerializableAsAttribute.SerializeOnly)} property if you wish to have duplicated IDs for serialization");
                return;
            }
            
            // try to get the serialize method info
            SerializeDelegate serialize = CreateSerializeDelegate(type, attribute.Id);
            if (serialize is null)
            {
                Debug.LogError($"Type <color=yellow>{type.FullName}</color> is declared to be serializable as <color=yellow>{typeof(T).FullName}</color> but it does not declare a valid {SerializeMethodName} method");
                return;
            }
            
            SerializersByType.Add(type, serialize);

            if (attribute.SerializeOnly)
            {
                return;
            }

            // try to get the deserialize method info
            DeserializeDelegate deserialize = CreateDeserializeDelegate(type);
            if (deserialize is null)
            {
                Debug.LogError($"Type <color=yellow>{type.FullName}</color> is declared to be serializable as <color=yellow>{typeof(T).FullName}</color> but it does not declare a valid {DeserializeMethodName} method");
                return;
            }
            
            DeserializersById.Add(attribute.Id, deserialize);
        }

#region NastyReflection
        private static SerializeDelegate CreateSerializeDelegate(Type type, string id)
        {
            if (!TryFindMethodMatchingDelegate<Func<JToken>>(type, SerializeMethodName, SerializeMethodBindingFlags, out MethodInfo method))
            {
                return null;
            }

            return instance =>
            {
                object data = method.Invoke(instance, null);
                if (data is not JToken dataToken)
                {
                    throw new Exception($"Serialize method from {type} returned an invalid JToken");
                }

                return new JObject
                {
                    { IdKey,   id        },
                    { DataKey, dataToken },
                };
            };
        }

        private static DeserializeDelegate CreateDeserializeDelegate(Type type)
        {
            if (!TryFindMethodMatchingDelegate<DeserializeDelegate>(type, DeserializeMethodName, DeserializeMethodBindingFlags, out MethodInfo method))
            {
                return null;
            }

            try
            {
                if (method.CreateDelegate(typeof(DeserializeDelegate)) is DeserializeDelegate deserialize)
                {
                    return deserialize;
                }

                return null;
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static bool TryFindMethodMatchingDelegate<TDelegate>(Type type, string methodName, BindingFlags bindingFlags, out MethodInfo method)
            where TDelegate : Delegate
        {
            method = null;
            Type delegateType = typeof(TDelegate);
            MethodInfo delegateMethod = delegateType.GetMethod("Invoke");
            if (delegateMethod is null)
            {
                return false;
            }

            ParameterInfo[] delegateParameters = delegateMethod.GetParameters();
            Type delegateReturnType = delegateMethod.ReturnType;

            MethodInfo[] typeMethods = type.GetMethods(bindingFlags);
            foreach (MethodInfo typeMethod in typeMethods)
            {
                if (typeMethod.Name != methodName || typeMethod.ReturnType != delegateReturnType)
                {
                    continue;
                }

                ParameterInfo[] parameters = typeMethod.GetParameters();
                if (!DoParametersMatch(delegateParameters, parameters))
                {
                    continue;
                }

                method = typeMethod;
                return true;
            }

            return false;
        }

        private static bool DoParametersMatch(ParameterInfo[] parametersA, ParameterInfo[] parametersB)
        {
            if (parametersA.Length != parametersB.Length)
            {
                return false;
            }

            for (int i = 0; i < parametersA.Length; i++)
            {
                if (parametersA[i].ParameterType != parametersB[i].ParameterType)
                {
                    return false;
                }
            }
            
            return true;
        }
#endregion
    }
}
