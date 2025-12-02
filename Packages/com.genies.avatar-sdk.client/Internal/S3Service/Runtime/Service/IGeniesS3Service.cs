using Cysharp.Threading.Tasks;
using Genies.S3Service.Models;

namespace Genies.S3Service
{
    public interface IGeniesS3Service
    {
        /// <summary>
        /// Upload a local file to s3
        /// </summary>
        /// <param name="s3FilePath"> The relative path to the file in s3, must include the file name and extension</param>
        /// <param name="localFilePath"> Path to the file on disk </param>
        /// <returns></returns>
        UniTask<string> UploadObject(string s3FilePath, string localFilePath);

        /// <summary>
        /// Uploads an objects data to s3 
        /// </summary>
        /// <param name="s3FilePath"> The relative path to the file in s3, must include the file name and extension</param>
        /// <param name="data"> the byte data of the file </param>
        /// <param name="existingFilePath"> The path to the file if it exists on disk </param>
        /// <returns> The distributionUrl to download the file later on </returns>
        UniTask<string> UploadObject(string s3FilePath, byte[] data, string existingFilePath = null);

        /// <summary>
        /// Downloads a file from a distribution Url.
        /// </summary>
        /// <param name="distributionUrl"> The distribution Url that was first generated with the initial upload</param>
        /// <param name="fileName"> Relative path to the file + it's name and extension</param>
        /// <returns></returns>
        UniTask<S3DownloadResponse> DownloadObject(string distributionUrl, string fileName);

        /// <summary>
        /// Removes an object from cache. Due to a limitation in the API we don't really delete the object from s3.
        /// </summary>
        /// <param name="distributionUrl"> The url used to fetch the object (primary key) </param>
        void DeleteObject(string distributionUrl);
    }
}
