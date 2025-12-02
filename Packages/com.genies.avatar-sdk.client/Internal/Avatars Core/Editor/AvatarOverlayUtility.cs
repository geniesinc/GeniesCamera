using Genies.Utilities.Editor;
using UnityEditor;

namespace Genies.Avatars.Sdk.Editor
{
    /// <summary>
    /// Editor utility class for creating and managing Avatar Overlay prefabs.
    /// This class provides functionality to create AvatarOverlay prefab assets in the Unity editor.
    /// </summary>
#if GENIES_SDK && !GENIES_INTERNAL
    internal static class AvatarOverlayUtility
#else
    public static class AvatarOverlayUtility
#endif
    {
        /// <summary>
        /// GUID reference to the base Avatar FBX model used for creating overlay prefabs.
        /// </summary>
        public const string AvatarFbxGuid = "20306f71dd2e8e040b240e84805a0da4";

        /// <summary>
        /// Handler delegate used for adding AvatarOverlay components to newly created prefabs.
        /// </summary>
        public static readonly PrefabCreationUtility.PrefabCreationHandler OverlayCreationHandler =
            prefab => prefab.AddComponent<AvatarOverlay>();

        /// <summary>
        /// Tries to create a new <see cref="AvatarOverlay"/> prefab asset at the specified path.
        /// </summary>
        /// <param name="path">The file path where the overlay prefab should be created.</param>
        /// <returns>True if the overlay asset was created successfully; otherwise, false.</returns>
        public static bool TryCreateOverlayAsset(string path)
        {
            return PrefabCreationUtility.TryCreatePrefabAsset(path, AvatarFbxGuid, OverlayCreationHandler);
        }

#if GENIES_INTERNAL
        [MenuItem("Assets/Create/Genies SDK/Avatar Overlay Prefab", false, 0)]
#endif
        private static void CreateOverlayAssetMenuItem()
        {
            string defaultName = $"New {ObjectNames.NicifyVariableName(nameof(AvatarOverlay))}.prefab";
            PrefabCreationUtility.StartPrefabAssetCreationWithNameEditing(defaultName, AvatarFbxGuid, OverlayCreationHandler);
        }
    }
}
