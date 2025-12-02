using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace Genies.ServiceManagement.Editor
{
    [CustomEditor(typeof(AutoResolverSettings))]
    public class AutoResolverSettingsEditor : UnityEditor.Editor
    {
        private ReorderableList _list;
        private Vector2 _scrollPosition;  // Added to track the scroll position

        private void OnEnable()
        {
            var items = serializedObject.FindProperty("ResolverSettingsList");
            _list = new ReorderableList(serializedObject, items, true, true, false, false);
            _list.drawHeaderCallback = (Rect rect) => { EditorGUI.LabelField(rect, "Resolver Settings List"); };
            _list.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
            {
                var element = items.GetArrayElementAtIndex(index);
                EditorGUI.PropertyField(new Rect(rect.x, rect.y, rect.width, EditorGUIUtility.singleLineHeight), element, GUIContent.none);
            };

            _list.elementHeightCallback = index =>
            {
                var element = items.GetArrayElementAtIndex(index);
                return EditorGUI.GetPropertyHeight(element);
            };
            
            AutoResolver.UpdateSettings(serializedObject.targetObject as AutoResolverSettings);
        }

        private void OnDisable()
        {
            serializedObject.Dispose();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            // Begin the scroll view here
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            _list.DoLayoutList();

            // End the scroll view here
            EditorGUILayout.EndScrollView();

            serializedObject.ApplyModifiedProperties();
        }
    }
}
