
using Cysharp.Threading.Tasks;

namespace Genies.Utilities
{
    public interface IJWTDecoderService
    {
        /// <summary>
        ///  Responsible to return and object with the data from the JWT token after decoded and serialized
        /// </summary>
        /// <param name="token">the raw jwt token</param>
        /// <returns></returns>
        UniTask<string> Decode(string token, string secretKey = null);

        /// <summary>
        /// Encodes the information using a secret key and header which can be decoded
        /// </summary>
        /// <param name="header"></param>
        /// <param name="info"></param>
        /// <param name="secretKey"></param>
        /// <returns></returns>
        UniTask<string> Encode(string header, string info, string secretKey);
    }
}
