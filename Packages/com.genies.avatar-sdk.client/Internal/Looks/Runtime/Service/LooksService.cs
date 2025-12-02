using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Genies.S3Service;
using Genies.DataRepositoryFramework;
using Genies.Looks.Models;
using Genies.Services.Model;

namespace Genies.Looks.Service
{
    /// <summary>
    /// Concrete implementation of <see cref="ILooksService"/> that provides look management functionality.
    /// This service handles creating, updating, deleting, and retrieving avatar looks, including media upload to S3.
    /// </summary>
    public class LooksService : ILooksService
    {
        private readonly MemoryCachedDataRepository<LookData> _dataRepository;
        private readonly IGeniesS3Service _s3Service;
        private readonly bool _isLocal;

        /// <summary>
        /// Initializes a new instance of the <see cref="LooksService"/> class with the specified configuration.
        /// </summary>
        /// <param name="s3Service">The S3 service for uploading and managing look media files.</param>
        /// <param name="isLocal">Whether to use local storage instead of remote API endpoints.</param>
        /// <param name="dataRepository">Optional custom data repository; if null, uses default based on isLocal flag.</param>
        public LooksService(IGeniesS3Service s3Service, bool isLocal, IDataRepository<LookData> dataRepository = null)
        {
            _s3Service = s3Service;
            _isLocal = isLocal;

            if (dataRepository != null)
            {
                _dataRepository = new MemoryCachedDataRepository<LookData>(dataRepository, data => data.Id);
            }
            else
            {
                IDataRepository<LookData> actualRepository;
                if (!isLocal)
                {
                    if (s3Service == null)
                    {
                        throw new LookServiceException("Can't pass a null s3 service for non local looks service");
                    }

                    actualRepository = new LooksApiDataRepository(new LooksApiPathResolver());
                }
                else
                {
                    _s3Service ??= new GeniesLocalS3Service();
                    actualRepository = new LocalDiskDataRepository<LookData>("LooksDataLocal", data => data.Id);
                }

                _dataRepository = new MemoryCachedDataRepository<LookData>(actualRepository, data => data.Id);
            }
        }

        private async UniTask<string> GenerateValidGuid()
        {
            var guid       = Guid.NewGuid().ToString();
            var currentIds = await _dataRepository.GetIdsAsync();

            while (currentIds.Contains(guid))
            {
                guid = Guid.NewGuid().ToString();
            }

            return guid;
        }

        /// <inheritdoc />
        public async UniTask Initialize()
        {
            _dataRepository.ClearCache();
            await _dataRepository.GetAllAsync();
        }

        /// <summary>
        /// Gets the last modified look by the user.
        /// </summary>
        public async UniTask<LookData> GetLastModifiedLook()
        {
            var allData      = await _dataRepository.GetAllAsync();
            var lastModified = allData.OrderByDescending(d => d.LastModified).FirstOrDefault();
            return lastModified;
        }

        /// <summary>
        /// Gets the last created look by the user.
        /// </summary>
        public async UniTask<LookData> GetLastCreatedLook()
        {
            var allData     = await _dataRepository.GetAllAsync();
            var lastCreated = allData.OrderByDescending(d => d.CreatedAt).FirstOrDefault();
            return lastCreated;
        }

        /// <summary>
        /// Returns the look for a specific id
        /// </summary>
        /// <param name="id"> The look id </param>
        public async UniTask<LookData> GetLookForIdAsync(string id)
        {
            return await _dataRepository.GetByIdAsync(id);
        }

        /// <summary>
        /// Returns the count of all the available looks.
        /// </summary>
        /// <param name="status"> Optional, if null will return the count of all looks. Else will return the count of the status specified</param>
        public async UniTask<int> GetAllLooksCountAsync(Look.StatusEnum? status = null)
        {
            var looks = await GetAllLooksAsync(status);
            return looks?.Count ?? 0;
        }

        /// <summary>
        /// Returns all the user's looks.
        /// </summary>
        /// <param name="status"> Optional, if null will return the count of all looks. Else will return the count of the status specified</param>
        public async UniTask<List<LookData>> GetAllLooksAsync(Look.StatusEnum? status = null)
        {
            var looks = await _dataRepository.GetAllAsync();

            if (looks?.Count > 0 && status != null)
            {
                return looks.Where(l => l.Status == status).ToList();
            }

            return looks;
        }

        /// <summary>
        /// Updates a single look, if the body's fields are null they will be ignored.
        /// </summary>
        /// <param name="updatedData"> Update body </param>
        /// <param name="thumbnail"> the thumbnail byte array </param>
        /// <param name="videoClip"> the video byte array </param>
        /// <returns></returns>
        public async UniTask<LookData> UpdateLookAsync(LookData updatedData, byte[] thumbnail, byte[] videoClip = null)
        {
            //Upload to s3
            updatedData = await UploadLooksMediaAndUpdateLook(updatedData, thumbnail, videoClip);

            //Store data
            return await _dataRepository.UpdateAsync(updatedData);
        }

        /// <summary>
        /// Updates a single look, if the body's fields are null they will be ignored.
        /// </summary>
        /// <param name="updatedData"> Update body </param>
        /// <param name="thumbnailLocalPath"> local path on device to the look thumbnail </param>
        /// <param name="videoClipLocalPath"> local path on device to the look video clip </param>
        /// <returns></returns>
        public async UniTask<LookData> UpdateLookAsync(LookData updatedData, string thumbnailLocalPath, string videoClipLocalPath = null)
        {
            //Upload to s3
            updatedData = await UploadLooksMediaAndUpdateLook(updatedData, thumbnailLocalPath, videoClipLocalPath);

            //Local service should handle creating the id.
            if (_isLocal)
            {
                if (string.IsNullOrEmpty(updatedData.Id))
                {
                    updatedData.Id = await GenerateValidGuid();
                }
            }

            //Store data
            return await _dataRepository.UpdateAsync(updatedData);
        }

