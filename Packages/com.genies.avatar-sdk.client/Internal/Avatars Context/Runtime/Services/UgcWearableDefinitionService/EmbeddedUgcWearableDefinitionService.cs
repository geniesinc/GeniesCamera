using Cysharp.Threading.Tasks;
using Genies.Avatars;
using IUgcWearableDefinitionService = Genies.Ugc.IUgcWearableDefinitionService;

namespace Genies.Avatars.Context
{
    /// <summary>
    /// Fetches <see cref="Wearable"/>s from the <see cref="AvatarEmbeddedData"/>.
    /// </summary>
    public sealed class EmbeddedUgcWearableDefinitionService : IUgcWearableDefinitionService
    {
        public UniTask<Ugc.Wearable> FetchAsync(string wearableId)
        {
            if (AvatarEmbeddedData.TryGetData(wearableId, out Ugc.Wearable wearable))
            {
                return UniTask.FromResult(wearable);
            }

            return UniTask.FromResult<Ugc.Wearable>(null);
        }
    }
}
