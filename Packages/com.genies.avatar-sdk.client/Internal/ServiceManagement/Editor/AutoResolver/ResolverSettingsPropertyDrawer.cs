using System;
using UnityEditor;
using UnityEngine;

namespace Genies.ServiceManagement.Editor
{
    /// <summary>
    /// Custom drawer for the Auto resolver settings entry
    /// </summary>
    [CustomPropertyDrawer(typeof(ResolverSettings))]
    public class ResolverSettingsPropertyDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var totalHeight = EditorGUIUtility.singleLineHeight; // For the header

            SerializedProperty resolverInstanceProperty = property.FindPropertyRelative("ResolverInstance");
            var resolverInstance = resolverInstanceProperty.managedReferenceValue;
            Type resolverType = resolverInstance?.GetType();

            if (AutoResolver.IsInstallerAvailable(resolverType))
            {
                totalHeight += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            }

            if (AutoResolver.IsInitializerAvailable(resolverType))
            {
                totalHeight += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            }

            // Consider resolverInstanceProperty height here:
            totalHeight += EditorGUI.GetPropertyHeight(resolverInstanceProperty);

            if (resolverInstanceProperty.hasVisibleChildren)
            {
                totalHeight += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing; // For the "Installation Settings" label

                SerializedProperty childProperty = resolverInstanceProperty.Copy();
                var depth = childProperty.depth;
                var enterChildren = true;

                while (childProperty.NextVisible(enterChildren) && childProperty.depth > depth)
                {
                    totalHeight += EditorGUI.GetPropertyHeight(childProperty, true) + EditorGUIUtility.standardVerticalSpacing;
                    enterChildren = false;
                }
            }

            return totalHeight;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            SerializedProperty isInstallerEnabledProperty = property.FindPropertyRelative("IsInstallerEnabled");
            SerializedProperty isInitializerEnabledProperty = property.FindPropertyRelative("IsInitializerEnabled");
            SerializedProperty resolverInstanceProperty = property.FindPropertyRelative("ResolverInstance");

            var resolverInstance = resolverInstanceProperty.managedReferenceValue;
            Type resolverType = resolverInstance?.GetType();

            // Draw a header with the centered type name

            // Draw a line under the header
            EditorGUI.DrawRect(new Rect(position.x, position.y, position.width, 1), Color.gray);
            position.y += 2; // Increment Y by 2 to create a small gap after the line

            var typeName = resolverType != null ? resolverType.Name : "Undefined Type";
            var centeredStyle = new GUIStyle(EditorStyles.boldLabel) { alignment = TextAnchor.MiddleCenter };
            EditorGUI.LabelField(new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight), typeName, centeredStyle);
            position.y += EditorGUIUtility.singleLineHeight;


            if (AutoResolver.IsInstallerAvailable(resolverType))
            {
                EditorGUI.PropertyField(new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight), isInstallerEnabledProperty);
                position.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            }

            if (AutoResolver.IsInitializerAvailable(resolverType))
            {
                EditorGUI.PropertyField(new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight), isInitializerEnabledProperty);
                position.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            }

            if (resolverInstanceProperty.hasVisibleChildren)
            {
                position.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
                EditorGUI.LabelField
                    (
                     new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight),
                     new GUIContent("Installation Settings"),
                     EditorStyles.boldLabel
                    );
                position.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

                EditorGUI.indentLevel++;

                SerializedProperty childProperty = resolverInstanceProperty.Copy();
                var depth = childProperty.depth;
                var enterChildren = true;

                while (childProperty.NextVisible(enterChildren) && childProperty.depth > depth)
                {
                    EditorGUI.PropertyField
                        (
                         new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight),
                         childProperty,
                         true
                        );
                    position.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
                    enterChildren = false;
                }


                EditorGUI.indentLevel--;
            }


            EditorGUI.EndProperty();
        }
    }
}
