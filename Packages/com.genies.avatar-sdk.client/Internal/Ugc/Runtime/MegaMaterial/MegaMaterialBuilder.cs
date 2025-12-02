using Cysharp.Threading.Tasks;
using Genies.Assets.Services;
using Genies.Refs;
using UnityEngine;

namespace Genies.Ugc
{
    public sealed class MegaMaterialBuilder : IMegaMaterialBuilder
    {
        // dependencies
        private readonly IAssetLoader<UgcElementAsset> _elementLoader;
        private readonly IAssetsProvider<Texture2D> _materialsProvider;
        private readonly IAssetsProvider<Texture2D> _patternsProvider;
        private readonly IAssetsProvider<Texture2D> _projectedTexturesProvider;

        public MegaMaterialBuilder(IAssetLoader<UgcElementAsset> elementLoader,
                                   IAssetsProvider<Texture2D> materialsProvider,
                                   IAssetsProvider<Texture2D> patternsProvider,
                                   IAssetsProvider<Texture2D> projectedTexturesProvider)
        {
            _elementLoader = elementLoader;
            _materialsProvider = materialsProvider;
            _patternsProvider = patternsProvider;
            _projectedTexturesProvider = projectedTexturesProvider;
        }

        public async UniTask<MegaMaterial> BuildMegaMaterialAsync(Split split)
        {
            if (split is null || string.IsNullOrEmpty(split.ElementId))
            {
                return null;
            }

            Ref<UgcElementAsset> elementRef = await _elementLoader.LoadAsync(split.ElementId);
            return await BuildMegaMaterialAsync(split, elementRef);
        }

        public async UniTask<MegaMaterial> BuildMegaMaterialAsync(Split split, Ref<UgcElementAsset> elementRef)
        {
            if (split is null || !elementRef.IsAlive)
            {
                return null;
            }

            // create a mega material (it will own the elementRef, so the ref will be disopsed when the mega material is)
            MegaMaterial megaMaterial = BuildMegaMaterial(elementRef, split.MaterialVersion);
            if (megaMaterial is null || !megaMaterial.IsAlive)
            {
                return null;
            }

            // apply the split to the material
            await megaMaterial.ApplySplitAsync(split);
            return megaMaterial;
        }

        public MegaMaterial BuildMegaMaterial(Ref<UgcElementAsset> elementRef, string materialVersion = null)
        {
            // for now the material version is ignored since we have not really implemented any retro-compatibility system
            return new MegaMaterial(_materialsProvider, _patternsProvider, _projectedTexturesProvider, elementRef);
        }
    }
}
