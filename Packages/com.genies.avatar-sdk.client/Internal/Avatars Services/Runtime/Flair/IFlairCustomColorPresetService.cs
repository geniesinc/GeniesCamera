using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Genies.Avatars;
using UnityEngine;

namespace Genies.Avatars.Services.Flair
{
    /// <summary>
    /// Defines the contract for managing custom color presets for avatar flair elements such as eyebrows and eyelashes.
    /// This service handles creation, retrieval, and deletion of user-customized color combinations.
    /// </summary>
    public interface IFlairCustomColorPresetService
    {
        public const string CustomEyebrowPrefix = "custom-eyebrow-preset";
        public const string CustomEyelashPrefix = "custom-eyelash-preset";
        public static readonly Dictionary<string, string> ChanelPrefixByType = new Dictionary<string, string>()
        {
            {UnifiedMaterialSlot.Eyebrows, CustomEyebrowPrefix},
            {UnifiedMaterialSlot.Eyelashes, CustomEyelashPrefix},

        };


        /// <summary>
        /// Return true if it is a customized color
        /// </summary>
        /// <param name="guid"></param>
        /// <returns></returns>
        public bool IsCustomColor(string guid);
        /// <summary>
        /// Return a list of saved custom colors saved by a user
        /// </summary>
        /// <param name="category"></param>
        /// <returns></returns>
       public UniTask<List<FlairColorPreset>> TryGetAllCustomColorsByCategory(string category);

        /// <summary>
        /// Get a single custom flair color preset by ID
        /// </summary>
        /// <param name="id">The ID of the custom color preset</param>
        /// <returns>The custom flair color preset or null if not found</returns>
        UniTask<FlairColorPreset> GetCustomColorById(string id);

        /// <summary>
        /// Force refresh the cache for a specific category
        /// </summary>
        /// <param name="category">The category to refresh</param>
        void RefreshCacheForCategory(string category);

        /// <summary>
        /// Delete a custom flair color saved on backend
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public UniTask DeleteCustomFlairColor(string guid, string category);

        /// <summary>
        /// Return a custom flair from the avatar definition if exist
        /// otherwise will return null
        /// </summary>
        /// <param name="category"></param>
        /// <param name="avatarDefinition"></param>
        /// <returns></returns>
        public UniTask<FlairColorPreset> TryGetCustomPresetFromAvatarsDefinition(string category, Naf.AvatarDefinition avatarDefinition);
        /// <summary>
        /// It will save a new custom flair preset or override the current one in the avatar definition
        /// </summary>
        /// <param name="category"></param>
        /// <param name="avatarDefinition"></param>
        /// <param name="colors"></param>
        /// <returns></returns>
        public UniTask<FlairColorPreset> SaveOrCreateCustomPreset(string category, Naf.AvatarDefinition avatarDefinition, string guid,  Color[] colors);
    }
}
