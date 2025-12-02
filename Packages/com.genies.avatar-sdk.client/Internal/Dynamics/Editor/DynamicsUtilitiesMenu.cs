using UnityEditor;
using System.Linq;
using UnityEngine;
using System.IO;
using Genies.Dynamics;
using UMA;

namespace Genies.Components.Dynamics
{
    public static class DynamicsUtilitiesMenu
    {
        /// <summary>
        /// Creates an initial dynamic structures based on clues given by the dynamic joints found in a rig.
        /// The names of the joints as well as their hiearchical relationship to one another are utilized.
        /// </summary>
#if GENIES_INTERNAL
        [MenuItem("Window/Genies/Dynamics/Editor Utilities/Dynamics/One Click Dynamics Structure")]
#endif
        public static void OneClickDynamicsStructure()
        {
            GameObject[] rootObjects = Resources.FindObjectsOfTypeAll<GameObject>().Where(obj => obj.name == DynamicsSetup.DefaultHierarchyRootName).ToArray();
            GameObject root = null;

            // Find the hierarchy root from which a search for dynamics joints can be conducted.
            if (rootObjects.Length == 1)
            {
                root = rootObjects[0];
            }
            else if (Selection.activeGameObject != null)
            {
                root = DynamicsSetup.FindRoot(Selection.activeGameObject);
            }
            else
            {
                EditorUtility.DisplayDialog("One Click Dynamics Structure", $"Failed to create structure. Please select the hierarchy with properly named dynamics joints parented under a {DynamicsSetup.DefaultHierarchyRootName} object and try again.", "OK");
                return;
            }

            if (root == null)
            {
                EditorUtility.DisplayDialog("One Click Dynamics Structure", $"Failed to create structure. Unable to find {DynamicsSetup.DefaultHierarchyRootName} in hierarchy.", "OK");
                return;
            }

            var structures = DynamicsSetup.AddDynamicsToHierarchy(root);

            if (structures.Any())
            {
                string structureNames = string.Join(", ", structures.Select(structure => structure.name));
                EditorUtility.DisplayDialog("One Click Dynamics Structures", $"Dynamics structure created successfully. ({structureNames})", "OK");
            }
            else
            {
                EditorUtility.DisplayDialog("One Click Dynamics Structure", "Failed to set up dynamics. No dynamics joints found in hierarchy.", "OK");
            }
        }

        /// <summary>
        /// Given a dynamics structure in Unity with particles, links, and/or colliders present within its hiearchy, this function
        /// will find all of these dynamics components and add them to the containing structure.
        /// </summary>
#if GENIES_INTERNAL
        [MenuItem("Window/Genies Dynamics/Editor Utilities/Dynamics/Populate Dynamics Structure")]
#endif
        public static void PopulateDynamicsStructure()
        {
            if (Selection.activeGameObject == null || !Selection.activeGameObject.TryGetComponent(out DynamicsStructure dynamicsStructure))
            {
                EditorUtility.DisplayDialog("Populate Dynamics Structure", "Please select a game object with a dynamics structure component.", "OK");
                return;
            }

            dynamicsStructure.Particles = dynamicsStructure.transform.GetComponentsInChildren<DynamicsParticle>().ToList();
            dynamicsStructure.Links = dynamicsStructure.transform.GetComponentsInChildren<DynamicsLink>().ToList();
            dynamicsStructure.Colliders = dynamicsStructure.transform.GetComponentsInChildren<DynamicsCollider>().ToList();
        }

        /// <summary>
        /// Gathers all relevant information in a dynamics structure and writes it to a dynamics recipe that is used to send dynamics
        /// through the content pipeline.
        /// </summary>
#if GENIES_INTERNAL
        [MenuItem("Window/Genies/Dynamics/Editor Utilities/Dynamics/Create Dynamics Recipe From Structure")]
#endif
        public static void CreateRecipeFromDynamicsStructure() => CreateRecipeDialog.ShowWindow();

        public class CreateRecipeDialog : EditorWindow
        {
            private string _assetName = string.Empty;
            private string _recipeFilePath = string.Empty;
            private string _slotFilePath = string.Empty;
            private string _dynAnimAssetFilePath = string.Empty;

