using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.ProjectWindowCallback;
using UnityEngine;

namespace Genies.Avatars.Sdk.Editor
{
#if GENIES_SDK && !GENIES_INTERNAL
    internal static class AvatarAnimatorControllerUtility
#else
    public static class AvatarAnimatorControllerUtility
#endif
    {
        private const string _configReferenceGuid = "d633efa2d84c29d49867dc502918df96";

        /// <summary>
        /// Creates a new <see cref="AnimatorController"/> asset in the project on the given path. The created controller will
        /// include the default AnimatorController configuration that our Avatars use.
        /// </summary>
        public static AnimatorController CreateAsset(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                Debug.LogError("Failed to create avatar animator controller asset. The given path is null or empty");
                return null;
            }

            string directory = Path.GetDirectoryName(path);
            if (!AssetDatabase.IsValidFolder(directory))
            {
                Debug.LogError($"Failed to create avatar animator controller asset. The given path is not a valid directory: {path}");
                return null;
            }

            string extension = Path.GetExtension(path);
            if (extension != ".controller")
            {
                Debug.LogError($"Failed to create avatar animator controller asset. The given path extension must be .controller: {path}");
                return null;
            }

            // create the new animator controller asset and add the face animation parameters
            var animatorController = AnimatorController.CreateAnimatorControllerAtPath(path);
            AddAllDefaultConfiguration(animatorController);

            return animatorController;
        }

        private static void AddAllDefaultConfiguration(AnimatorController controller)
        {
            var config = GetConfig();
            if (config == null)
            {
                return;
            }

            foreach (var defaultAsset in config.Defaults)
            {
                defaultAsset.ApplyToTargetController(controller);
            }
        }

        /// <summary>
        /// Adds the face animation parameters used by our Avatars to the given <see cref="AnimatorController"/>.
        /// </summary>
        public static void AddFaceAnimationParameters(AnimatorController controller)
        {
            var config = GetConfig();
            if (config == null)
            {
                return;
            }

            bool foundDefault = false;
            foreach(var defaultAsset in config.Defaults)
            {
                if(defaultAsset.GetType() == typeof(FaceParamAnimControllerDefaultAsset))
                {
                    defaultAsset.ApplyToTargetController(controller);
                    foundDefault = true;
                }
            }

            if (!foundDefault)
            {
                Debug.LogError("No face param data found");
            }
        }

        /// <summary>
        /// Adds the custom grab pose animation layers used by our Avatars to the given <see cref="AnimatorController"/>.
        /// </summary>
        public static void AddGrabLayers(AnimatorController controller)
        {
            var config = GetConfig();
            if (config == null)
            {
                return;
            }

            bool foundDefault = false;
            foreach (var defaultAsset in config.Defaults)
            {
                if (defaultAsset.GetType() == typeof(GrabLayersAnimControllerDefaultAsset))
                {
                    defaultAsset.ApplyToTargetController(controller);
                    foundDefault = true;
                }
            }

            if (!foundDefault)
            {
                Debug.LogError("No grab layer data found");
            }
        }

#if GENIES_INTERNAL
        [MenuItem("Assets/Create/Genies SDK/Avatar Animator Controller", false, 0)]
#endif
        private static void CreateAssetMenuItem()
        {
            string defaultName = $"New Animator Controller.controller";
            Texture2D icon = EditorGUIUtility.IconContent("AnimatorController Icon").image as Texture2D;
            var endAction = ScriptableObject.CreateInstance<CreateAssetAction>();
            ProjectWindowUtil.StartNameEditingIfProjectWindowExists(0, endAction, defaultName, icon, null);
        }

#if GENIES_INTERNAL
        [MenuItem("Tools/Genies/Avatar/Add face animation parameters", false)]
#endif
        private static void AddFaceAnimationParametersMenuItem()
        {
            AddFaceAnimationParameters(Selection.activeObject as AnimatorController);
        }

#if GENIES_INTERNAL
        [MenuItem("Tools/Genies/Avatar/Add face animation parameters", true)]
#endif
        private static bool AddFaceAnimationParametersValidator()
        {
            return Selection.activeObject is AnimatorController;
        }

#if GENIES_INTERNAL
        [MenuItem("Tools/Genies/Avatar/Add grab layers", false)]
#endif
        private static void AddGrabLayersMenuItem()
        {
            AddGrabLayers(Selection.activeObject as AnimatorController);
        }

#if GENIES_INTERNAL
        [MenuItem("Tools/Genies/Avatar/Add grab layers", true)]
#endif
        private static bool AddGrabLayersValidator()
        {
            return Selection.activeObject is AnimatorController;
        }

        private static AnimControllerDefaultsConfig GetConfig()
        {
            string refPath = AssetDatabase.GUIDToAssetPath(_configReferenceGuid);
            var config = AssetDatabase.LoadAssetAtPath<AnimControllerDefaultsConfig>(refPath);
            if (!config)
            {
                Debug.LogError($"Couldn't find Anim Controller Defaults Config using Guid {_configReferenceGuid}");
                return null;
            }

            return config;
        }

        private sealed class CreateAssetAction : EndNameEditAction
        {
            public override void Action(int instanceId, string pathName, string resourceFile)
            {
                CreateAsset(pathName);
            }
        }
    }
}
