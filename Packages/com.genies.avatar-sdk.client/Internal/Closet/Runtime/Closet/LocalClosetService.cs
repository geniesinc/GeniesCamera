using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Genies.Services.Model;

namespace Genies.Closet
{
    /// <summary>
    /// Local implementation of <see cref="IClosetService"/> that provides no-operation closet functionality.
    /// This service is useful for offline scenarios, testing, or when remote closet functionality is not available.
    /// All methods return empty results or complete without performing any actual operations.
    /// </summary>
    public class LocalClosetService : IClosetService
    {
        /// <inheritdoc cref="IClosetService.GetOwnedNftGuids"/>
        public UniTask<List<string>> GetOwnedNftGuids()
        {
            return UniTask.FromResult(new List<string>());
        }

        /// <summary>
        /// Gets the owned NFT IDs. This method appears to be unused in the current interface.
        /// </summary>
        /// <returns>An empty list of nullable integers.</returns>
        public UniTask<List<int?>> GetOwnedNftIds()
        {
            return UniTask.FromResult(new List<int?>());
        }

        /// <inheritdoc cref="IClosetService.GetOwnedNftInfo"/>
        public UniTask<List<IClosetService.NftInfo>> GetOwnedNftInfo()
        {
            return UniTask.FromResult(new List<IClosetService.NftInfo>());
        }

        /// <inheritdoc cref="IClosetService.GetOwnedUnlockablesInfo"/>
        public UniTask<List<IClosetService.UnlockablesInfo>> GetOwnedUnlockablesInfo()
        {
            return UniTask.FromResult(new List<IClosetService.UnlockablesInfo>());
        }

        /// <inheritdoc cref="IClosetService.GetClosetItems"/>
        public UniTask<ClosetItemResponse> GetClosetItems()
        {
            return default;
        }

        /// <inheritdoc cref="IClosetService.AddWearableToCloset(string, string)"/>
        public UniTask AddWearableToCloset(string wearableId, string fullAssetName)
        {
            return default;
        }

        /// <inheritdoc cref="IClosetService.AddWearableToCloset(string, string, string)"/>
        public UniTask AddWearableToCloset(string wearableId, string fullAssetName, string createdBy)
        {
            return default;
        }

        /// <inheritdoc cref="IClosetService.RemoveWearableFromCloset"/>
        public UniTask RemoveWearableFromCloset(string wearableId, string fullAssetName, string createdBy)
        {
            return UniTask.CompletedTask;
        }

        /// <inheritdoc cref="IClosetService.GetThingsByProtocol"/>
        public UniTask<ClosetItemResponse> GetThingsByProtocol(List<string> protocolIds, string minSdk)
        {
            return default;
        }

        /// <inheritdoc cref="IClosetService.AddThingToCloset"/>
        public UniTask AddThingToCloset(string thingId)
        {
            return default;
        }
    }
}
