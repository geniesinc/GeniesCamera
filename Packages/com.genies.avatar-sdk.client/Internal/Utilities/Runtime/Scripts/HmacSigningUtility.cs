using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using Cysharp.Threading.Tasks;

namespace Genies.Utilities
{
    public static class HmacSigningUtility
    {
        public static async UniTask<string> SignAsync(string message, string secretKey)
        {
            return await UniTask.RunOnThreadPool(() => Sign(message, secretKey));
        }

        private static string Sign(string message, string secretKey)
        {
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secretKey));
            byte[]    hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(message));
            return BitConverter.ToString(hash).Replace("-", "").ToLower();
        }

        public static async UniTask<List<string>> SignMultipleAsync(IEnumerable<string> messages, string secretKey)
        {
            var tasks = new List<UniTask<string>>();

            foreach (var message in messages)
            {
                tasks.Add(SignAsync(message, secretKey));
            }

            return new List<string>(await UniTask.WhenAll(tasks));
        }
    }
}
