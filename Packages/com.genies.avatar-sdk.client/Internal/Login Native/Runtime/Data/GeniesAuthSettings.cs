using System;
using System.Text;
using UnityEngine;

namespace Genies.Login.Native.Data
{
    [Serializable]
#if GENIES_SDK && !GENIES_INTERNAL
    internal class GeniesAuthSettings
#else
    public class GeniesAuthSettings
#endif
    {
        public string ClientId;
        public string ClientSecret;

        public static GeniesAuthSettings LoadFromResources()
        {
            var ta = Resources.Load<TextAsset>("GeniesAuthSettings");
            if (ta == null)
            {
                return null;
            }

            byte[] bytes = ta.bytes;

            // Reverse XOR
            byte key = 0x5A;
            for (int i = 0; i < bytes.Length; i++)
            {
                bytes[i] ^= key;
            }

            string json = System.Text.Encoding.UTF8.GetString(bytes);
            return JsonUtility.FromJson<GeniesAuthSettings>(json);
        }
    }
}
