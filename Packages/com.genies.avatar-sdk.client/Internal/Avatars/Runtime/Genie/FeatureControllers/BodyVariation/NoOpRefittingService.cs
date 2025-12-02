using Cysharp.Threading.Tasks;

namespace Genies.Avatars
{
    public sealed class NoOpRefittingService : IRefittingService
    {
        public UniTask LoadAllVectorsAsync()
            => UniTask.CompletedTask;
        
        public string GetBodyVariationBlendShapeName(string bodyVariation)
            => string.Empty;

        public UniTask AddBodyVariationBlendShapeAsync(OutfitAsset asset, string bodyVariation)
            => UniTask.CompletedTask;

        public UniTask WaitUntilReadyAsync()
            => UniTask.CompletedTask;
    }
}