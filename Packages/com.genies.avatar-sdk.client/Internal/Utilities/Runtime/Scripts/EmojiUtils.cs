using System;
using System.Collections.Generic;
using System.Text;

namespace Genies.Utilities
{
    public static class EmojiUtils
    {
        //Add all unwanted symbols, that are added together with emoji's here.
        private static List<string> _unwantedSymbols = new()
        {
            "0D2040260FFE",
            "0d2040260ffe",
            "3cd8fbdf",
            "3CD8FBDF",
            "3cd8fcdf",
            "3CD8FCDF",
            "3cd8fddf",
            "3CD8FDDF",
            "3cd8fedf",
            "3CD8FEDF",
            "3cd8ffdf",
            "3CD8FFDF",
        };

        public static string ToHexString(this string str)
        {
            var sb = new StringBuilder();

            var bytes = Encoding.Unicode.GetBytes(str);
            foreach (var t in bytes)
            {
                sb.Append(t.ToString("x2"));
            }

            return sb.ToString(); // returns: "48656C6C6F20776F726C64" for "Hello world"
        }

        public static string ToHexString(this string str, Encoding e)
        {
            var sb = new StringBuilder();

            var bytes = e.GetBytes(str);
            foreach (var t in bytes)
            {
                sb.Append(t.ToString("x2"));
            }

            return sb.ToString(); // returns: "48656C6C6F20776F726C64" for "Hello world"
        }

        public static string FromHexString(this string hexString)
        {
            var bytes = new byte[hexString.Length / 2];
            for (var i = 0; i < bytes.Length; i++)
            {
                bytes[i] = Convert.ToByte(hexString.Substring(i * 2, 2), 16);
            }

            return Encoding.Unicode.GetString(bytes); // returns: "Hello world" for "48656C6C6F20776F726C64"
        }

        public static string FilterEmojiErrors(string input)
        {
            var hexStr = ToHexString(input);
            foreach (var symbol in _unwantedSymbols)
            {
                if (hexStr.Contains(symbol))
                {
                    hexStr = hexStr.Replace(symbol, string.Empty);
                }
            }

            return hexStr.FromHexString();
        }
    }
}
