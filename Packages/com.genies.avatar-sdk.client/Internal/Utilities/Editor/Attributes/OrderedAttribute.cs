using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine.Scripting;

namespace Genies.Utilities.Editor
{
    /// <summary>
    /// Use this to decorate types with specific sorting, the utility methods will
    /// help you create and get instances of those decorated types in an ordered fashion.
    /// (Lower goes first!)
    /// </summary>
    public abstract class OrderedAttribute : PreserveAttribute
    {
        public readonly int order;

        public OrderedAttribute(int order = 0) => this.order = order;

        public static Type[] GetTypes<T>() where T : OrderedAttribute
        {
            return TypeCache.GetTypesWithAttribute<T>()
                            .OrderBy(t => t.GetCustomAttribute<T>().order)
                            .ToArray();
        }

        public static Type[] GetTypes<T, TBaseType>() where T : OrderedAttribute => TypeCache.GetTypesWithAttribute<T>()
                                                                                              .Where(t => typeof(TBaseType).IsAssignableFrom(t))
                                                                                              .OrderBy(t => t.GetCustomAttribute<T>().order)
                                                                                              .ToArray();

        public static TBaseType[] GetSortedInstances<T, TBaseType>() where T : OrderedAttribute => GetTypes<T, TBaseType>()
                                                                                                 .Select(t => (TBaseType)Activator.CreateInstance(t))
                                                                                                 .Where(t => t != null)
                                                                                                 .ToArray();

        public class PriorityDelegate<T, TDelegate>
            where T : OrderedAttribute
            where TDelegate : Delegate
        {
            public T attribute;
            public TDelegate method;

            public int order => attribute.order;

            public PriorityDelegate(T attribute, TDelegate method)
            {
                this.attribute = attribute;
                this.method = method;
            }
        }
    }
}
