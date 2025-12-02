using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Genies.Utilities.Editor
{
    /// <summary>
    /// Custom editor that draws buttons for methods decorated with InspectorButtonAttribute.
    /// </summary>
    [CustomEditor(typeof(MonoBehaviour), true)]
    public class InspectorButtonDrawer : UnityEditor.Editor
    {
        private MethodInfo[] _buttonMethods;

        private void OnEnable()
        {
            // Cache methods with InspectorButtonAttribute for performance
            _buttonMethods = target.GetType()
                .GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                .Where(method => method.GetCustomAttribute<InspectorButtonAttribute>() != null)
                .Where(method => method.GetParameters().Length == 0) // Only parameterless methods
                .ToArray();
        }

        public override void OnInspectorGUI()
        {
            // Draw the default inspector
            DrawDefaultInspector();

            // Only draw buttons if there are methods with the attribute
            if (_buttonMethods?.Length > 0)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Inspector Buttons", EditorStyles.boldLabel);

                foreach (var method in _buttonMethods)
                {
                    var attribute = method.GetCustomAttribute<InspectorButtonAttribute>();
                    var buttonText = string.IsNullOrEmpty(attribute.ButtonText) ? method.Name : attribute.ButtonText;

                    // Check if button should be enabled based on current mode
                    bool isEnabled = IsButtonEnabled(attribute.Mode);

                    // Disable GUI if button shouldn't be enabled in current mode
                    using (new EditorGUI.DisabledScope(!isEnabled))
                    {
                        if (GUILayout.Button(buttonText))
                        {
                            // Invoke the method on the target object
                            try
                            {
                                method.Invoke(target, null);
                            }
                            catch (Exception ex)
                            {
                                Debug.LogError($"Error invoking method {method.Name}: {ex.Message}");
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Determines if a button should be enabled based on its mode and the current Editor state.
        /// </summary>
        /// <param name="mode">The button's execution mode</param>
        /// <returns>True if the button should be enabled, false otherwise</returns>
        private static bool IsButtonEnabled(InspectorButtonAttribute.ExecutionMode mode)
        {
            return mode switch
            {
                InspectorButtonAttribute.ExecutionMode.Any => true,
                InspectorButtonAttribute.ExecutionMode.EditMode => !EditorApplication.isPlaying,
                InspectorButtonAttribute.ExecutionMode.PlayMode => EditorApplication.isPlaying,
                _ => true
            };
        }
    }
}
