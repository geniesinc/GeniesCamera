using System.Net;
using System.Threading;

namespace Genies.S3Service.Models
{
    internal class UploadRequest
    {
        public CancellationTokenSource uploadCancellationTokenSource;
        public HttpWebRequest webRequest;
    }
}
