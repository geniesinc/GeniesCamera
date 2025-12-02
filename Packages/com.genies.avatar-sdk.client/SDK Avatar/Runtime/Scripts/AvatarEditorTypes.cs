using System;
using System.Collections.Generic;
using UnityEngine;

namespace Genies.Sdk
{
    /// <summary>
    /// Wrapper for wearable asset information
    /// </summary>
    [Serializable]
    public class WearableAssetInfo : IDisposable
    {
        public string AssetId;
        public AssetType AssetType;
        public string Name;
        public string Category;
        public Sprite Icon;

        private Action OnDisposed;

        public WearableAssetInfo(string assetId, AssetType assetType, string name, string category, Sprite icon, Action onDisposed)
        {
            AssetId = assetId;
            AssetType = assetType;
            Name = name;
            Category = category;
            Icon = icon;
            OnDisposed = onDisposed;
        }

        public void Dispose()
        {
            OnDisposed?.Invoke();
        }

        /// <summary>
        /// Creates from the internal AvatarEditor WearableAssetInfo type
        /// </summary>
        internal static WearableAssetInfo FromInternal(Genies.AvatarEditor.Core.WearableAssetInfo internalAsset)
        {
            return new WearableAssetInfo(
                internalAsset.AssetId,
                EnumMapper.FromInternal(internalAsset.AssetType),
                internalAsset.Name,
                internalAsset.Category,
                internalAsset.Icon.Item,
                internalAsset.Dispose
            );
        }

        /// <summary>
        /// Converts a list from internal types to SDK types
        /// </summary>
        internal static List<WearableAssetInfo> FromInternalList(List<Genies.AvatarEditor.Core.WearableAssetInfo> internalList)
        {
            var result = new List<WearableAssetInfo>();
            foreach (var item in internalList)
            {
                result.Add(FromInternal(item));
            }

            return result;
        }
    }

    /// <summary>
    /// Asset types available in the avatar system
    /// </summary>
    public enum AssetType
    {
        WardrobeGear = 0,
        AvatarBase = 1,
        AvatarMakeup = 2,
        Flair = 3,
        AvatarEyes = 4,
        ColorPreset = 5,
        ImageLibrary = 6,
        AnimationLibrary = 7,
        Avatar = 8,
        Decor = 9,
        ModelLibrary = 10
    }

    /// <summary>
    /// Gender types for avatar body configuration
    /// </summary>
    public enum GenderType
    {
        Male,
        Female,
        Androgynous
    }

    /// <summary>
    /// Body size types for avatar body configuration
    /// </summary>
    public enum BodySize
    {
        Skinny,
        Medium,
        Heavy
    }

    /// <summary>
    /// Internal utility class for mapping SDK enums to internal assembly enums.
    /// This provides stable mapping that doesn't rely on enum ordinal positions.
    /// </summary>
    internal static class EnumMapper
    {
        /// <summary>
        /// Maps SDK GenderType to AvatarEditor GenderType using explicit name-based mapping.
        /// </summary>
        internal static Genies.AvatarEditor.Core.GenderType ToInternal(GenderType genderType)
        {
            return genderType switch
            {
                GenderType.Male => Genies.AvatarEditor.Core.GenderType.Male,
                GenderType.Female => Genies.AvatarEditor.Core.GenderType.Female,
                GenderType.Androgynous => Genies.AvatarEditor.Core.GenderType.Androgynous,
                _ => throw new System.ArgumentException($"Unknown GenderType: {genderType}")
            };
        }

        /// <summary>
        /// Maps SDK BodySize to AvatarEditor BodySize using explicit name-based mapping.
        /// </summary>
        internal static Genies.AvatarEditor.Core.BodySize ToInternal(BodySize bodySize)
        {
            return bodySize switch
            {
                BodySize.Skinny => Genies.AvatarEditor.Core.BodySize.Skinny,
                BodySize.Medium => Genies.AvatarEditor.Core.BodySize.Medium,
                BodySize.Heavy => Genies.AvatarEditor.Core.BodySize.Heavy,
                _ => throw new System.ArgumentException($"Unknown BodySize: {bodySize}")
            };
        }

        /// <summary>
        /// Maps SDK AssetType to Inventory AssetType using explicit name-based mapping.
        /// </summary>
        internal static Genies.Inventory.AssetType ToInternal(AssetType assetType)
        {
            return assetType switch
            {
                AssetType.WardrobeGear => Genies.Inventory.AssetType.WardrobeGear,
                AssetType.AvatarBase => Genies.Inventory.AssetType.AvatarBase,
                AssetType.AvatarMakeup => Genies.Inventory.AssetType.AvatarMakeup,
                AssetType.Flair => Genies.Inventory.AssetType.Flair,
                AssetType.AvatarEyes => Genies.Inventory.AssetType.AvatarEyes,
                AssetType.ColorPreset => Genies.Inventory.AssetType.ColorPreset,
                AssetType.ImageLibrary => Genies.Inventory.AssetType.ImageLibrary,
                AssetType.AnimationLibrary => Genies.Inventory.AssetType.AnimationLibrary,
                AssetType.Avatar => Genies.Inventory.AssetType.Avatar,
                AssetType.Decor => Genies.Inventory.AssetType.Decor,
                AssetType.ModelLibrary => Genies.Inventory.AssetType.ModelLibrary,
                _ => throw new System.ArgumentException($"Unknown AssetType: {assetType}")
            };
        }

        /// <summary>
        /// Maps Inventory AssetType to SDK AssetType using explicit name-based mapping.
        /// </summary>
        internal static AssetType FromInternal(Genies.Inventory.AssetType assetType)
        {
            return assetType switch
            {
                Genies.Inventory.AssetType.WardrobeGear => AssetType.WardrobeGear,
                Genies.Inventory.AssetType.AvatarBase => AssetType.AvatarBase,
                Genies.Inventory.AssetType.AvatarMakeup => AssetType.AvatarMakeup,
                Genies.Inventory.AssetType.Flair => AssetType.Flair,
                Genies.Inventory.AssetType.AvatarEyes => AssetType.AvatarEyes,
                Genies.Inventory.AssetType.ColorPreset => AssetType.ColorPreset,
                Genies.Inventory.AssetType.ImageLibrary => AssetType.ImageLibrary,
                Genies.Inventory.AssetType.AnimationLibrary => AssetType.AnimationLibrary,
                Genies.Inventory.AssetType.Avatar => AssetType.Avatar,
                Genies.Inventory.AssetType.Decor => AssetType.Decor,
                Genies.Inventory.AssetType.ModelLibrary => AssetType.ModelLibrary,
                _ => throw new System.ArgumentException($"Unknown AssetType: {assetType}")
            };
        }
    }
}
