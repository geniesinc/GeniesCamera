using System;
using System.Reflection;

namespace Genies.Addressables.Editor.Utilities
{
    /// <summary>
    /// Helper class to wrap reflection
    /// </summary>
    public static class ReflectionHelper
    {
        public static T GetPropertyValue<T>(this object obj, string propertyName, Type objType = null) where T : class
        {
            if (obj == null)
            {
                throw new ArgumentNullException("obj");
            }

            objType ??= obj.GetType();
            
            var propInfo = GetPropertyInfo(objType, propertyName);
            if (propInfo == null)
            {
                throw new ArgumentOutOfRangeException("propertyName",
                    $"Couldn't find property {propertyName} in type {objType.FullName}");
            }

            return propInfo.GetValue(obj, null) as T;
        }

        public static void SetPropertyValue(this object obj, string propertyName, object val, Type objType = null)
        {
            if (obj == null)
            {
                throw new ArgumentNullException("obj");
            }

            objType ??= obj.GetType();
            
            var propInfo = GetPropertyInfo(objType, propertyName);
            if (propInfo == null)
            {
                throw new ArgumentOutOfRangeException("propertyName",
                    $"Couldn't find property {propertyName} in type {objType.FullName}");
            }

            propInfo.SetValue(obj, val, null);
        }
        
        public static T GetFieldValue<T>(this object obj, string fieldName, Type objType = null) where T : class
        {
               
            if (obj == null)
            {
                throw new ArgumentNullException("obj");
            }

            objType ??= obj.GetType();
            
            var fieldInfo = GetFieldInfo(objType, fieldName);
            if (fieldInfo == null)
            {
                throw new ArgumentOutOfRangeException("fieldName",
                    $"Couldn't find field {fieldName} in type {objType.FullName}");
            }

            return fieldInfo.GetValue(obj) as T;
        }

        public static void SetFieldValue(this object obj, string fieldName, object val, Type objType = null)
        {
            if (obj == null)
            {
                throw new ArgumentNullException("obj");
            }

            objType ??= obj.GetType();
            
            var fieldInfo = GetFieldInfo(objType, fieldName);
            if (fieldInfo == null)
            {
                throw new ArgumentOutOfRangeException("fieldName",
                    $"Couldn't find field {fieldName} in type {objType.FullName}");
            }

            fieldInfo.SetValue(obj, val);
        }
        
        private static PropertyInfo GetPropertyInfo(Type type, string propertyName)
        {
            PropertyInfo propInfo = null;
            do
            {
                propInfo = type.GetProperty(propertyName, 
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                type = type.BaseType;
            }
            while (propInfo == null && type != null);
            
            return propInfo;
        }
        
        private static FieldInfo GetFieldInfo(Type type, string propertyName)
        {
            FieldInfo fieldInfo = null;
            do
            {
                fieldInfo = type.GetField(propertyName, 
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                type = type.BaseType;
            }
            while (fieldInfo == null && type != null);
            
            return fieldInfo;
        }
    }
}