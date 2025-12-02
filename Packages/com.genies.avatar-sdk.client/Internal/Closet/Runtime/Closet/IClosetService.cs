using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Genies.Services.Model;

namespace Genies.Closet
{
    /// <summary>
    /// Defines the contract for managing user closet operations including NFTs, wearables, and collectible items.
    /// This interface provides methods for retrieving, adding, and removing items from a user's virtual closet.
    /// </summary>
    public interface IClosetService
    {
        /// <summary>
        /// Retrieves the GUIDs of all NFTs owned by the current user.
        /// </summary>
        /// <returns>A task that completes with a list of NFT GUIDs owned by the user.</returns>
        UniTask<List<string>> GetOwnedNftGuids();
        
        /// <summary>
        /// Retrieves detailed information about all NFTs owned by the current user.
        /// </summary>
        /// <returns>A task that completes with a list of NFT information including GUIDs and IDs.</returns>
        UniTask<List<NftInfo>> GetOwnedNftInfo();
        
        /// <summary>
        /// Retrieves information about all unlockable items owned by the current user.
        /// Unlockables are special reward-type NFTs that can be earned through gameplay or achievements.
        /// </summary>
        /// <returns>A task that completes with a list of unlockable item information.</returns>
        UniTask<List<UnlockablesInfo>> GetOwnedUnlockablesInfo();
        
        /// <summary>
        /// Retrieves all items in the user's closet including wearables, NFTs, and things.
        /// </summary>
        /// <returns>A task that completes with a response containing all closet items.</returns>
        UniTask<ClosetItemResponse> GetClosetItems();
        
        /// <summary>
        /// Adds a wearable item to the user's closet.
        /// </summary>
        /// <param name="wearableId">The unique identifier of the wearable item.</param>
        /// <param name="fullAssetName">The full asset name of the wearable.</param>
        /// <returns>A task that completes when the wearable has been added to the closet.</returns>
        UniTask AddWearableToCloset(string wearableId, string fullAssetName);
        
        /// <summary>
        /// Adds a wearable item to the user's closet with a specific creator.
        /// This overload can be used for externally created wearables.
        /// </summary>
        /// <param name="wearableId">The unique identifier of the wearable item.</param>
        /// <param name="fullAssetName">The full asset name of the wearable.</param>
        /// <param name="createdBy">The identifier of who created this wearable.</param>
        /// <returns>A task that completes when the wearable has been added to the closet.</returns>
        UniTask AddWearableToCloset(string wearableId, string fullAssetName, string createdBy);
        
        /// <summary>
        /// Removes a wearable item from the user's closet.
        /// </summary>
        /// <param name="wearableId">The unique identifier of the wearable item.</param>
        /// <param name="fullAssetName">The full asset name of the wearable.</param>
        /// <param name="createdBy">The identifier of who created this wearable.</param>
        /// <returns>A task that completes when the wearable has been removed from the closet.</returns>
        UniTask RemoveWearableFromCloset(string wearableId, string fullAssetName, string createdBy);
        
        /// <summary>
        /// Retrieves all "Things" (collectible items) that match the specified protocol IDs.
        /// Things are special collectible items with specific protocol associations.
        /// </summary>
        /// <param name="protocolIds">List of protocol IDs to filter by.</param>
        /// <param name="minSdk">Minimum SDK version required.</param>
        /// <returns>A task that completes with a response containing matching Things.</returns>
        UniTask<ClosetItemResponse> GetThingsByProtocol(List<string> protocolIds, string minSdk);
        
        /// <summary>
        /// Adds a Thing (collectible item) to the user's closet.
        /// </summary>
        /// <param name="thingId">The unique identifier of the Thing to add.</param>
        /// <returns>A task that completes when the Thing has been added to the closet.</returns>
        UniTask AddThingToCloset(string thingId);
        
        /// <summary>
        /// Contains information about an NFT including its GUID and optional numeric ID.
        /// </summary>
        public struct NftInfo
        {
            /// <summary>
            /// The globally unique identifier of the NFT.
            /// </summary>
            public string Guid;
            
            /// <summary>
            /// The optional numeric identifier of the NFT.
            /// </summary>
            public int? Id;
        }

        /// <summary>
        /// Contains information about unlockable items including GUID and ID.
        /// Unlockables are special reward-type items that can be earned.
        /// </summary>
        public struct UnlockablesInfo
        {
            /// <summary>
            /// The globally unique identifier of the unlockable item.
            /// </summary>
            public string Guid;
            
            /// <summary>
            /// The string identifier of the unlockable item.
            /// </summary>
            public string Id;
        }
    }
}
