using System.Collections.Generic;
using Genies.Assets.Services;

namespace Genies.Avatars
{
    /// <summary>
    /// Context class that groups the dependencies required by the Avatars tech.
    /// </summary>
    public sealed class AvatarsContext
    {
        public IAssetLoader<SpeciesAsset> SpeciesLoader;
        public ISubSpeciesLoader SubSpeciesLoader;
        public IAssetLoader<BlendShapeAsset> BlendShapeLoader;
        public IAssetLoader<BlendShapePresetAsset> BlendShapePresetLoader;
        public ISlottedAssetLoader<MaterialAsset> MaterialLoader;
        public IAssetLoader<ColorAsset> SkinColorLoader;
        public IAssetLoader<Texture2DAsset> TattooLoader;
        public IAssetLoader<Texture2DAsset> MakeupLoader;
        public IAssetLoader<MakeupColorAsset> MakeupColorLoader;
        public ISlottedAssetLoader<FlairAsset> FlairLoader;
        public IOutfitAssetLoader OutfitAssetLoader;
        public IRefittingService RefittingService;
        public IReadOnlyDictionary<string, IOutfitAssetMetadataService> OutfitMetadataServicesBySpecies;

        public AvatarsContext ShallowCopy()
        {
            return this.MemberwiseClone() as AvatarsContext;
        }
    }
}
