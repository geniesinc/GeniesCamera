using System;

namespace Genies.DiskCaching
{
    /// <summary>
    /// A model for a cached file on disk.
    /// </summary>
    [Serializable]
    public struct DiskCacheEntry
    {
        public string s3DistributionUrl;
        public string filePath;
        public DateTime creationTimestamp;
        public string tag; // Tag to group or categorize related entries (ex. buildId)
    }
}
