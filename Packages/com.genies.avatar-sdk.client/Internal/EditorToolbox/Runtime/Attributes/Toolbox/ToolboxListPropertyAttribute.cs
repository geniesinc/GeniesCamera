using System;
using System.Diagnostics;

namespace Toolbox.Core{
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    [Conditional("UNITY_EDITOR")]
    public class ToolboxListPropertyAttribute : ToolboxPropertyAttribute
    { }
}