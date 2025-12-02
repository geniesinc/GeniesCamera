using System;
using System.Reflection;

namespace Toolbox.Editor.Drawers
{
    public class MethodValueExtractor : IValueExtractor
    {
        public bool TryGetValue(string source, object declaringObject, out object value)
        {
            value = default;
            if (string.IsNullOrEmpty(source) || declaringObject == null)
            {
                return false;
            }

            Type       type = declaringObject.GetType();
            MethodInfo info = null;

            // Loop to check each base type for the field
            while (type != null)
            {
                info = type.GetMethod(source, ReflectionUtility.allBindings | BindingFlags.FlattenHierarchy, null, CallingConventions.Any, Type.EmptyTypes, null);
                if (info != null)
                {
                    break;
                }

                type = type.BaseType;
            }

            if (info == null)
            {
                return false;
            }

            value = info.Invoke(declaringObject, null);
            return true;
        }
    }
}
