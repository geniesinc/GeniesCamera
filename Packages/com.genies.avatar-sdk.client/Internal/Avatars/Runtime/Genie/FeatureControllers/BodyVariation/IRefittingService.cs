using Cysharp.Threading.Tasks;

namespace Genies.Avatars
{
    public interface IRefittingService
    {
        UniTask LoadAllVectorsAsync();
        string GetBodyVariationBlendShapeName(string bodyVariation);
        UniTask AddBodyVariationBlendShapeAsync(OutfitAsset asset, string bodyVariation);
        UniTask WaitUntilReadyAsync();
    }
}
