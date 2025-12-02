using System;
using System.Reflection;

namespace Toolbox.Editor.Drawers
{
    public class PropertyValueExtractor : IValueExtractor
    {
        public bool TryGetValue(string source, object declaringObject, out object value)
        {
            value = default;
            if (string.IsNullOrEmpty(source))
            {
                return false;
            }

            Type         type = declaringObject.GetType();
            PropertyInfo info = null;

            // Loop to check each base type for the field
            while (type != null)
            {
                info = type.GetProperty(source, ReflectionUtility.allBindings | BindingFlags.FlattenHierarchy);
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

            value = info.GetValue(declaringObject);
            return true;
        }
    }
}
