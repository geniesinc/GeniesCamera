using Cysharp.Threading.Tasks;

namespace Genies.Ugc
{
    /// <summary>
    /// Must be able to fetch a <see cref="Wearable"/> instance from a given ID.
    /// </summary>
    public interface IUgcWearableDefinitionService
    {
        UniTask<Wearable> FetchAsync(string wearableId);
    }
}
