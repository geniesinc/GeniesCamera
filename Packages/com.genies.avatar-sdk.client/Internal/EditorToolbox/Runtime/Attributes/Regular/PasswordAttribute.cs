using System;
using System.Diagnostics;
using UnityEngine;

namespace Toolbox.Core{
    /// <summary>
    /// Draws a password field.
    /// 
    /// <para>Supported types: <see cref="string"/>.</para>
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    [Conditional("UNITY_EDITOR")]
    public class PasswordAttribute : PropertyAttribute
    { }
}