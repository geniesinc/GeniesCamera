using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Genies.Customization.Framework.Navigation;

namespace Genies.Customizer.Editor.Navigation
{
    /// <summary>
    /// Custom property drawer for ChildNodeDropdown attribute
    /// Provides a dropdown in the Unity Inspector for selecting child nodes
    /// </summary>
    [CustomPropertyDrawer(typeof(ChildNodeDropdownAttribute))]
    public class ChildNodeDropdownDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            // Get the NavigationNode that owns this property
            var targetObject = property.serializedObject.targetObject as NavigationNode;
            if (targetObject == null)
            {
                EditorGUI.PropertyField(position, property, label);
                EditorGUI.EndProperty();
                return;
            }

            // Create dropdown options
            var options = new List<BaseNavigationNode>();
            var optionNames = new List<string>();

            // Add "None" option
            options.Add(null);
            optionNames.Add("None");

            // Add child nodes
            if (targetObject.childNodes != null)
            {
                foreach (var childNode in targetObject.childNodes.Where(node => node != null))
                {
                    options.Add(childNode);
                    optionNames.Add(childNode.name);
                }
            }

            // Find current selection index
            int currentIndex = 0;
            var currentValue = property.objectReferenceValue as BaseNavigationNode;
            for (int i = 0; i < options.Count; i++)
            {
                if (options[i] == currentValue)
                {
                    currentIndex = i;
                    break;
                }
            }

            // Create the dropdown
            EditorGUI.BeginChangeCheck();
            int newIndex = EditorGUI.Popup(position, label.text, currentIndex, optionNames.ToArray());
            
            if (EditorGUI.EndChangeCheck() && newIndex >= 0 && newIndex < options.Count)
            {
                property.objectReferenceValue = options[newIndex];
            }

            EditorGUI.EndProperty();
        }
    }
} 