using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Genies.Assets.Services;
using Genies.Shaders;
using Genies.Refs;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Genies.Ugc
{
    /// <summary>
    /// Encapsulates a Unity Material instance from the mega shader. You can apply region, style or pattern definitions to the wrapped material
    /// and this class will take care of all the generated dependencies. Must be disposed for the material to be destroyed and all dependencies disposed.
    /// </summary>
    public sealed class MegaMaterial : IDisposable
    {
        private const int RegionCount = MegaShaderMaterialExtensions.MaxRegions;
        private const string BaseMaterialResourcePath = "material_base_BASIC_inside";
        private readonly Color[] _regionDebuggingColors = new[] { Color.red, Color.green, Color.blue, Color.yellow };

        public bool IsAlive { get; private set; }
        public Material Material { get; private set; }

        public bool UseDefaultColors
        {
            get => Material.GetFloat(MegaShaderProperty.CustomColors) == 0.0f;
            set => Material.SetFloat(MegaShaderProperty.CustomColors, value ? 0.0f : 1.0f);
        }

        // dependencies
        private readonly IAssetsProvider<Texture2D> _materialsProvider;
        private readonly IAssetsProvider<Texture2D> _patternsProvider;
        private readonly IAssetsProvider<Texture2D> _projectedTexturesProvider;

        // state
        private Ref<UgcElementAsset> _elementRef;
        private Ref<Texture2D>[] _materialRefs;
        private Ref<Texture2D>[] _patternRefs;
        private Ref<Texture2D> _projectedTextureRef;
        private CancellationTokenSource[] _styleCancellations;
        private CancellationTokenSource[] _patternCancellations;
        private CancellationTokenSource _projTexCancellation;
        private RefsCache _textureCache;

        public MegaMaterial(IAssetsProvider<Texture2D> materialsProvider, IAssetsProvider<Texture2D> patternsProvider, Material material)
        {
            if (materialsProvider is null || patternsProvider is null || !material)
            {
                return;
            }

            Material = material;
            _materialsProvider = materialsProvider;
            _patternsProvider = patternsProvider;
            InitializeRegionArrays();
            _projTexCancellation = new CancellationTokenSource();
            _textureCache = new RefsCache();

            IsAlive = true;
        }

        public MegaMaterial(IAssetsProvider<Texture2D> materialsProvider,
                            IAssetsProvider<Texture2D> patternsProvider,
                            IAssetsProvider<Texture2D> projectedTexturesProvider,
                            Ref<UgcElementAsset> elementRef)
        {
            if (!elementRef.IsAlive)
            {
                return;
            }

            if (materialsProvider is null || patternsProvider is null)
            {
                elementRef.Dispose();
                return;
            }

            Material = CreateMaterial(elementRef.Item);
            _materialsProvider = materialsProvider;
            _patternsProvider = patternsProvider;
            _projectedTexturesProvider = projectedTexturesProvider;
            _elementRef = elementRef;
            InitializeRegionArrays();
            _projTexCancellation = new CancellationTokenSource();
            _textureCache = new RefsCache();

            IsAlive = true;
        }

        public async UniTask ApplySplitAsync(Split split)
        {
            await ApplyRegionsAsync(split.Regions);
            await ApplyProjectedTexturesAsync(split.ProjectedTextures);
            UseDefaultColors = split.UseDefaultColors;
        }

        public async UniTask ApplyProjectedTexturesAsync(List<ProjectedTexture> projectedTextures)
        {
            if (_projectedTexturesProvider is null)
            {
                return;
            }

            _projTexCancellation.Cancel();
            _projTexCancellation.Dispose();
            _projTexCancellation = new CancellationTokenSource();
            
            // TODO: composite/bake if there are multiple projected textures
            ProjectedTexture projectedTexture = projectedTextures?.FirstOrDefault();
            if (projectedTexture is null)
            {
                return;
            }

#if UNITY_EDITOR
            Debug.Log($"about to load projected texture {projectedTexture.ProjectionId}");
            Debug.Log($"    from {projectedTexture.ProjectionRemoteUrl}");
#endif
            try
            {
                CancellationToken cancellationToken = _projTexCancellation.Token;
                Ref<Texture2D> textureRef = await _projectedTexturesProvider.LoadAssetAsync(projectedTexture);

                // this ensures that only the last call to this method will be applied. I.e.: there can be two calls but the first
                // texture takes more time to load
                if (cancellationToken.IsCancellationRequested)
                {
                    textureRef.Dispose();
                    return;
                }
                
                // clear previous texture if any
                Material.SetTexture(MegaShaderProperty.DecalAlbedoTransparency, null);
                _projectedTextureRef.Dispose();

                if (!textureRef.IsAlive)
                {
                    Debug.LogError("<color=red>something wrong with downloaded projected texture</color>");
                    return;
                }
                
                Material.SetTexture(MegaShaderProperty.DecalAlbedoTransparency, textureRef.Item);
                _projectedTextureRef = textureRef;
            }
            catch (AggregateException ex)
            {
                Debug.LogError(ex.Message);
            }
        }

        public async UniTask ApplyRegionsAsync(IEnumerable<Region> regions)
        {
            if (!IsAlive || regions is null)
            {
                return;
            }

            // cache all textures temporarily so we avoid unloading any texture that will get loaded by another region
            _textureCache.CacheReferences(_materialRefs, true);
            _textureCache.CacheReferences(_patternRefs,  true);

            await UniTask.WhenAll(regions.Select(ApplyRegionAsync));

            _textureCache.ReleaseCache();
        }

        public async UniTask ApplyRegionAsync(Region region)
        {
            if (!IsAlive || region is null)
            {
                return;
            }

            int regionIndex = region.RegionNumber - 1;
            await ApplyStyleAsync(region.Style, regionIndex);
        }

        public async UniTask ApplyStyleAsync(Style style, int regionIndex)
        {
            if (!IsAlive || style is null || regionIndex < 0 || regionIndex >= RegionCount)
            {
                return;
            }

            try
            {
                CancellationToken cancellationToken = StartNewStyleApplyOperation(regionIndex);

                // try to load the material texture
                Texture2D texture = await LoadRegionTextureAsync(isMaterial: true, style.SurfaceTextureId, regionIndex, cancellationToken);

                if (cancellationToken.IsCancellationRequested)
                {
                    return;
                }

                // apply all style properties to the material
                Material.SetColor(MegaShaderRegionProperty.Color, regionIndex, style.Color);
                Material.SetTexture(MegaShaderRegionProperty.Material, regionIndex, texture);
                Material.SetFloat(MegaShaderRegionProperty.MaterialScale, regionIndex, style.SurfaceScale);

                // apply the style's pattern to the material
                await ApplyPatternAsync(style.Pattern, regionIndex);
            }
            catch (Exception e)
            {
                Debug.LogError($"MegaMaterial's ApplyStyleAsync couldn't apply style for region index {regionIndex} with exception: {e}.");
                return;
            }
        }

        public async UniTask ApplyPatternAsync(Pattern pattern, int regionIndex)
        {
            if (!IsAlive || pattern is null || regionIndex < 0 || regionIndex >= RegionCount)
            {
                return;
            }

            CancellationToken cancellationToken = StartNewPatternApplyOperation(regionIndex);

            // try to load the pattern texture
            Texture2D texture = await LoadRegionTextureAsync(isMaterial: false, pattern.TextureId, regionIndex, cancellationToken);

            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }

            // compute the duo tone flag from the pattern type field
            float duoTone = pattern.Type switch
            {
                PatternType.Textured => 0.0f,
                PatternType.Duotone => 1.0f,
                _ => 0.0f,
            };

            // apply all pattern properties to the material
            Material.SetFloat  (MegaShaderRegionProperty.PatternDuotone,     regionIndex, duoTone);
            Material.SetTexture(MegaShaderRegionProperty.PatternTexture,     regionIndex, texture);
            Material.SetFloat  (MegaShaderRegionProperty.PatternRotation,    regionIndex, pattern.Rotation);
            Material.SetFloat  (MegaShaderRegionProperty.PatternScale,       regionIndex, pattern.Scale);
            Material.SetVector (MegaShaderRegionProperty.PatternOffset,      regionIndex, pattern.Offset);
            Material.SetFloat  (MegaShaderRegionProperty.PatternHue,         regionIndex, pattern.Hue);
            Material.SetFloat  (MegaShaderRegionProperty.PatternSaturation,  regionIndex, pattern.Saturation);
            Material.SetFloat  (MegaShaderRegionProperty.PatternGain,        regionIndex, pattern.Gain);
            Material.SetFloat  (MegaShaderRegionProperty.PatternDuoContrast, regionIndex, pattern.DuoContrast);
            Material.SetColor  (MegaShaderRegionProperty.PatternDuoColor1,   regionIndex, pattern.DuoColor1);
            Material.SetColor  (MegaShaderRegionProperty.PatternDuoColor2,   regionIndex, pattern.DuoColor2);
        }

        /// <summary>
        /// Gets a new MegaMaterial instance from this instance with region debugging colors and the base material.
        /// </summary>
        public async UniTask<MegaMaterial> CreateRegionDebuggingMaterialAsync()
        {
            var megaMaterial = new MegaMaterial(_materialsProvider, _patternsProvider, _projectedTexturesProvider, _elementRef.New());
            megaMaterial.UseDefaultColors = false;

            // set the debugging colors with the default material for all regions
            for (int i = 0; i < RegionCount; ++i)
            {
                Texture2D texture = await megaMaterial.LoadRegionDefaultMaterial(i);
                megaMaterial.Material.SetColor(MegaShaderRegionProperty.Color, i, _regionDebuggingColors[i]);
                megaMaterial.Material.SetTexture(MegaShaderRegionProperty.Material, i, texture);
            }

            return megaMaterial;
        }

        public void Dispose()
        {
            if (!IsAlive)
            {
                return;
            }

            if (Material)
            {
                Object.Destroy(Material);
            }

            Material = null;
            _elementRef.Dispose();
            _projectedTextureRef.Dispose();
            _projTexCancellation.Cancel();
            _projTexCancellation.Dispose();

            for (int i = 0; i < RegionCount; ++i)
            {
                _materialRefs[i].Dispose();
                _patternRefs[i].Dispose();
                _styleCancellations[i].Cancel();
                _styleCancellations[i].Dispose();
                _patternCancellations[i].Cancel();
                _patternCancellations[i].Dispose();
            }

            _materialRefs = null;
            _patternRefs = null;
            _styleCancellations = null;
            _patternCancellations = null;
            _projTexCancellation = null;
            _textureCache.ReleaseCache();
            _textureCache = null;

            IsAlive = false;
        }

        private void InitializeRegionArrays()
        {
            _materialRefs = new Ref<Texture2D>[RegionCount];
            _patternRefs = new Ref<Texture2D>[RegionCount];
            _styleCancellations = new CancellationTokenSource[RegionCount];
            _patternCancellations = new CancellationTokenSource[RegionCount];

            for (int i = 0; i < RegionCount; ++i)
            {
                _styleCancellations[i] = new CancellationTokenSource();
                _patternCancellations[i] = new CancellationTokenSource();
            }
        }

        private static Material CreateMaterial(UgcElementAsset element)
        {
            Material material = GeniesShaders.MegaShader.NewMaterial();

            // set base material properties
            material.SetTexture(MegaShaderProperty.AlbedoTransparency, element.AlbedoTransparency);
            material.SetTexture(MegaShaderProperty.MetallicSmoothness, element.MetallicSmoothness);
            material.SetTexture(MegaShaderProperty.Normal,             element.Normal);
            material.SetTexture(MegaShaderProperty.RGBAMask,           element.RgbaMask);
            material.SetFloat(MegaShaderProperty.CustomColors, 0.0f);

            return material;
        }

        private CancellationToken StartNewStyleApplyOperation(int regionIndex)
        {
            _styleCancellations[regionIndex].Cancel();
            _styleCancellations[regionIndex].Dispose();
            _styleCancellations[regionIndex] = new CancellationTokenSource();
            return _styleCancellations[regionIndex].Token;
        }

        private CancellationToken StartNewPatternApplyOperation(int regionIndex)
        {
            _patternCancellations[regionIndex].Cancel();
            _patternCancellations[regionIndex].Dispose();
            _patternCancellations[regionIndex] = new CancellationTokenSource();
            return _patternCancellations[regionIndex].Token;
        }

        private async UniTask<Texture2D> LoadRegionTextureAsync(bool isMaterial, string key, int regionIndex, CancellationToken cancellationToken = default)
        {
            // set the proper provider and texture refs array depending on the texture type (material or pattern)
            IAssetsProvider<Texture2D> provider;
            Ref<Texture2D>[]           textureRefs;

            if (isMaterial)
            {
                provider = _materialsProvider;
                textureRefs = _materialRefs;
            }
            else
            {
                provider = _patternsProvider;
                textureRefs = _patternRefs;
            }

            if (cancellationToken.IsCancellationRequested)
            {
                textureRefs[regionIndex].Dispose();
                return null;
            }

            // try to load the texture
            Ref<Texture2D> textureRef = default;
            if (!string.IsNullOrEmpty(key))
            {
                try
                {
                    textureRef = await provider.LoadAssetAsync(key);

                    if (cancellationToken.IsCancellationRequested)
                    {
                        // textureRef.Dispose();
                        // textureRef = default;
                    }
                }
                catch (Exception) { } // avoid throwing if something goes wrong with the loading
            }

            // if we couldn't load the material texture then try to load the default material
            if (isMaterial && !textureRef.IsAlive)
            {
                return await LoadRegionDefaultMaterial(regionIndex, cancellationToken);
            }

            // dispose the previous texture (if any)
            textureRefs[regionIndex].Dispose();

            if (!textureRef.IsAlive)
            {
                return null;
            }

            // store the loaded reference for later disposal
            textureRefs[regionIndex] = textureRef;
            return textureRef.Item;
        }

        private async UniTask<Texture2D> LoadRegionDefaultMaterial(int regionIndex, CancellationToken cancellationToken = default)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                _materialRefs[regionIndex].Dispose();
                return null;
            }

            Ref<Texture2D> textureRef = default;

            try
            {
                // the default material is on resources since it is required for the MegaShader to look properly
                textureRef = await ResourcesUtility.LoadAssetAsync<Texture2D>(BaseMaterialResourcePath);

                if (cancellationToken.IsCancellationRequested)
                {
                    textureRef.Dispose();
                    textureRef = default;
                }
            }
            catch (Exception) { }

            // dispose the previous texture (if any)
            _materialRefs[regionIndex].Dispose();

            if (!textureRef.IsAlive)
            {
                return null;
            }

            // store the loaded reference for later disposal
            _materialRefs[regionIndex] = textureRef;
            return textureRef.Item;
        }

    }
}
