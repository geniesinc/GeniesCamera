using System;

namespace Genies.Utilities
{
    /// <summary>
    /// Makes a class type available to the <see cref="SerializerAs{T}"/> static class, so it can be serialized and
    /// deserialized as the specified <see cref="AsType"/>. The class type containing this attribute must implement
    /// the Serialize and Deserialize methods.
    /// <br/><br/>
    /// The Serialize and Deserialize methods can have any access modifiers and must have the following signature:
    /// <code>
    /// JToken Serialize();
    /// static AsType Serialize(JToken token);
    /// </code>
    /// Enable <see cref="SerializeOnly"/> if you wish to have multiple types that can be serialized as the same type
    /// with the same id (there must be at least one class declared with <see cref="SerializeOnly"/> disabled, which
    /// will act as the deserializer). You don't need to declare a Deserialize method for <see cref="SerializeOnly"/>
    /// types.
    /// </summary>
    [AttributeUsage(validOn: AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public sealed class SerializableAsAttribute : Attribute
    {
        public readonly Type   AsType;
        public readonly string Id;
        public readonly bool   SerializeOnly;

        public SerializableAsAttribute(Type asType, string id, bool serializeOnly = false)
        {
            AsType = asType;
            Id = id;
            SerializeOnly = serializeOnly;
        }
    }
}
