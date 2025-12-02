using Cysharp.Threading.Tasks;
using Genies.Naf;
using Genies.Utilities;
using UnityEngine;

namespace Genies.Avatars.Behaviors
{
    /// <summary>
    /// Factory class for creating different types of avatar controllers and genies.
    /// This static class provides methods for instantiating avatars with various configurations including unified genies, baked genies, and non-UMA genies.
    /// </summary>
    public static class AvatarControllerFactory
    {
        private const string ComposerUnifiedGeniePrefabPath = "UnifiedGenieInstance";
        private const string _unifiedDefaultBodyType = "Static/Genie_Unified_gen13gp_Race_Container";
        private const string _avatarLayerName = "Avatar";

        /// <summary>
        /// This method creates a new instance of an editable genie, assigned a layer, components and disposal procedure.
        /// </summary>
        /// <param name="avatarDefinition">Json representation of the avatar</param>
        /// <param name="root">Transform of the parent</param>
        /// <param name="assetParamsService">Service to fetch asset params (version, lod)</param>
        /// <returns>IAvatarController</returns>
        public static async UniTask<IAvatarController> CreateNafGenie(string avatarDefinition, Transform root, IAssetParamsService assetParamsService = null)
        {
            // load the user unified genie prefab that contains the animation controller and clone camera
            var unifiedGeniePrefab = Resources.Load<AvatarController>(ComposerUnifiedGeniePrefabPath);
            var genieInstance      = Object.Instantiate(unifiedGeniePrefab, Vector3.one * 1000, Quaternion.identity, root);

            genieInstance.name = "UnifiedGenieInstance";

            // create the unified genie instance and get the controller back
            var controller = await NativeAvatarsFactory.CreateUnifiedGenieAsync(avatarDefinition, genieInstance.transform, assetParamsService);

            // add the default body if not containing any assets TODO create a default avatar definition for NAF avatars
            if (controller.GetEquippedAssetIds().Count == 0)
            {
                await controller.EquipAssetAsync(_unifiedDefaultBodyType,
                    await controller.AssetParamsService.FetchParamsAsync(_unifiedDefaultBodyType));
            }

            controller.Genie.Root.SetLayerRecursive(LayerMask.NameToLayer("Avatar"));
            controller.Genie.Disposed += () => Object.Destroy(genieInstance);

            // move the camera as a child of the genie
            var camera = genieInstance.GetComponentInChildren<Camera>();
            camera.transform.SetParent(controller.Genie.Root.transform, false);

            genieInstance.Initialize(controller);
            return genieInstance;
        }

        public static async UniTask<NativeUnifiedGenieController> CreateSimpleNafGenie(string avatarDefinition, Transform root, IAssetParamsService assetParamsService = null)
        {
            // create the unified genie instance and get the controller back
            var controller = await NativeAvatarsFactory.CreateUnifiedGenieAsync(avatarDefinition, root, assetParamsService);

            // add the default body if not containing any assets TODO create a default avatar definition for NAF avatars
            if (controller.GetEquippedAssetIds().Count == 0)
            {
                await controller.EquipAssetAsync(_unifiedDefaultBodyType,
                    await controller.AssetParamsService.FetchParamsAsync(_unifiedDefaultBodyType));
            }

            var layer = LayerMask.NameToLayer(_avatarLayerName);
            if (layer >= 0)
            {
                controller.Genie.Root.SetLayerRecursive(layer);
            }

            return controller;
        }
    }
}
