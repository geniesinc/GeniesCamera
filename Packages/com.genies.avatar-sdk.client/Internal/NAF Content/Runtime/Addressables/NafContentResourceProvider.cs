using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Genies.Addressables.UniversalResourceLocation;
using Genies.Naf.Content;
using Genies.Refs;
using Genies.ServiceManagement;
using GnWrappers;
using UnityEngine;

namespace Genies.Naf.Addressables
{
    public class NafContentResourceProvider : ICustomResourceProvider
    {
        private readonly ContainerApi _containerApi;
        private IAssetParamsService _assetParamsService = ServiceManager.GetService<IAssetParamsService>(null);
        private IAssetIdConverter _idConverter = ServiceManager.GetService<IAssetIdConverter>(null);

        public NafContentResourceProvider(NafAssetResolverConfig resolverConfig)
        {
            // ContainerApi requires NAF plugin to be initialized
            if (NafPlugin.IsInitialized is false)
            {
                NafPlugin.Initialize();
            }

            _containerApi = new ContainerApi(resolverConfig.Serialize());
        }

        public async UniTask<Ref<Sprite>> Provide(string internalId)
        {
            var convertedId = await _idConverter.ConvertToUniversalIdAsync(internalId);
            var parameters = await _assetParamsService.FetchParamsAsync(internalId);
            parameters["lod"] = "0"; // Always load LOD 0 for icons
            Ref<Sprite> iconRef = await LoadIconAsyncInternal(convertedId, parameters);
            return iconRef;
        }

        private async UniTask<Ref<Sprite>> LoadIconAsyncInternal(string assetId, Dictionary<string, string> parameters = null)
        {
            using var cParams = CreateParameterMap(parameters);

            int handle = _containerApi.LoadIconAsync(assetId, cParams);

            if (handle == -1)
            {
                return default; // Failed to start
            }

            while (!_containerApi.IsIconAsyncLoadComplete(handle))
            {
                await UniTask.Yield();
            }

            using GnWrappers.Texture texture = _containerApi.GetIconAsyncLoadResult(handle);

            Ref<UnityEngine.Texture> refTex = texture.AsUnityTexture();
            Sprite sprite = CreateFromTexture(refTex.Item as UnityEngine.Texture2D);

            Ref<Sprite> spriteRef = CreateRef.FromUnityObject(sprite);
            spriteRef = CreateRef.FromDependentResource(spriteRef, refTex);

            return spriteRef;
        }

        private UnorderedMapString CreateParameterMap(Dictionary<string, string> parameters)
        {
            if (parameters == null)
            {
                return null;
            }

            var cParams = new UnorderedMapString();
            foreach (var kv in parameters)
            {
                cParams.Add(kv.Key, kv.Value);
            }
            return cParams;
        }

        private Sprite CreateFromTexture(Texture2D texture)
        {
            if (texture == null)
            {
                Debug.Log("Creating default texture for missing icon!");
                texture = Texture2D.grayTexture;
            }

            return Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
        }
    }
}
