using System;
using System.Diagnostics;

namespace Toolbox.Core{
    /// <summary>
    /// Hides serialized field.
    /// 
    /// <para>Supported types: all.</para>
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    [Conditional("UNITY_EDITOR")]
    public class HideAttribute : ToolboxConditionAttribute
    { }
}