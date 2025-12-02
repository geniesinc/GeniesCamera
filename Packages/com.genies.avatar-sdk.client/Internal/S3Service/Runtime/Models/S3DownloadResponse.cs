namespace Genies.S3Service.Models
{
    public struct S3DownloadResponse
    {
        public bool wasDownloaded;
        public string downloadedFilePath;

        public S3DownloadResponse(bool wasDownloaded, string downloadedFilePath)
        {
            this.wasDownloaded = wasDownloaded;
            this.downloadedFilePath = downloadedFilePath;
        }
    }
}
