using System;
using Cysharp.Threading.Tasks;
using Genies.CrashReporting;
using UnityEngine;

namespace Genies.Utilities
{
    public class JWTDecoderService: IJWTDecoderService
    {
        public UniTask<string> Decode(string token, string secretKey = null)
        {
            var serializedInfo = token;

            try
            {
                var parts = token.Split('.');
                if (parts.Length > 2)
                {
                    var decode = parts[1];
                    var padLength = 4 - decode.Length % 4;
                    if (padLength < 4)
                    {
                        decode += new string('=', padLength);
                    }
                    var bytes = System.Convert.FromBase64String(decode);
                    serializedInfo = System.Text.Encoding.ASCII.GetString(bytes);

                    if (!string.IsNullOrEmpty(secretKey))
                    {
                        // Decode with secretKey if provided
                        var header = parts[0];
                        var signature = parts[2];

                        var computedSignature = GenerateSignature(header, serializedInfo, secretKey);

                        if (computedSignature != signature)
                        {
                            throw new Exception("Invalid JWT signature");
                        }
                    }

                    return UniTask.FromResult(serializedInfo);
                }
            }
            catch (Exception e)
            {
                CrashReporter.LogError($"[{nameof(JWTDecoderService)}] failed to deserialize JWT {e}");
            }

            return UniTask.FromResult(string.Empty);
        }

        private string GenerateSignature(string header, string payload, string secretKey)
        {
            using (var hmac = new System.Security.Cryptography.HMACSHA256(System.Text.Encoding.ASCII.GetBytes(secretKey)))
            {
                var data = $"{header}.{payload}";
                var hash = hmac.ComputeHash(System.Text.Encoding.ASCII.GetBytes(data));
                return System.Convert.ToBase64String(hash).TrimEnd('=');
            }
        }

        public UniTask<string> Encode(string header, string token, string secretKey)
        {
            string encodedToken = string.Empty;

            try
            {
                var bytes = System.Text.Encoding.ASCII.GetBytes(token);

                var base64String = System.Convert.ToBase64String(bytes).TrimEnd('=');

                var payload = base64String;
                var signature = GenerateSignature(header, token, secretKey);

                encodedToken = $"{header}.{payload}.{signature}";
                Debug.Log(encodedToken);
            }
            catch (Exception e)
            {
                CrashReporter.LogError($"[{nameof(JWTDecoderService)}] failed to encode {e}");
            }

            return UniTask.FromResult(encodedToken);
        }
    }
}