            private const string _recipeSuffix = "_DynamicsRecipe";
            private const string _recipeExtension = ".asset";
            private const string _slotSuffix = "_slot";
            private const string _slotExtension = ".asset";
            private const string _structureSuffix = "_DynamicsStructure";
            private const string _dynAnimAssetSuffix = "_DynamicsAnimatorAsset";
            private const string _dynAnimAssetExtension = ".asset";

            private DynamicsStructure _structure;
            private SlotDataAsset _slotDataAsset;

            public static void ShowWindow()
            {
                CreateRecipeDialog dialog = GetWindow<CreateRecipeDialog>("Create Dynamics Recipe From Structure");
                dialog.minSize = new Vector2(300, 500);
            }

            private GUIStyle GetCenteredTextStyle()
            {
                var style = new GUIStyle(GUI.skin.box)
                {
                    alignment = TextAnchor.MiddleCenter
                };
                return style;
            }

            private void OnGUI()
            {
                // Dynamics structure drag and drop area:
                Event structureDropEvent = Event.current;
                Rect structureDropArea = GUILayoutUtility.GetRect(0f, 100f, GUILayout.ExpandWidth(true));
                GUI.Box(structureDropArea, "Drop a dynamics structure object here to populate recipe data.", GetCenteredTextStyle());

                switch (structureDropEvent.type)
                {
                    case EventType.DragUpdated:
                    case EventType.DragPerform:
                        if (!structureDropArea.Contains(structureDropEvent.mousePosition))
                        {
                            break;
                        }

                        DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

                        if (structureDropEvent.type == EventType.DragPerform && DragAndDrop.objectReferences.Length == 1)
                        {
                            _structure = DragAndDrop.objectReferences[0] as DynamicsStructure;
                            if (_structure == null)
                            {
                                if (DragAndDrop.objectReferences[0] is GameObject gameObject)
                                {
                                    _structure = gameObject.GetComponent<DynamicsStructure>();
                                }
                            }

                            if (_structure != null)
                            {
                                DragAndDrop.AcceptDrag();
                            }
                        }

                        Event.current.Use();
                        break;
                }

                if (_structure != null)
                {
                    var recipeSummary = $"Structure Summary: \n {_structure.Particles?.Count} Particles \n {_structure?.Links.Count} Links \n {_structure.Colliders?.Count} Colliders";
                    EditorGUILayout.HelpBox(recipeSummary, MessageType.None);
                }
                else
                {
                    EditorGUILayout.HelpBox("No recipe data specified.", MessageType.None);
                }

                GUILayout.Space(25);

                // Slot data drag and drop area:
                Event slotDropEvent = Event.current;
                Rect slotDropArea = GUILayoutUtility.GetRect(0f, 100f, GUILayout.ExpandWidth(true));
                GUI.Box(slotDropArea, "Drop the corresponding slot data asset here.", GetCenteredTextStyle());

                switch(slotDropEvent.type)
                {
                    case EventType.DragUpdated:
                    case EventType.DragPerform:
                        if (!slotDropArea.Contains(slotDropEvent.mousePosition))
                        {
                            break;
                        }

                        DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

                        if (slotDropEvent.type == EventType.DragPerform && DragAndDrop.objectReferences.Length == 1)
                        {
                            _slotDataAsset = DragAndDrop.objectReferences[0] as SlotDataAsset;
                            if (_slotDataAsset != null)
                            {
                                DragAndDrop.AcceptDrag();
                                _slotFilePath = AssetDatabase.GetAssetPath(_slotDataAsset);
                                _assetName = _slotFilePath.Replace(_slotSuffix + _slotExtension, string.Empty);
                                _recipeFilePath = _assetName + _recipeSuffix + _recipeExtension;
                                _dynAnimAssetFilePath = _assetName + _dynAnimAssetSuffix + _dynAnimAssetExtension;
                            }
                        }

                        Event.current.Use();
                        break;
                }

                if (_slotDataAsset != null)
                {
                    var slotSummary = $"Slot Summary: \nSlot Name: {_slotDataAsset.slotName}";
                    EditorGUILayout.HelpBox(slotSummary, MessageType.None);
                }
                else
                {
                    EditorGUILayout.HelpBox("No slot data asset has been specified.", MessageType.None);
                }

                GUILayout.FlexibleSpace();

                if (_structure == null || _slotDataAsset == null)
                {
                    GUI.enabled = false;
                }
                else
                {

                }

                if (GUILayout.Button("Save Dynamics Recipe", GUILayout.ExpandWidth(true), GUILayout.Height(50)))
                {
                    DynamicsRecipe recipe = AssetDatabase.LoadAssetAtPath<DynamicsRecipe>(_recipeFilePath);
                    if (recipe == null)
                    {
                        recipe = CreateInstance<DynamicsRecipe>();
                        AssetDatabase.CreateAsset(recipe, _recipeFilePath);
                    }

                    DynamicsSetup.PopulateRecipeData(recipe, _structure);
                    recipe.StructureName = Path.GetFileName(_slotFilePath).Replace(_slotSuffix + _slotExtension, string.Empty) + _structureSuffix;
                    recipe.parentName = DynamicsSetup.DefaultHierarchyRootName;
                    recipe.generatedChildName = DynamicsSetup.DefaultDynamicsContainerName;
                    EditorUtility.SetDirty(recipe);

                    AssetDatabase.SaveAssets();
                    AssetDatabase.Refresh();

                    DynamicsAnimatorAsset dynAnimAsset = AssetDatabase.LoadAssetAtPath<DynamicsAnimatorAsset>(_dynAnimAssetFilePath);
                    if (dynAnimAsset == null)
                    {
                        dynAnimAsset = CreateInstance<DynamicsAnimatorAsset>();
                        AssetDatabase.CreateAsset(dynAnimAsset, _dynAnimAssetFilePath);
                    }
                    dynAnimAsset.recipe = recipe;
                    EditorUtility.SetDirty(recipe);

                    AssetDatabase.SaveAssets();
                    AssetDatabase.Refresh();

                    // The recipe is now tied to a saved asset file and it is best to edit it directly in the inspector.
                    Selection.activeObject = AssetDatabase.LoadMainAssetAtPath(_recipeFilePath);

                    // Update the structure name in scene to siginfy that recipe is saved.
                    _structure.gameObject.name = recipe.StructureName;
                }

                GUI.enabled = true;
            }
        }

