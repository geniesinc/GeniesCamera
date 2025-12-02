using System;
using System.IO;
using Cysharp.Threading.Tasks;
using Genies.DiskCaching;
using Genies.S3Service.Models;
using Newtonsoft.Json;
using UnityEngine;
using static Genies.CrashReporting.CrashReporter;

namespace Genies.S3Service
{
    /// <summary>
    /// Would've preferred to rename the whole package to something like FileUploadService since `LocalS3` doesn't make too much sense.
    /// But basically this class can be used to mock uploading to s3 when in reality it just stores files to disk.
    /// </summary>
    public class GeniesLocalS3Service : IGeniesS3Service
    {
        private readonly string _cachePath = Path.Combine(Application.persistentDataPath,                 "S3UploadCache");
        private readonly string _diskCacheMetadataFileName = Path.Combine(Application.persistentDataPath, "S3UploadCache", "cache-metadata.json");
        private DiskCache _cache;

        public GeniesLocalS3Service()
        {
            LoadOrCreateDiskCache(new DiskCacheOptions() { cacheExpirationInSeconds = int.MaxValue });
        }

        /// <summary>
        /// Mocks uploading a local file, but actually stores it on disk
        /// </summary>
        /// <param name="s3FilePath"> The relative path to the file in s3, must include the file name and extension</param>
        /// <param name="localFilePath"> Path to the file on disk </param>
        /// <returns></returns>
        public UniTask<string> UploadObject(string s3FilePath, string localFilePath)
        {
            //Ensure file exists before we try to upload it.
            if (!File.Exists(localFilePath))
            {
                LogHandledException(new GeniesS3OperationException($"Tried to upload file that doesn't exist. Path: {localFilePath}"));
                return UniTask.FromResult(string.Empty);
            }

            _cache.AddEntry(localFilePath, localFilePath);
            //File already exists so no need to do anything.
            return UniTask.FromResult(localFilePath);
        }

        /// <summary>
        /// Uploads an objects data to s3
        /// </summary>
        /// <param name="s3FilePath"> The relative path to the file in s3, must include the file name and extension</param>
        /// <param name="data"> the byte data of the file </param>
        /// <param name="existingFilePath"> The path to the file if it exists on disk </param>
        /// <returns> The distributionUrl to download the file later on </returns>
        public UniTask<string> UploadObject(string s3FilePath, byte[] data, string existingFilePath = null)
        {
            try
            {
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

                    File.WriteAllBytes(targetDiskPath, data);
                }

                _cache.AddEntry(targetDiskPath, targetDiskPath);

                return UniTask.FromResult(targetDiskPath);
            }
            catch (Exception e)
            {
                LogHandledException(new GeniesS3OperationException($"Failed to upload file. Path: {s3FilePath}", e));
            }

            return UniTask.FromResult(string.Empty);
        }

        /// <summary>
        /// Downloads a file from a distribution Url.
        /// </summary>
        /// <param name="distributionUrl"> The distribution Url that was first generated with the initial upload</param>
        /// <param name="fileName"> Relative path to the file + it's name and extension</param>
        /// <returns></returns>
        public UniTask<S3DownloadResponse> DownloadObject(string distributionUrl, string fileName)
        {
            if (string.IsNullOrEmpty(distributionUrl))
            {
                return UniTask.FromResult(new S3DownloadResponse(wasDownloaded: false, downloadedFilePath: string.Empty));
            }

            if (_cache.IsObjectCached(distributionUrl))
            {
                _cache.TryGetCachedFilePath(distributionUrl, out var cachedFilePath);
                return UniTask.FromResult(new S3DownloadResponse(wasDownloaded: true, downloadedFilePath: cachedFilePath));
            }

            return UniTask.FromResult(new S3DownloadResponse(wasDownloaded: false, downloadedFilePath: string.Empty));
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
