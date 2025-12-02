
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading;
using Cysharp.Threading.Tasks;
using Genies.DiskCaching;
using Genies.S3Service.Models;
using Genies.Services.Api;
using Genies.Services.Model;
using Newtonsoft.Json;
using UnityEngine;
using static Genies.CrashReporting.CrashReporter;

namespace Genies.S3Service
{
    /// <summary>
    /// A service for uploading user content to their own s3 bucket, depends on <see cref="ImageApi"/> the name might be misleading but
    /// the <see cref="ImageApi"/> simply provides a pre-signed upload url to the user's s3 bucket see https://docs.aws.amazon.com/AmazonS3/latest/userguide/ShareObjectPreSignedURL.html
    /// the <see cref="ImageApi"/> responds with a "distributionUrl" (this url can be saved and use in the future to retrieve the object from s3). Also the <see cref="ImageApi"/> isn't just for images
    /// it supports any types of assets that we provide.
    ///
    /// This service sits on top of <see cref="ImageApi"/> and provides a way for developers to upload assets and cache them on disk. The caching is used to avoid redundant GETs of those assets.
    /// Items in the cache expire after 30 days if they haven't been used.
    /// </summary>
    public class GeniesS3Service : IGeniesS3Service
    {
        /// <summary>
        /// Async method that returns the currently logged in user's valiud UserID.
        /// </summary>
        public delegate UniTask<string> GetAccountUserId();

        private readonly string _cachePath = Path.Combine(Application.persistentDataPath,                 "S3UploadCache");
        private readonly string _diskCacheMetadataFileName = Path.Combine(Application.persistentDataPath, "S3UploadCache", "cache-metadata.json");
        private readonly IImageApi _imageApi;
        private readonly GetAccountUserId _getAccountUserId;
        private DiskCache _cache;
        private Dictionary<string, UploadRequest> _uploadRequests = new Dictionary<string, UploadRequest>();
        private readonly Dictionary<string, UniTaskCompletionSource<S3DownloadResponse>> _downloadOperations = new();

        public GeniesS3Service(IImageApi imageApi, GetAccountUserId getAccountUserId, DiskCacheOptions cachingOptions)
        {
            _imageApi = imageApi;
            _getAccountUserId = getAccountUserId;

            LoadOrCreateDiskCache(cachingOptions);
            _cache.FindAndClearExpiredCacheEntries();
        }

        /// <summary>
        /// Upload a local file to s3
        /// </summary>
        /// <param name="s3FilePath"> The relative path to the file in s3, must include the file name and extension</param>
        /// <param name="localFilePath"> Path to the file on disk </param>
        /// <returns></returns>
        public async UniTask<string> UploadObject(string s3FilePath, string localFilePath)
        {
            //Ensure file exists before we try to upload it.
            if (!File.Exists(localFilePath))
            {
                LogHandledException(new GeniesS3OperationException($"Tried to upload file that doesn't exist. Path: {localFilePath}"));
                return string.Empty;
            }

            try
            {
                // Read the source file into a byte array.
                using var fsSource       = new FileStream(localFilePath, FileMode.Open, FileAccess.Read);
                var       data           = new byte[fsSource.Length];
                var       numBytesToRead = (int)fsSource.Length;
                var       numBytesRead   = 0;
                while (numBytesToRead > 0)
                {
                    // Read may return anything from 0 to numBytesToRead.
                    var offset = fsSource.Read(data, numBytesRead, numBytesToRead);

                    // Break when there's nothing more to read.
                    if (offset == 0)
                    {
                        break;
                    }

                    numBytesRead += offset;
                    numBytesToRead -= offset;
                }

                //Upload bytes using overload method.
                return await UploadObject(s3FilePath, data, existingFilePath: localFilePath);
            }
            catch (Exception e)
            {
                LogHandledException(new GeniesS3OperationException($"Failed to upload file. Path: {localFilePath}", e));
            }

            return string.Empty;
        }

