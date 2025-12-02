using System;
using System.Collections.Generic;
using UnityEngine.Scripting;
using UnityEngine;

namespace Genies.Components.ShaderlessTools
{
    [Preserve]
    public enum ShaderFieldType
    {
        Int,
        StringList,
        Boolean,
        String,
        Guid
    }

    [Serializable][Preserve]
    public class ShaderField
    {
        public string name;
        public ShaderFieldType type;
        [SerializeReference] public object Data;

        public ShaderField()
        {
        }

        public ShaderField(string name, object data, ShaderFieldType type)
        {
            this.name = name;
            this.Data = data;
            this.type = type;
        }
    }

    [Serializable][Preserve]
    public class StringField
    {
        public string value;

        public StringField()
        {
        }

        public StringField(string value)
        {
            this.value = value;
        }
    }

    [Serializable][Preserve]
    public class StringListField
    {
        public List<string> value;

        public StringListField()
        {
        }

        public StringListField(List<string> value)
        {
            this.value = value;
        }
    }

    [Serializable][Preserve]
    public class IntField
    {
        public int value;

        public IntField()
        {
        }

        public IntField(int value)
        {
            this.value = value;
        }
    }

    [Serializable][Preserve]
    public class BoolField
    {
        public bool value;

        public BoolField()
        {
        }

        public BoolField(bool value)
        {
            this.value = value;
        }
    }

    [Preserve]
    public static class ShaderFieldExtensions
    {
        public static string GetAsString(this ShaderField field)
        {
            var data = field.Data as StringField;
            return data?.value;
        }

        public static List<string> GetAsStringList(this ShaderField field)
        {
            var data = field.Data as StringListField;

            return data?.value;
        }

        public static int GetAsInt(this ShaderField field)
        {
            var data = field.Data as IntField;
            return data?.value ?? 0;
        }

        public static bool GetAsBool(this ShaderField field)
        {
            var data = field.Data as BoolField;
            return data?.value ?? false;
        }
    }
}
