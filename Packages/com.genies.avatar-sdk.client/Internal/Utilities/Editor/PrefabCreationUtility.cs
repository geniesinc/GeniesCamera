using System;
using System.IO;
using UnityEditor;
using UnityEditor.ProjectWindowCallback;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Genies.Utilities.Editor
{
    /// <summary>
    /// Some handy methods for the easy creation of prefab assets from any given source.
    /// </summary>
    public static class PrefabCreationUtility
    {
        private const string PrefabExtension = ".prefab";

        /// <summary>
        /// Invoked when creating a prefab asset right before saving it to the project assets. You can perform any
        /// changes you want to the given <see cref="GameObject"/> so they are saved to the asset. Please keep in mind
        /// that the particular GameObject instance that is given will be destroyed after the asset is saved.
        /// </summary>
        public delegate void PrefabCreationHandler(GameObject prefab);
        
        private static Texture2D _prefabIcon;
        
        /// <summary>
        /// If a valid project window folder is currently focused then a new prefab asset will be created and enter name
        /// editing state. If the user cancels the name editing then no asset will be created. This method can be called
        /// from <see cref="MenuItem"/> methods to create context menu items for prefab asset creation.
        /// </summary>
        /// <param name="defaultName">The default asset name when the name editing state is entered</param>
        /// <param name="creationHandler">Optional callback to pre-process the create prefab right before saving it as an asset</param>
        /// <returns></returns>
        public static void StartPrefabAssetCreationWithNameEditing(
            string defaultName,
            PrefabCreationHandler creationHandler = null)
        {
            if (!defaultName.EndsWith(PrefabExtension))
            {
                defaultName += PrefabExtension;
            }

            _prefabIcon ??= EditorGUIUtility.IconContent("Prefab Icon").image as Texture2D;
            var endFromSourceAction = ScriptableObject.CreateInstance<CreatePrefabFromSourceAction>();
            endFromSourceAction.Source = null;
            endFromSourceAction.CreationHandler = creationHandler;
            ProjectWindowUtil.StartNameEditingIfProjectWindowExists(0, endFromSourceAction, defaultName, _prefabIcon, null);
        }
        
        /// <summary>
        /// If a valid project window folder is currently focused then a new prefab asset will be created and enter name
        /// editing state. If the user cancels the name editing then no asset will be created. This method can be called
        /// from <see cref="MenuItem"/> methods to create context menu items for prefab asset creation.
        /// </summary>
        /// <param name="defaultName">The default asset name when the name editing state is entered</param>
        /// <param name="sourceGuid">The GUID of the source asset to create the prefab from. It must be loadable as a
        /// GameObject, like model assets (FBX, OBJ...) or other prefabs</param>
        /// <param name="creationHandler">Optional callback to pre-process the create prefab right before saving it as an asset</param>
        /// <returns></returns>
        public static void StartPrefabAssetCreationWithNameEditing(
            string defaultName,
            string sourceGuid,
            PrefabCreationHandler creationHandler = null)
        {
            if (!defaultName.EndsWith(PrefabExtension))
            {
                defaultName += PrefabExtension;
            }

            _prefabIcon ??= EditorGUIUtility.IconContent("Prefab Icon").image as Texture2D;
            var endFromSourceGuidAction = ScriptableObject.CreateInstance<CreatePrefabFromSourceGuidAction>() ?? throw new ArgumentNullException("ScriptableObject.CreateInstance<CreatePrefabFromSourceGuidAction>()");
            endFromSourceGuidAction.SourceGuid = sourceGuid;
            endFromSourceGuidAction.CreationHandler = creationHandler;
            ProjectWindowUtil.StartNameEditingIfProjectWindowExists(0, endFromSourceGuidAction, defaultName, _prefabIcon, null);
        }

        /// <summary>
        /// If a valid project window folder is currently focused then a new prefab asset will be created and enter name
        /// editing state. If the user cancels the name editing then no asset will be created. This method can be called
        /// from <see cref="MenuItem"/> methods to create context menu items for prefab asset creation.
        /// </summary>
        /// <param name="defaultName">The default asset name when the name editing state is entered</param>
        /// <param name="source">The source GameObject to create the prefab from (if null, a new GameObject will be created)</param>
        /// <param name="creationHandler">Optional callback to pre-process the create prefab right before saving it as an asset</param>
        /// <returns></returns>
        public static void StartPrefabAssetCreationWithNameEditing(
            string defaultName,
            GameObject source,
            PrefabCreationHandler creationHandler = null)
        {
            if (!defaultName.EndsWith(PrefabExtension))
            {
                defaultName += PrefabExtension;
            }

            _prefabIcon ??= EditorGUIUtility.IconContent("Prefab Icon").image as Texture2D;
            var endFromSourceAction = ScriptableObject.CreateInstance<CreatePrefabFromSourceAction>();
            endFromSourceAction.Source = source;
            endFromSourceAction.CreationHandler = creationHandler;
            ProjectWindowUtil.StartNameEditingIfProjectWindowExists(0, endFromSourceAction, defaultName, _prefabIcon, null);
        }

        /// <summary>
        /// Creates a new prefab asset on the given path.
        /// </summary>
        /// <param name="path">The path within the project folders (must end with the .prefab extension)</param>
        /// <param name="creationHandler">Optional callback to pre-process the create prefab right before saving it the the path</param>
        /// <returns></returns>
        public static bool TryCreatePrefabAsset(string path, PrefabCreationHandler creationHandler = null)
        {
            return TryCreatePrefabAsset(path, source: null, creationHandler);
        }

        /// <summary>
        /// Creates a new prefab from the given source asset GUID.
        /// </summary>
        /// <param name="path">The path within the project folders (must end with the .prefab extension)</param>
        /// <param name="sourceGuid">The GUID of the source asset to create the prefab from. It must be loadable as a
        /// GameObject, like model assets (FBX, OBJ...) or other prefabs</param>
        /// <param name="creationHandler">Optional callback to pre-process the create prefab right before saving it the the path</param>
        /// <returns></returns>
        public static bool TryCreatePrefabAsset(string path, string sourceGuid, PrefabCreationHandler creationHandler = null)
        {
            if (string.IsNullOrEmpty(sourceGuid))
            {
                Debug.LogError(GetPrefabAssetCreationError(path, "The given source GUID is null or empty"));
                return false;
            }
            
            GameObject sourceAsset;
            
            try
            {
                string sourcePath = AssetDatabase.GUIDToAssetPath(sourceGuid);
                if (string.IsNullOrEmpty(sourcePath))
                {
                    Debug.LogError(GetPrefabAssetCreationError(path, $"Couldn't find the source asset from GUID {sourceGuid}"));
                    return false;
                }
                
                sourceAsset = AssetDatabase.LoadAssetAtPath<GameObject>(sourcePath);
                
                if (!sourceAsset)
                {
                    Debug.LogError(GetPrefabAssetCreationError(path, $"Couldn't load the source asset as GameObject from path: {sourcePath}"));
                    return false;
                }
            }
            catch (Exception exception)
            {
                Debug.LogError(GetPrefabAssetCreationError(path, exception.ToString()));
                return false;
            }
            
            return TryCreatePrefabAsset(path, sourceAsset, creationHandler);
        }
        
        /// <summary>
        /// Creates a new prefab asset from the given source GameObject.
        /// </summary>
        /// <param name="path">The path within the project folders (must end with the .prefab extension)</param>
        /// <param name="source">The source GameObject to create the prefab from (if null, a new GameObject will be created)</param>
        /// <param name="creationHandler">Optional callback to pre-process the create prefab right before saving it the the path</param>
        /// <returns></returns>
        public static bool TryCreatePrefabAsset(string path, GameObject source, PrefabCreationHandler creationHandler = null)
        {
            if (string.IsNullOrEmpty(path))
            {
                Debug.LogError(GetPrefabAssetCreationError(path, "The given asset path is null or empty"));
                return false;
            }

            string directory = Path.GetDirectoryName(path);
            if (!AssetDatabase.IsValidFolder(directory))
            {
                Debug.LogError(GetPrefabAssetCreationError(path, "The given path is not in a valid asset folder"));
                return false;
            }

            string extension = Path.GetExtension(path);
            if (extension != PrefabExtension)
            {
                Debug.LogError(GetPrefabAssetCreationError(path, $"The given path extension is not {PrefabExtension}"));
                return false;
            }
            
            GameObject prefab;

            try
            {
                prefab = source ? Object.Instantiate(source) : new GameObject("TemporaryPrefabCreationObject");
            }
            catch (Exception exception)
            {
                Debug.LogError(GetPrefabAssetCreationError(path, exception.ToString()));
                return false;
            }

            try
            {
                creationHandler?.Invoke(prefab);
                PrefabUtility.SaveAsPrefabAsset(prefab, path);
            }
            catch (Exception exception)
            {
                Debug.LogError(GetPrefabAssetCreationError(path, exception.ToString()));
                return false;
            }
            finally
            {
                Object.DestroyImmediate(prefab);
            }
            
            return true;
        }

        private static string GetPrefabAssetCreationError(string path, string reason)
        {
            return $"Failed to create prefab asset at {path}. Reason: {reason}";
        }

        private sealed class CreatePrefabFromSourceGuidAction : EndNameEditAction
        {
            public string SourceGuid;
            public PrefabCreationHandler CreationHandler;
            
            public override void Action(int instanceId, string pathName, string resourceFile)
            {
                TryCreatePrefabAsset(pathName, SourceGuid, CreationHandler);
                SourceGuid = null;
                CreationHandler = null;
            }
        }
        
        private sealed class CreatePrefabFromSourceAction : EndNameEditAction
        {
            public GameObject Source;
            public PrefabCreationHandler CreationHandler;
            
            public override void Action(int instanceId, string pathName, string resourceFile)
            {
                TryCreatePrefabAsset(pathName, Source, CreationHandler);
                Source = null;
                CreationHandler = null;
            }
        }
    }
}
