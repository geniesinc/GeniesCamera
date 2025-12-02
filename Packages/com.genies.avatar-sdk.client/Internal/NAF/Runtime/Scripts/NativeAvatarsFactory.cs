using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Genies.Naf
{
    public static class NativeAvatarsFactory
    {
        public static async UniTask<NativeUnifiedGenieController> CreateUnifiedGenieAsync(string definition = null,
            Transform parent = null, IAssetParamsService assetParamsService = null)
        {
            NativeGenieBuilder builder = CreateDefaultNativeGenieBuilder(parent);
            NativeUnifiedGenieController controller = await CreateUnifiedGenieAsync(builder, definition, assetParamsService);
            return controller;
        }

        public static async UniTask<NativeUnifiedGenieController> CreateUnifiedGenieAsync(NativeGenieBuilder builder,
            string definition = null, IAssetParamsService assetParamsService = null)
        {
            // set a no-op asset params service if none is provided
            assetParamsService ??= new NoOpAssetParamsService();

            // create the controller
            var controller = new NativeUnifiedGenieController(builder, assetParamsService);

            // set the definition if provided
            if (!string.IsNullOrWhiteSpace(definition))
            {
                await controller.SetDefinitionAsync(definition);
            }

            return controller;
        }

        public static NativeGenieBuilder CreateDefaultNativeGenieBuilder(Transform parent = null)
        {
            var prefab = Resources.Load<NativeGenieBuilder>("NativeGenie");
            if (!prefab)
            {
                Debug.LogError($"[{nameof(NativeAvatarsFactory)}] could not find {nameof(NativeGenieBuilder)} prefab in Resources.");
                return null;
            }

            NativeGenieBuilder genie = Object.Instantiate(prefab, parent);
            if (!genie)
            {
                return null;
            }

            return genie;
        }
    }
}