        /// <summary>
        /// Creates a new look.
        /// </summary>
        /// <param name="newData"> The body of the new look </param>
        /// <param name="thumbnail"> the thumbnail byte array </param>
        /// <param name="videoClip"> the video byte array </param>
        public async UniTask<LookData> CreateLookAsync(LookData newData, byte[] thumbnail, byte[] videoClip = null)
        {
            //Local service should handle creating the id.
            if (_isLocal)
            {
                if (string.IsNullOrEmpty(newData.Id))
                {
                    newData.Id = await GenerateValidGuid();
                }
            }

            //Upload to s3
            newData = await UploadLooksMediaAndUpdateLook(newData, thumbnail, videoClip);

            //Store data
            return await _dataRepository.CreateAsync(newData);
        }

        /// <summary>
        /// Creates a new look.
        /// </summary>
        /// <param name="newData"> The body of the new look </param>
        /// <param name="thumbnailLocalPath"> local path on device to the look thumbnail </param>
        /// <param name="videoClipLocalPath"> local path on device to the look video clip </param>
        public async UniTask<LookData> CreateLookAsync(LookData newData, string thumbnailLocalPath, string videoClipLocalPath = null)
        {
            //Local service should handle creating the id.
            if (_isLocal)
            {
                newData.Id ??= await GenerateValidGuid();
            }

            //Upload to s3
            newData = await UploadLooksMediaAndUpdateLook(newData, thumbnailLocalPath, videoClipLocalPath);

            //Store data
            return await _dataRepository.CreateAsync(newData);
        }

        /// <summary>
        /// Returns the path to the look thumbnail on s3
        /// </summary>
        /// <param name="lookId"> The id of the look </param>
        private string GetThumbnailUploadPath(string lookId)
        {
            return $"Looks/{lookId}/thumbnail/{lookId}.png";
        }

        /// <summary>
        /// Returns the path to the look video on s3
        /// </summary>
        /// <param name="lookId"> The id of the look </param>
        private string GetVideoUploadPath(string lookId)
        {
            return $"Looks/{lookId}/video/{lookId}.mp4";
        }

        /// <summary>
        /// Upload files to s3 and generate a signed url for each which will be stored in the look data model
        /// </summary>
        /// <param name="data"> the look data </param>
        /// <param name="thumbnailLocalPath"> thumbnail path on disk </param>
        /// <param name="videoClipLocalPath"> video path on disk </param>
        /// <returns></returns>
        private async UniTask<LookData> UploadLooksMediaAndUpdateLook(LookData data, string thumbnailLocalPath, string videoClipLocalPath = null)
        {
            var thumbnailUploadTask = new UniTask<string>(string.Empty);
            var videoUploadTask     = new UniTask<string>(string.Empty);

            if (!string.IsNullOrEmpty(thumbnailLocalPath))
            {
                var thumbnailPath = GetThumbnailUploadPath(data.Id);
                thumbnailUploadTask = _s3Service.UploadObject(thumbnailPath, thumbnailLocalPath);
            }

            if (!string.IsNullOrEmpty(videoClipLocalPath))
            {
                var videoPath = GetVideoUploadPath(data.Id);
                videoUploadTask = _s3Service.UploadObject(videoPath, videoClipLocalPath);
            }

            var (thumbnailUrl, videoUrl) = await UniTask.WhenAll(thumbnailUploadTask, videoUploadTask);

            data.ThumbnailUrl = thumbnailUrl;
            data.VideoUrl = videoUrl;
            return data;
        }

        /// <summary>
        /// Upload files to s3 and generate a signed url for each which will be stored in the look data model
        /// </summary>
        /// <param name="data"> the look data </param>
        /// <param name="thumbnail"> thumbnail bytes </param>
        /// <param name="videoClip"> video bytes </param>
        /// <returns></returns>
        private async UniTask<LookData> UploadLooksMediaAndUpdateLook(LookData data, byte[] thumbnail, byte[] videoClip = null)
        {
            var thumbnailUploadTask = new UniTask<string>(string.Empty);
            var videoUploadTask     = new UniTask<string>(string.Empty);

            if (thumbnail != null && thumbnail.Length > 0)
            {
                var thumbnailPath = GetThumbnailUploadPath(data.Id);
                thumbnailUploadTask = _s3Service.UploadObject(thumbnailPath, thumbnail);
            }

            if (videoClip != null && videoClip.Length > 0)
            {
                var videoPath = GetVideoUploadPath(data.Id);
                videoUploadTask = _s3Service.UploadObject(videoPath, videoClip);
            }

            var (thumbnailUrl, videoUrl) = await UniTask.WhenAll(thumbnailUploadTask, videoUploadTask);

            data.ThumbnailUrl = thumbnailUrl;
            data.VideoUrl = videoUrl;
            return data;
        }

        /// <summary>
        /// Deletes a look by id.
        /// </summary>
        /// <param name="id"> Id of the look to delete </param>
        public async UniTask<bool> DeleteLookAsync(string id)
        {
            return await _dataRepository.DeleteAsync(id);
        }

        /// <inheritdoc />
        public void ClearCache()
        {
            _dataRepository.ClearCache();
        }
    }
}
