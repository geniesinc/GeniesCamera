using System;
using System.Diagnostics;

namespace Toolbox.Core{
    /// <summary>
    /// Use this attribute to close previously created <see cref="BeginHorizontalAttribute"/>.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    [Conditional("UNITY_EDITOR")]
    public class EndHorizontalAttribute : ToolboxDecoratorAttribute
    { }
}