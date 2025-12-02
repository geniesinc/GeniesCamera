using System;
using System.Diagnostics;
using UnityEngine;

namespace Toolbox.Core{
    /// <summary>
    /// Draws the left-ordered toggle.
    /// 
    /// <para>Supported types: <see cref="bool"/>.</para>
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    [Conditional("UNITY_EDITOR")]
    public class LeftToggleAttribute : PropertyAttribute
    { }
}