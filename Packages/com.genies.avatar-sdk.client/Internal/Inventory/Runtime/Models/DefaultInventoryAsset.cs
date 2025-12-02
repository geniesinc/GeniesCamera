using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Genies.Models;
using UnityEngine;
using Genies.Services.Model;

namespace Genies.Inventory
{
    public class DefaultInventoryAsset
    {
        public string AssetId;
        public AssetType AssetType;
        public string Name;
        public string Category;
        public List<string> SubCategories;
        public int Order;

        public PipelineData PipelineData;
    }

    public class ColoredInventoryAsset : DefaultInventoryAsset
    {
        public List<Color> Colors;
    }

    public class ColorTaggedInventoryAsset : DefaultInventoryAsset
    {
        public List<string> ColorTags;
    }

    public class DefaultAvatarBaseAsset : DefaultInventoryAsset
    {
        public List<string> Tags;
    }

    public class DefaultAnimationLibraryAsset : DefaultInventoryAsset
    {
        public string MoodsTag;
        public List<string> ProtocolTags;
        public List<DefaultAnimationChildAsset> ChildAssets;
    }

    public class DefaultAnimationChildAsset
    {
        public string AssetId;
        public string ProtocolTag;

        public DefaultAnimationChildAsset(Services.Model.ChildAsset childAsset)
        {
            AssetId = childAsset.Guid;
            ProtocolTag = childAsset.ProtocolTag;
        }
    }

    public class PipelineData
    {
        public long AssetVersion;
        public string PipelineVersion;
        public string ParentId;
        public bool UniversalAvailable;
        public string UniversalBuildVersion;
        public string AssetAddress;

        public PipelineData(PipelineItemV2 pipelineItem)
        {
            AssetVersion = pipelineItem.AssetVersion.HasValue ? pipelineItem.AssetVersion.Value : 0;
            PipelineVersion = pipelineItem.PipelineVersion;
            ParentId = pipelineItem.ParentId;
            UniversalAvailable = pipelineItem.UniversalAvailable.HasValue ? pipelineItem.UniversalAvailable.Value : false;
            UniversalBuildVersion = pipelineItem.UniversalBuildVersion;
            AssetAddress = pipelineItem.AssetAddress;
        }
    }

    public enum AssetType
    {
        WardrobeGear,
        AvatarBase,
        AvatarMakeup,
        Flair,
        AvatarEyes,
        ColorPreset,
        ImageLibrary,
        AnimationLibrary,
        Avatar,
        Decor,
        ModelLibrary
    }
    
    public enum AvatarBaseCategory
    {
        None = 0,
        Lips = 1,
        Jaw = 2,
        Nose = 3,
        Eyes = 4,
        Brow = 5
    }
}
