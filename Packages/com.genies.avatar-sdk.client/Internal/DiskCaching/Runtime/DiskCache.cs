using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using static Genies.CrashReporting.CrashReporter;

namespace Genies.DiskCaching
{
    /// <summary>
    /// Model for caching uploaded s3 files on disk. Provides methods for getting cached entries
    /// and expiring entries.
    ///
    /// Note: The distributionUrl is the primary key to a cached entry
    /// </summary>
    [Serializable]
    public class DiskCache
    {
        [JsonIgnore]
        private DiskCacheOptions _options;

        [JsonProperty]
        private Dictionary<string, DiskCacheEntry> _entries = new Dictionary<string, DiskCacheEntry>();

        /// <summary>
        /// Keeps track of insertion order for FIFO eviction.
        /// </summary>
        [JsonProperty]
        private LinkedList<string> _insertionOrder = new LinkedList<string>();

        public DiskCache()
        {
            _options = DiskCacheOptions.Default;
        }

        /// <summary>
        /// Construct cache with options
        /// </summary>
        /// <param name="options"> The cache options </param>
        public DiskCache(DiskCacheOptions options)
        {
            _options = options;
        }

        /// <summary>
        /// Property to access all entries in the disk cache
        /// </summary>
        public Dictionary<string, DiskCacheEntry> Entries => _entries;

        /// <summary>
        /// Sets the caching options
        /// </summary>
        public void SetOptions(DiskCacheOptions diskCacheOptions)
        {
            _options = diskCacheOptions;
        }

        /// <summary>
        /// Looks through cached entries and finds the one that have expired based on
        /// <see cref="DiskCacheOptions.cacheExpirationInSeconds"/>
        /// </summary>
        public void FindAndClearExpiredCacheEntries()
        {
            var expiredEntries = new List<DiskCacheEntry>();
            foreach (var entry in _entries)
            {
                var cachedEntry = entry.Value;
                var timeSpan = DateTime.UtcNow - cachedEntry.creationTimestamp;

                if (timeSpan.TotalSeconds >= _options.cacheExpirationInSeconds)
                {
                    expiredEntries.Add(cachedEntry);
                }
            }

            foreach (var expiredEntry in expiredEntries)
            {
                DeleteEntry(expiredEntry.s3DistributionUrl);
            }
        }

        /// <summary>
        /// Deletes the object if it exists on disk.
        /// </summary>
        /// <param name="s3DistributionUrl"> The url to the object on s3 </param>
        /// <param name="shouldDeleteFile"> If the file should be deleted or just the cache metadata </param>
        public void DeleteEntry(string s3DistributionUrl, bool shouldDeleteFile = true)
        {
            if (!_entries.TryGetValue(s3DistributionUrl, out var entry))
            {
                return;
            }

            if (File.Exists(entry.filePath) && shouldDeleteFile)
            {
                File.Delete(entry.filePath);
            }

            _entries.Remove(s3DistributionUrl);
            _insertionOrder.Remove(s3DistributionUrl);
        }

        /// <summary>
        /// Adds a new entry to the cache and evicts the oldest entries if max size is exceeded.
        /// </summary>
        /// <param name="s3DistributionUrl"> The primary key in the cache </param>
        /// <param name="filePath"> The path to the file on disk. </param>
        /// <param name="tag"></param>
        public void AddEntry(string s3DistributionUrl, string filePath, string tag = null)
        {
            // Ensure the file exists on disk
            if (!File.Exists(filePath))
            {
                LogHandledException(new Exception($"Tried to cache a file that doesn't exist: {filePath}"));
                return;
            }

            // If the entry already exists, update the insertion order and skip adding a duplicate
            if (_entries.ContainsKey(s3DistributionUrl))
            {
                _insertionOrder.Remove(s3DistributionUrl);
                _insertionOrder.AddLast(s3DistributionUrl);
                return;
            }

            // Create a new cache entry
            var newEntry = new DiskCacheEntry
            {
                s3DistributionUrl = s3DistributionUrl,
                creationTimestamp = DateTime.UtcNow,
                filePath = filePath,
                tag = tag,
            };

            // Add the entry to the cache and update the insertion order
            _entries[s3DistributionUrl] = newEntry;
            _insertionOrder.AddLast(s3DistributionUrl);

            // Check and enforce the maximum size
            EnforceMaxCacheSize();
        }

        /// <summary>
        /// Enforces the maximum size by removing the oldest entries until the size is under the limit.
        /// </summary>
        /// <param name="s3DistributionUrl"> The url to the object on s3 </param>
        /// <param name="filePathOnDisk"> The returned file path on disk </param>
        /// <returns> True if the file exists and is cached </returns>
        public void EnforceMaxCacheSize()
        {
            if (_options.maxCacheSizeInBytes <= 0)
            {
                return; // No size limit defined
            }

            long totalCacheSize = CalculateTotalCacheSize();

            while (totalCacheSize > _options.maxCacheSizeInBytes && _insertionOrder.Count > 0)
            {
                var oldestKey = _insertionOrder.First.Value;
                if (_entries.TryGetValue(oldestKey, out var entry))
                {
                    totalCacheSize -= GetFileSize(entry.filePath);
                    DeleteEntry(oldestKey);
                }
            }
        }

