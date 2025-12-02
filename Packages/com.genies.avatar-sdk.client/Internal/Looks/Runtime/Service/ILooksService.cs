using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Genies.Looks.Models;
using Genies.Services.Model;

namespace Genies.Looks.Service
{
    /// <summary>
    /// Client service for interfacing with the looks apis
    /// </summary>
    public interface ILooksService
    {
        /// <summary>
        /// Fetch recent looks and initialize.
        /// </summary>
        public UniTask Initialize();

        /// <summary>
        /// Gets the last modified look by the user.
        /// </summary>
        public UniTask<LookData> GetLastModifiedLook();

        /// <summary>
        /// Gets the last created look by the user.
        /// </summary>
        public UniTask<LookData> GetLastCreatedLook();

        /// <summary>
        /// Returns the look for a specific id
        /// </summary>
        /// <param name="id"> The look id </param>
        public UniTask<LookData> GetLookForIdAsync(string id);

        /// <summary>
        /// Returns the count of all the available looks.
        /// </summary>
        /// <param name="status"> Optional, if null will return the count of all looks. Else will return the count of the status specified</param>
        public UniTask<int> GetAllLooksCountAsync(Look.StatusEnum? status = null);

        /// <summary>
        /// Returns all the user's looks.
        /// </summary>
        /// <param name="status"> Optional, if null will return the count of all looks. Else will return the count of the status specified</param>
        public UniTask<List<LookData>> GetAllLooksAsync(Look.StatusEnum? status = null);

        /// <summary>
        /// Updates a single look, if the body's fields are null they will be ignored.
        /// </summary>
        /// <param name="updatedData"> Update body </param>
        /// <param name="thumbnail"> the thumbnail byte array </param>
        /// <param name="videoClip"> the video byte array </param>
        /// <returns></returns>
        public UniTask<LookData> UpdateLookAsync(LookData updatedData, byte[] thumbnail, byte[] videoClip);

        /// <summary>
        /// Updates a single look, if the body's fields are null they will be ignored.
        /// </summary>
        /// <param name="updatedData"> Update body </param>
        /// <param name="thumbnailLocalPath"> local path on device to the look thumbnail </param>
        /// <param name="videoClipLocalPath"> local path on device to the look video clip </param>
        /// <returns></returns>
        public UniTask<LookData> UpdateLookAsync(LookData updatedData, string thumbnailLocalPath, string videoClipLocalPath);

        /// <summary>
        /// Creates a new look.
        /// </summary>
        /// <param name="newData"> The body of the new look </param>
        /// <param name="thumbnail"> the thumbnail byte array </param>
        /// <param name="videoClip"> the video byte array </param>
        public UniTask<LookData> CreateLookAsync(LookData newData, byte[] thumbnail, byte[] videoClip);

        /// <summary>
        /// Creates a new look.
        /// </summary>
        /// <param name="newData"> The body of the new look </param>
        /// <param name="thumbnailLocalPath"> local path on device to the look thumbnail </param>
        /// <param name="videoClipLocalPath"> local path on device to the look video clip </param>
        public UniTask<LookData> CreateLookAsync(LookData newData, string thumbnailLocalPath, string videoClipLocalPath);

        /// <summary>
        /// Deletes a look by id.
        /// </summary>
        /// <param name="id"> Id of the look to delete </param>
        public UniTask<bool> DeleteLookAsync(string id);

        /// <summary>
        /// Clears the cached looks
        /// </summary>
        public void ClearCache();
    }
}
