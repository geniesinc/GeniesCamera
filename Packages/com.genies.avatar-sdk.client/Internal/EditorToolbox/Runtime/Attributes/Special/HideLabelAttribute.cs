using System;
using System.Diagnostics;

namespace Toolbox.Core{
    /// <summary>
    /// Hides property label.
    /// 
    /// <para>Supported types: all.</para>
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    [Conditional("UNITY_EDITOR")]
    public class HideLabelAttribute : NewLabelAttribute
    {
        public HideLabelAttribute() : base(string.Empty)
        { }
    }
}