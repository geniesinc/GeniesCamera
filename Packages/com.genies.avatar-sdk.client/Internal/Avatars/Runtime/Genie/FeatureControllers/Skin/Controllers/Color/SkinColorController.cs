using System;
using Cysharp.Threading.Tasks;
using Genies.Refs;
using UnityEngine;
using Genies.Assets.Services;

namespace Genies.Avatars
{
    /// <summary>
    /// Controls the skin color on a <see cref="MegaSkinGenieMaterial"/> instance.
    /// </summary>
    public sealed class SkinColorController : ISkinColorController, IDisposable
    {
        private static readonly int _skinColorPropertyId = Shader.PropertyToID("_SkinColor");

        public ColorAsset CurrentColor { get; private set; }

        public event Action Updated;

        // dependencies
        private readonly MegaSkinGenieMaterial _skinMaterial;
        private readonly IAssetLoader<ColorAsset> _colorLoader;

        // state
        private readonly Color _originalColor;

        public SkinColorController(MegaSkinGenieMaterial skinMaterial, IAssetLoader<ColorAsset> colorLoader)
        {
            _skinMaterial = skinMaterial;
            _colorLoader = colorLoader;

            _originalColor = skinMaterial.NonBakedMaterial.GetColor(_skinColorPropertyId);
            CurrentColor = new ColorAsset($"unknown-{_originalColor.ToString()}", _originalColor);
        }

        /// <inheritdoc/>
        public async UniTask LoadAndSetSkinColorAsync(string assetId)
        {
            if (assetId is null || assetId == CurrentColor?.Id)
            {
                return;
            }

            // we don't need to keep this ref alive as the ColorAsset doesn't really allocate releasable assets
            using Ref<ColorAsset> assetRef = await _colorLoader.LoadAsync(assetId);
            SetSkinColor(assetRef.Item);
        }

        /// <inheritdoc/>
        public void SetSkinColor(ColorAsset colorAsset)
        {
            if (colorAsset?.Id is null || CurrentColor?.Id == colorAsset.Id)
            {
                return;
            }

            CurrentColor = colorAsset;
            _skinMaterial.NonBakedMaterial.SetColor(_skinColorPropertyId, colorAsset.Color);
            _skinMaterial.NotifyUpdate();

            Updated?.Invoke();
        }

        /// <inheritdoc/>
        public void SetSkinColor(Color color)
        {
            string assetId = $"color: #{ColorUtility.ToHtmlStringRGBA(color)}";
            var colorAsset = new ColorAsset(assetId, color);
            SetSkinColor(colorAsset);
        }

        /// <inheritdoc/>
        public bool IsColorEquipped(string assetId)
        {
            return assetId == CurrentColor?.Id;
        }

        public void Dispose()
        {
            Updated = null;
            CurrentColor = null;

            if (!_skinMaterial.NonBakedMaterial)
            {
                return;
            }

            _skinMaterial.NonBakedMaterial.SetColor(_skinColorPropertyId, _originalColor);
            _skinMaterial.NotifyUpdate();
        }
    }
}
