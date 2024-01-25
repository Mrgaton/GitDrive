using System.Security.Cryptography;
using System.Text;

namespace GitDrive.Helpers
{
    internal class DataEncoder
    {
        private static Base91 base91 = new Base91();

        public static string EncodeDataStr(string data) => EncodeData(Encoding.UTF8.GetBytes(data));

        public static string EncodeData(byte[] data)
        {
            return base91.Encode(Brotli.Compress(data));
        }

        public static string DecodeDataStr(string data) => Encoding.UTF8.GetString(DecodeData(data));

        public static byte[] DecodeData(string data)
        {
            return Brotli.Decompress(base91.Decode(data));
        }

        private static SHA384 hashAlg = SHA384.Create();
        public static string EncodePart(string part) => Convert.ToHexString(hashAlg.ComputeHash(Encoding.UTF8.GetBytes(part)));
        public static string EncodePath(string path)
        {
            StringBuilder sb = new StringBuilder();

            foreach(var part in path.Split('\\'))
            {
                if (string.IsNullOrEmpty(part)) continue;
                sb.Append(EncodePart(part));
                sb.Append('\\');
            }

            return !path.EndsWith('\\') ? sb.ToString().TrimEnd('\\') : sb.ToString();
        }
    }
}