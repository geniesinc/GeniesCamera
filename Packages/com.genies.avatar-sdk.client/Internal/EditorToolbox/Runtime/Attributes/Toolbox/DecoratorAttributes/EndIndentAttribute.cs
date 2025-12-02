using System;
using System.Diagnostics;

namespace Toolbox.Core{
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
    [Conditional("UNITY_EDITOR")]
    public class EndIndentAttribute : ToolboxDecoratorAttribute
    {
        public EndIndentAttribute(int indentToSubtract = 1)
        {
            IndentToSubtract = indentToSubtract;
        }

        public int IndentToSubtract { get; private set; }
    }
}