        /// <summary>
        /// Uploads an objects data to s3
        /// </summary>
        /// <param name="s3FilePath"> The relative path to the file in s3, must include the file name and extension</param>
        /// <param name="data"> the byte data of the file </param>
        /// <param name="existingFilePath"> The path to the file if it exists on disk </param>
        /// <returns> The distributionUrl to download the file later on </returns>
        public async UniTask<string> UploadObject(string s3FilePath, byte[] data, string existingFilePath = null)
        {
            //Ensure concurrency
            if (_uploadRequests.TryGetValue(s3FilePath, out var request))
            {
                request.uploadCancellationTokenSource?.Cancel();
                request.webRequest?.Abort();
                _uploadRequests.Remove(s3FilePath);
            }

            try
            {
                //Track request
                var cancellationTokenSource = new CancellationTokenSource();
                var uploadRequest           = new UploadRequest();
                uploadRequest.uploadCancellationTokenSource = cancellationTokenSource;
                _uploadRequests.Add(s3FilePath, uploadRequest);

                //If cancellation happened before requesting url
                if (cancellationTokenSource.IsCancellationRequested)
                {
                    throw new OperationCanceledException();
                }

                //Get a pre-signed s3 url for uploading the user data
                var imageUploadModel = new ImageUpload(s3FilePath);
                var userId             = await _getAccountUserId();
                var urlResponse      = await _imageApi.ImageUploadAsync(imageUploadModel, userId);
                //Debug.Log($"PresignedUrl: <color=yellow>{urlResponse.PresignedUrl}</color>");
                //Debug.Log($"DistributionUrl: <color=yellow>{urlResponse.DistributionUrl}</color>");

                //If cancellation happened after requesting url
                if (cancellationTokenSource.IsCancellationRequested)
                {
                    throw new OperationCanceledException();
                }

                //Generate request
                var httpRequest = WebRequest.Create(urlResponse.PresignedUrl) as HttpWebRequest;
                httpRequest!.Method = "PUT";

                //Track http request
                uploadRequest.webRequest = httpRequest;

                using var dataStream = httpRequest.GetRequestStream();
                await dataStream.WriteAsync(data, 0, data.Length);

                //Handle response
                using var uploadResponse = await httpRequest.GetResponseAsync() as HttpWebResponse;
                if (uploadResponse!.StatusCode != HttpStatusCode.OK)
                {
                    LogHandledException(new GeniesS3OperationException($"S3 Upload didn't finish with OK status. {uploadResponse.StatusCode}"));
                }
                uploadResponse.Close();

                //Update cache
                var targetDiskPath = existingFilePath;
                if (string.IsNullOrEmpty(targetDiskPath))
                {
                    targetDiskPath = Path.Combine(_cachePath, s3FilePath);
                    var directory = Path.GetDirectoryName(targetDiskPath);
                    if (directory != null)
                    {
                        Directory.CreateDirectory(directory);
                    }
#if UNITY_EDITOR
                    Debug.Log($"<color=yellow>[UploadObject] Saving cached file to disk: {targetDiskPath}</color>");
#endif
                    File.WriteAllBytes(targetDiskPath, data);
                }
                _cache.AddEntry(urlResponse.DistributionUrl, targetDiskPath);

                return urlResponse.DistributionUrl;
            }
            catch (Exception e)
            {
                LogHandledException(new GeniesS3OperationException($"Failed to upload file. Path: {s3FilePath}", e));
            }

            return string.Empty;
        }

        /// <summary>
        /// Downloads a file from a distribution Url.
        /// </summary>
        /// <param name="distributionUrl"> The distribution Url that was first generated with the initial upload</param>
        /// <param name="fileName"> Relative path to the file + it's name and extension</param>
        /// <returns></returns>
        public async UniTask<S3DownloadResponse> DownloadObject(string distributionUrl, string fileName)
        {
            if (string.IsNullOrEmpty(distributionUrl))
            {
                return new S3DownloadResponse(wasDownloaded: false, downloadedFilePath: string.Empty);
            }

            if (_cache.IsObjectCached(distributionUrl))
            {
                _cache.TryGetCachedFilePath(distributionUrl, out var cachedFilePath);
                return  new S3DownloadResponse(wasDownloaded: true, downloadedFilePath: cachedFilePath);
            }

            // avoid downloading to the same file path multiple times (this avoids some rare access violation IO exceptions)
            var savePath  = Path.Combine(_cachePath, fileName);
            if (_downloadOperations.TryGetValue(savePath, out var operation))
            {
                return await operation.Task;
            }

            try
            {
                _downloadOperations[savePath] = operation = new UniTaskCompletionSource<S3DownloadResponse>();

                //Download file
                var directory = Path.GetDirectoryName(savePath);
                Directory.CreateDirectory(directory);

                var uri = new UriBuilder(distributionUrl)
                {
                    Scheme = Uri.UriSchemeHttps,
                    Port = -1
                }.Uri;

                using var client = new WebClient();
#if UNITY_EDITOR
                Debug.Log($"About to download from distribution uri: <color=yellow>{uri}</color>");
                Debug.Log($"Local file savePath: <color=yellow>{savePath}</color>");
#endif
                // TODO FIX: if Download fails, a corrupted file will be left at savePath
                await client.DownloadFileTaskAsync(uri, savePath);

                //Cache download
                _cache.AddEntry(distributionUrl, savePath);
                SaveDiskCacheMetadata();

                _downloadOperations.Remove(savePath);
                var result = new S3DownloadResponse(wasDownloaded: true, downloadedFilePath: savePath);
                operation.TrySetResult(result);
                return result;
            }
            catch (AggregateException e)
            {
                _downloadOperations.Remove(savePath);
                LogHandledException(e);
                var result = new S3DownloadResponse(wasDownloaded: false, downloadedFilePath: null);
                operation.TrySetResult(result);
                return result;
            }
        }

        /// <summary>
        /// Removes an object from cache. Due to a limitation in the API we don't really delete the object from s3.
        /// </summary>
        /// <param name="distributionUrl"> The url used to fetch the object (primary key) </param>
        public void DeleteObject(string distributionUrl)
        {
            _cache.DeleteEntry(distributionUrl);
        }

        /// <summary>
        /// Loads the <see cref="DiskCache"/> json from disk if it exists or creates a new one.
        /// </summary>
        /// <param name="diskCacheOptions"> The options to initialize the cache with </param>
        private void LoadOrCreateDiskCache(DiskCacheOptions diskCacheOptions)
        {
            if (!File.Exists(_diskCacheMetadataFileName))
            {
                _cache = new DiskCache(diskCacheOptions);
                return;
            }

            using var streamReader = new StreamReader(_diskCacheMetadataFileName);
            var       json         = streamReader.ReadToEnd();
            _cache = JsonConvert.DeserializeObject<DiskCache>(json);
            _cache?.SetOptions(diskCacheOptions);
            _cache ??= new DiskCache(diskCacheOptions);
        }

        /// <summary>
        /// Saves the <see cref="DiskCache"/> as a json to disk
        /// </summary>
        private void SaveDiskCacheMetadata()
        {
            var jsonString = JsonConvert.SerializeObject(_cache, Formatting.Indented);
            File.WriteAllText(_diskCacheMetadataFileName, jsonString);
        }
    }
}
