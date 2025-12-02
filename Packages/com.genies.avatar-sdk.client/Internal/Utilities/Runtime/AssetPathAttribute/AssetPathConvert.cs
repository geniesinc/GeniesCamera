using System;
using System.IO;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Genies.Utilities
{
    public partial class AssetPath
    {
        private const string RESOURCES_FOLDER_NAME = "/Resources/";
        private const string ASSETS_FOLDER_NAME = "Assets/";

        /// <summary>
        /// Takes the string from the Asset Path Attribute and converts it into
        /// a usable resources path.
        /// </summary>
        /// <param name="assetPath">The project path that AssetPathAttribute serializes</param>
        /// <returns>The resources path if it exists otherwise returns the same path</returns>
        public static string ConvertToResourcesPath(string projectPath)
        {
            // Make sure it's not empty
            if (string.IsNullOrEmpty(projectPath))
            {
                return string.Empty;
            }

            // Define the keyword for the resources folder. You must ensure that your resources folder in Unity is named exactly "Resources" for this to work.
            const string resourcesKeyword = "Resources/";

            // Find the start index of the resource path (after "Resources/").
            int resourcesStartIndex = projectPath.IndexOf(resourcesKeyword, StringComparison.OrdinalIgnoreCase);

            if (resourcesStartIndex == -1)
            {
                // "Resources/" not found in the path, return empty string.
                return string.Empty;
            }

            // Calculate the start index of the actual path within the Resources folder.
            resourcesStartIndex += resourcesKeyword.Length;

            // Extract the resource path.
            string resourcePath = projectPath.Substring(resourcesStartIndex);

            // Removing the file extension
            resourcePath = Path.ChangeExtension(resourcePath, null);

            // 'Path.ChangeExtension' leaves a trailing period if extension is present, we want to remove this.
            if (resourcePath != null && resourcePath.EndsWith("."))
            {
                resourcePath = resourcePath.Remove(resourcePath.Length - 1);
            }

            return resourcePath;
        }

        /// <summary>
        /// Loads the asset at the following path. If the asset is contained within a resources folder
        /// this uses <see cref="UnityEngine.Resources.Load(string)"/>. If we are in the Editor this will
        /// use <see cref="UnityEditor.AssetDatabase.LoadAssetAtPath(string, Type)"/> instead. This will
        /// allow you to load any type at any path. Keep in mind at Runtime the asset can only be loaded
        /// if it is inside a resources folder.
        /// </summary>
        /// <param name="projectPath">The full project path of the object you are trying to load.</param>
        /// <param name="type"> asset type</param>
        /// <returns>The loaded asset or null if it could not be found.</returns>
        public static Object Load(string projectPath, Type type)
        {
            // Make sure our path is not null
            if(string.IsNullOrEmpty(projectPath))
            {
                return null;
            }

            // Get our resources path
            string resourcesPath = ConvertToResourcesPath(projectPath);

            if(!string.IsNullOrEmpty(resourcesPath))
            {
                // The asset is in a resources folder.
                return Resources.Load(resourcesPath, type);
            }

#if UNITY_EDITOR
            // We could not find it in resources so we just try the AssetDatabase.
            return UnityEditor.AssetDatabase.LoadAssetAtPath(projectPath, type);
#else
            return null;
#endif
        }

        /// <summary>
        /// Loads the asset at the following path. If the asset is contained within a resources folder
        /// this uses <see cref="UnityEngine.Resources.Load(string)"/>. If we are in the Editor this will
        /// use <see cref="UnityEditor.AssetDatabase.LoadAssetAtPath(string, Type)"/> instead. This will
        /// allow you to load any type at any path. Keep in mind at Runtime the asset can only be loaded
        /// if it is inside a resources folder.
        /// </summary>
        /// <typeparam name="T">The type of object you want to load</typeparam>
        /// <param name="projectPath">The full project path of the object you are trying to load.</param>
        /// <returns>The loaded asset or null if it could not be found.</returns>
        public static T Load<T>(string projectPath) where T : UnityEngine.Object
        {
            return Load(projectPath, typeof(T)) as T;
        }

    }
}
