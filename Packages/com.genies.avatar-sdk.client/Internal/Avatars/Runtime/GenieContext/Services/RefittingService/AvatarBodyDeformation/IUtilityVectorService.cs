using Cysharp.Threading.Tasks;

namespace Genies.Avatars
{
    public interface IUtilityVectorService
    {
        UniTask<UtilityVector> LoadAsync(string vectorId);

        UtilMeshName GetUtilityMeshFromAssetCategory(OutfitAsset asset);
    }
}