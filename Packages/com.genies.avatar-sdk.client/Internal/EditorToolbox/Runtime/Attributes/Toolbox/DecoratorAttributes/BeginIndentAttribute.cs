using System;
using System.Diagnostics;

namespace Toolbox.Core{
    /// <summary>
    /// Begins indentation group. Has to be closed by the <see cref="EndIndentAttribute"/>.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
    [Conditional("UNITY_EDITOR")]
    public class BeginIndentAttribute : ToolboxDecoratorAttribute
    {
        public BeginIndentAttribute(int indentToAdd = 1)
        {
            IndentToAdd = indentToAdd;
        }

        public int IndentToAdd { get; private set; }
    }
}