        /// <summary>
        /// Provides a dialog window with buttons to add colliders to a selected dynamics structure.
        /// </summary>
#if GENIES_INTERNAL
        [MenuItem("Window/Genies/Dynamics/Editor Utilities/Dynamics/Add Dynamics Colliders")]
#endif
        public static void AddDynamicsColliders() => AddCollidersDialog.ShowWindow();

        public class AddCollidersDialog : EditorWindow
        {
            private DynamicsStructure structure;
            private GameObject root;

            public static void ShowWindow()
            {
                AddCollidersDialog dialog = GetWindow<AddCollidersDialog>("Add Dynamics Colliders");
                dialog.minSize = new Vector2(300, 600);
            }

            private void OnGUI()
            {
                GUIStyle style = new(GUI.skin.label)
                {
                    wordWrap = true,
                    alignment = TextAnchor.MiddleCenter
                };

                GUILayout.Space(25);
                GUILayout.Label("This is a utility for adding the common colliders needed for dynamics.\n\nMake sure you have a dynamics structure selected in the hierarchy and then click one of the following buttons to add the corresponding collider.", style);
                GUILayout.Space(25);

                if (GUILayout.Button("Head", GUILayout.ExpandWidth(true), GUILayout.Height(50)))
                {
                    if (!CheckSelection())
                    {
                        return;
                    }

                    DynamicsSetup.AddHumanoidCollider(ColliderConfiguration.HumanoidColliderLocation.Head, structure);
                }

                if (GUILayout.Button("Neck", GUILayout.ExpandWidth(true), GUILayout.Height(50)))
                {
                    if (!CheckSelection())
                    {
                        return;
                    }

                    DynamicsSetup.AddHumanoidCollider(ColliderConfiguration.HumanoidColliderLocation.Neck, structure);
                }

                if (GUILayout.Button("Torso", GUILayout.ExpandWidth(true), GUILayout.Height(50)))
                {
                    if (!CheckSelection())
                    {
                        return;
                    }

                    DynamicsSetup.AddHumanoidCollider(ColliderConfiguration.HumanoidColliderLocation.Torso, structure);
                }

                if (GUILayout.Button("Upper Arms", GUILayout.ExpandWidth(true), GUILayout.Height(50)))
                {
                    if (!CheckSelection())
                    {
                        return;
                    }

                    DynamicsSetup.AddHumanoidCollider(ColliderConfiguration.HumanoidColliderLocation.LeftUpperArm, structure);
                    DynamicsSetup.AddHumanoidCollider(ColliderConfiguration.HumanoidColliderLocation.RightUpperArm, structure);
                }

                if (GUILayout.Button("Lower Arms", GUILayout.ExpandWidth(true), GUILayout.Height(50)))
                {
                    if (!CheckSelection())
                    {
                        return;
                    }

                    DynamicsSetup.AddHumanoidCollider(ColliderConfiguration.HumanoidColliderLocation.LeftLowerArm, structure);
                    DynamicsSetup.AddHumanoidCollider(ColliderConfiguration.HumanoidColliderLocation.RightLowerArm, structure);
                }

                if (GUILayout.Button("Hands", GUILayout.ExpandWidth(true), GUILayout.Height(50)))
                {
                    if (!CheckSelection())
                    {
                        return;
                    }

                    DynamicsSetup.AddHumanoidCollider(ColliderConfiguration.HumanoidColliderLocation.LeftHand, structure);
                    DynamicsSetup.AddHumanoidCollider(ColliderConfiguration.HumanoidColliderLocation.RightHand, structure);
                }

                if (GUILayout.Button("Hips", GUILayout.ExpandWidth(true), GUILayout.Height(50)))
                {
                    if (!CheckSelection())
                    {
                        return;
                    }

                    DynamicsSetup.AddHumanoidCollider(ColliderConfiguration.HumanoidColliderLocation.Hips, structure);
                }

                if (GUILayout.Button("Upper Legs", GUILayout.ExpandWidth(true), GUILayout.Height(50)))
                {
                    if (!CheckSelection())
                    {
                        return;
                    }

                    DynamicsSetup.AddHumanoidCollider(ColliderConfiguration.HumanoidColliderLocation.LeftUpperLeg, structure);
                    DynamicsSetup.AddHumanoidCollider(ColliderConfiguration.HumanoidColliderLocation.RightUpperLeg, structure);
                }

                if (GUILayout.Button("Lower Legs", GUILayout.ExpandWidth(true), GUILayout.Height(50)))
                {
                    if (!CheckSelection())
                    {
                        return;
                    }

                    DynamicsSetup.AddHumanoidCollider(ColliderConfiguration.HumanoidColliderLocation.LeftLowerLeg, structure);
                    DynamicsSetup.AddHumanoidCollider(ColliderConfiguration.HumanoidColliderLocation.RightLowerLeg, structure);
                }
            }

            private bool CheckSelection()
            {
                var structures = FindObjectsByType<DynamicsStructure>(FindObjectsSortMode.None);

                // Determine the dynamics structure to be added to.
                // 1. If only one structure in scene, use that structure.
                // 2. If a game object is selected and it contains a structure, use that structure.
                if (structures.Length == 1)
                {
                    structure = structures[0];
                }
                else if (Selection.activeGameObject != null && Selection.activeGameObject.TryGetComponent(out structure))
                {
                    // Structure selected in hierarchy.
                }
                else if (structures.Length > 1)
                {
                    EditorUtility.DisplayDialog("Add Dynamics Colliders", "More than one dynamics structure found in scene, please select the one you would like to add colliders to.", "OK");
                    return false;
                }
                else
                {
                    EditorUtility.DisplayDialog("Add Dynamics Colliders", "Failed to add collider, please make sure a dynamics structure is selected.", "OK");
                    return false;
                }

                root = DynamicsSetup.FindRoot(structure.gameObject);

                if (root == null)
                {
                    EditorUtility.DisplayDialog("Add Dynamics Colliders", "Failed to add collider, please ensure that the Root game object is in the same hierarchy as the selected dynamics structure.", "OK");
                    return false;
                }

                return true;
            }
        }
    }
}