        /// <summary>
        /// Calculates the total size of all files in the cache.
        /// </summary>
        public long CalculateTotalCacheSize()
        {
            return _entries.Values.Sum(entry => GetFileSize(entry.filePath));
        }

        /// <summary>
        /// Gets the size of a file.
        /// </summary>
        private long GetFileSize(string filePath)
        {
            try
            {
                return File.Exists(filePath) ? new FileInfo(filePath).Length : 0;
            }
            catch
            {
                return 0; // Return 0 if the file does not exist or cannot be accessed
            }
        }

        public bool TryGetCachedFilePath(string s3DistributionUrl, out string filePathOnDisk)
        {
            if (!IsObjectCached(s3DistributionUrl))
            {
                filePathOnDisk = string.Empty;
                return false;
            }

            filePathOnDisk = _entries[s3DistributionUrl].filePath;
            return true;
        }

        /// <summary>
        /// Returns true if the object mapped to the provided <see cref="s3DistributionUrl"/>
        /// is cached on disk or not.
        /// </summary>
        /// <param name="s3DistributionUrl"> The url to the object on s3 </param>
        /// <returns> True if object exists on disk </returns>
        public bool IsObjectCached(string s3DistributionUrl)
        {
            if (!_entries.TryGetValue(s3DistributionUrl, out var entry))
            {
                return false;
            }

            if (File.Exists(entry.filePath))
            {
                return true;
            }

            DeleteEntry(s3DistributionUrl);
            return false;
        }

        public DiskCacheEntry? GetEntry(string key)
        {
            return _entries.TryGetValue(key, out DiskCacheEntry entry) ? entry : null;
        }

        /// <summary>
        /// Saves the DiskCache to a file.
        /// </summary>
        public void SaveToFile(string filePath)
        {
            try
            {
                var json = JsonConvert.SerializeObject(this, Formatting.Indented);
                File.WriteAllText(filePath, json);
            }
            catch (Exception ex)
            {
                LogHandledException(new Exception($"Failed to save disk cache to file: {ex.Message}", ex));
            }
        }

        /// <summary>
        /// Loads the DiskCache from a file.
        /// </summary>
        public static DiskCache LoadFromFile(string filePath, DiskCacheOptions options)
        {
            if (!File.Exists(filePath))
            {
                return new DiskCache(options);
            }

            try
            {
                var json = File.ReadAllText(filePath);
                var diskCache = JsonConvert.DeserializeObject<DiskCache>(json);
                diskCache.SetOptions(options);
                diskCache.ValidateCache();
                return diskCache;
            }
            catch (Exception ex)
            {
                LogHandledException(new Exception($"Failed to load disk cache from file: {ex.Message}", ex));
                return new DiskCache(options);
            }
        }

        /// <summary>
        /// Validates the cache to ensure all cached files exist and removes invalid entries.
        /// </summary>
        public void ValidateCache()
        {
            foreach (var key in _insertionOrder.ToList())
            {
                if (!IsObjectCached(key))
                {
                    _insertionOrder.Remove(key);
                }
            }
        }

        /// <summary>
        /// Clears all files in the cache directory except the serialized cache file.
        /// </summary>
        public void ClearCache(string cacheDirectory, string cacheFilePath)
        {
            try
            {
                var files = Directory.GetFiles(cacheDirectory);
                foreach (var file in files)
                {
                    if (Path.GetFullPath(file) != Path.GetFullPath(cacheFilePath))
                    {
                        File.Delete(file);
                    }
                }

                _entries.Clear();
                _insertionOrder.Clear();
            }
            catch (Exception ex)
            {
                LogHandledException(new Exception($"Failed to clear cache directory: {ex.Message}", ex));
            }
        }

        /// <summary>
        /// Removes files that are not part of the cache entries or the serialized cache file.
        /// </summary>
        public void RemoveExtraneousFiles(string cacheDirectory, string cacheFilePath)
        {
            try
            {
                var files = Directory.GetFiles(cacheDirectory);
                var cachedFilePaths = new HashSet<string>(_entries.Values.Select(entry => Path.GetFullPath(entry.filePath)));

                foreach (var file in files)
                {
                    if (!cachedFilePaths.Contains(Path.GetFullPath(file)) &&
                        Path.GetFullPath(file) != Path.GetFullPath(cacheFilePath))
                    {
                        File.Delete(file);
                    }
                }
            }
            catch (Exception ex)
            {
                LogHandledException(new Exception($"Failed to remove extraneous files: {ex.Message}", ex));
            }
        }

    }
}
