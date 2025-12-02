using System;
using System.Collections.Generic;
using System.Diagnostics;
using Cysharp.Threading.Tasks;
using Genies.Avatars;
using UnityEditor;
using UnityEngine;

using Debug = UnityEngine.Debug;

namespace Genies.Naf.Editor
{
    [CustomEditor(typeof(NativeGenieBuilder))]
    public class NativeGenieBuilderEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            if (target is not NativeGenieBuilder builder)
            {
                return;
            }

            if (!Application.isPlaying)
            {
                return;
            }

            if (builder.RefittingDebuggerEnabled)
            {
                if (GUILayout.Button("Disable Refitting Debugger"))
                {
                    builder.SetRefittingDebuggerEnabled(false);
                    builder.RebuildAsync(forced: true).Forget();
                }

                DrawRefittingDebugSlider(builder);
            }
            else
            {
                if (GUILayout.Button("Enable Refitting Debugger"))
                {
                    builder.SetRefittingDebuggerEnabled(true);
                    builder.RebuildAsync(forced: true).Forget();
                }
            }

            GUILayout.Space(16);

            DrawColorAttributesInspector(builder);
            DrawBodyAttributesInspector(builder);

            GUILayout.Space(16);
            GUILayout.BeginHorizontal(); GUILayout.FlexibleSpace();
            if (GUILayout.Button("Export as glTF"))
            {
                ExportAsGltfAsync(builder, writeGlb: false).Forget();
            }
            if (GUILayout.Button("Export as glb"))
            {
                ExportAsGltfAsync(builder, writeGlb: true).Forget();
            }
            GUILayout.FlexibleSpace(); GUILayout.EndHorizontal();
        }

        private static string LastSavedPresetPath = "Assets";
        private static int PresetPickerControlId;
        private static readonly Dictionary<string, bool> SelectedAttributesForPreset = new();
        private static readonly GUILayoutOption[] SelectedToggleOptions = new[]
        {
            GUILayout.Width(16),
        };

        private void DrawRefittingDebugSlider(NativeGenieBuilder builder)
        {
            IReadOnlyList<SkinnedMeshRenderer> renderers = builder.NativeGenie.Renderers;
            if (renderers.Count == 0)
            {
                return;
            }

            float weight = renderers[0].GetBlendShapeWeight(0);
            float newWeight = EditorGUILayout.Slider("Refitting Weight", weight, 0.0f, 100.0f);
            if (Mathf.Approximately(weight, newWeight))
            {
                return;
            }

            foreach (SkinnedMeshRenderer renderer in renderers)
            {
                renderer.SetBlendShapeWeight(0, newWeight);
            }
        }

        private void DrawColorAttributesInspector(NativeGenieBuilder builder)
        {
            List<string> attributes = builder.GetExistingColorAttributes();
            if (attributes.Count == 0)
            {
                EditorGUILayout.HelpBox("This native genie has no color attributes", MessageType.Info);
                return;
            }

            GUILayout.BeginHorizontal(); GUILayout.FlexibleSpace();
            GUILayout.Label($"Color Attributes ({attributes.Count})");
            GUILayout.FlexibleSpace(); GUILayout.EndHorizontal();
            GUILayout.Space(4);

            if (GUILayout.Button("Unset All"))
            {
                builder.UnsetAllColors();
                builder.RebuildAsync().Forget();
            }

            GUILayout.Space(4);
            foreach (string attribute in attributes)
            {
                Color? color = builder.GetColor(attribute);
                Color displayColor = color ?? default;

                GUILayout.BeginHorizontal();
                Color newColor = EditorGUILayout.ColorField(attribute, displayColor);
                bool unset = color.HasValue && GUILayout.Button("Unset");
                GUILayout.EndHorizontal();

                if (unset)
                {
                    builder.UnsetColor(attribute);
                    builder.RebuildAsync().Forget();
                }
                else if (newColor != displayColor)
                {
                    builder.SetColor(attribute, newColor);
                    builder.RebuildColors();
                }
            }
            GUILayout.Space(4);
        }

        private void DrawBodyAttributesInspector(NativeGenieBuilder builder)
        {
            List<string> attributes = builder.GetExistingShapeAttributes();
            if (attributes.Count == 0)
            {
                EditorGUILayout.HelpBox("This native genie has no shape attributes", MessageType.Info);
                return;
            }

            GUILayout.BeginHorizontal(); GUILayout.FlexibleSpace();
            GUILayout.Label($"Shape Attributes ({attributes.Count})");
            GUILayout.FlexibleSpace(); GUILayout.EndHorizontal();
            GUILayout.Space(4);

            CheckForBodyPresetSelection(builder);
            if (GUILayout.Button("Set Preset"))
            {
                PresetPickerControlId = GUIUtility.GetControlID("PresetPicker".GetHashCode(), FocusType.Passive);
                EditorGUIUtility.ShowObjectPicker<BodyAttributesPreset>(null, false, "", PresetPickerControlId);
            }

            if (GUILayout.Button("Reset All"))
            {
                builder.ResetShapeAttributeWeights();
                builder.RebuildSkeletonOffset();
            }

            GUILayout.Space(4);
            foreach (string attribute in attributes)
            {
                float weight = builder.GetShapeAttributeWeight(attribute);

                GUILayout.BeginHorizontal();
                SelectedAttributesForPreset.TryAdd(attribute, false);
                SelectedAttributesForPreset[attribute] = EditorGUILayout.Toggle(GUIContent.none, SelectedAttributesForPreset[attribute], SelectedToggleOptions);
                float newWeight = EditorGUILayout.Slider(attribute, weight, -1.0f, 1.0f);
                GUILayout.EndHorizontal();

                if (newWeight != weight)
                {
                    builder.SetShapeAttributeWeight(attribute, newWeight);
                    builder.RebuildSkeletonOffset();
                }
            }
            GUILayout.Space(4);

            if (GUILayout.Button("Select All"))
            {
                foreach (string attribute in attributes)
                {
                    SelectedAttributesForPreset[attribute] = true;
                }
            }

            if (GUILayout.Button("Deselect All"))
            {
                foreach (string attribute in attributes)
                {
                    SelectedAttributesForPreset[attribute] = false;
                }
            }

            if (GUILayout.Button("Export Preset from Selected"))
            {
                SaveToBodyPreset(builder, attributes.Count);
            }

            if (LastSavedPresetPath != "Assets" && GUILayout.Button("Update Last Preset from Selected"))
            {
                SaveToBodyPreset(builder, LastSavedPresetPath, attributes.Count);
            }
        }

        private static void CheckForBodyPresetSelection(NativeGenieBuilder builder)
        {
            Event current = Event.current;
            if (current.type is not EventType.ExecuteCommand || current.commandName != "ObjectSelectorClosed" || PresetPickerControlId != EditorGUIUtility.GetObjectPickerControlID())
            {
                return;
            }

            var bodyPreset = EditorGUIUtility.GetObjectPickerObject() as BodyAttributesPreset;
            if (bodyPreset)
            {
                builder.SetShapeAttributes(bodyPreset);
                builder.RebuildSkeletonOffset();
            }
        }

        private static void SaveToBodyPreset(NativeGenieBuilder builder, int attributeCount)
        {
            string path = EditorUtility.SaveFilePanelInProject(
                "Save as Body Preset",
                "BodyPreset",
                "asset",
                "Please enter a file name to save the asset to",
                LastSavedPresetPath
            );

            SaveToBodyPreset(builder, path, attributeCount);
        }

        private static void SaveToBodyPreset(NativeGenieBuilder builder, string path, int attributeCount)
        {
            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            LastSavedPresetPath = path;
            bool created = false;
            var asset = AssetDatabase.LoadAssetAtPath<BodyAttributesPreset>(path);
            if (!asset)
            {
                asset = CreateInstance<BodyAttributesPreset>();
                created = true;
            }

            asset.attributesStates.Clear();
            foreach ((string attribute, bool selected) in SelectedAttributesForPreset)
            {
                if (selected)
                {
                    asset.attributesStates.Add(new BodyAttributeState(attribute, builder.GetShapeAttributeWeight(attribute)));
                }
            }

            if (created)
            {
                AssetDatabase.CreateAsset(asset, path);
            }
            else
            {
                EditorUtility.SetDirty(asset);
                AssetDatabase.SaveAssetIfDirty(asset);
            }

            AssetDatabase.SaveAssets();
        }

        public static async UniTask ExportAsGltfAsync(NativeGenieBuilder builder, bool writeGlb = false)
        {
            string extension = writeGlb ? "glb" : "gltf";
            string filePath = EditorUtility.SaveFilePanel("Export as glTF", null, builder.NativeGenie.Root.name, extension);

            if (string.IsNullOrEmpty(filePath))
            {
                return;
            }

            var stopwatch = new Stopwatch();
            stopwatch.Start();

            await UniTask.RunOnThreadPool(() => builder.AssetBuilder.ExportAsGltf(filePath, writeGlb));

            stopwatch.Stop();
            TimeSpan elapsed = stopwatch.Elapsed;
            Debug.Log($"<color=magenta>Took <color=cyan>{elapsed.TotalSeconds:0.000} seconds</color> to export the genie.</color>");
        }
    }
}
