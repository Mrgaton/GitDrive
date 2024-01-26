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

        private static char PlusPaddingChar = '-';
        private static char SlashPaddingChar = '_';

        public static string ToBase64Url(byte[] data) => Convert.ToBase64String(data).Trim('=').Replace('+', PlusPaddingChar).Replace('/', SlashPaddingChar);

        public static byte[] FromBase64Url(string data) => Convert.FromBase64String(data.Replace(SlashPaddingChar, '/').Replace(PlusPaddingChar, '+').PadRight(data.Length + (4 - data.Length % 4) % 4, '='));

        private static SHA256 hashAlg = SHA256.Create();

        public static string EncodeName(string part) => ToBase64Url(hashAlg.ComputeHash(Encoding.UTF8.GetBytes(XORSTR(Program.EncKey, part))));

        public static string XORSTR(string key, string input)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < input.Length; i++) sb.Append((char)(input[i] ^ key[i % key.Length]));
            return sb.ToString();
        }

        public static string EncodeDirectoryPath(string path) => EncodeDirectoryPath(path.Split('\\'));

        public static string EncodeDirectoryPath(string[] paths)
        {
            StringBuilder sb = new StringBuilder();
            foreach (var part in paths)
            {
                if (string.IsNullOrEmpty(part)) continue;
                sb.Append(ToBase64Url(Encoding.UTF8.GetBytes(XORSTR(Program.EncKey, part))));
                sb.Append('\\');
            }
            return !paths[paths.Length - 1].EndsWith('\\') ? sb.ToString().TrimEnd('\\') : sb.ToString();
        }

        public static string EncodeFilePath(string path)
        {
            var splited = path.Split('\\').Where(c => !string.IsNullOrEmpty(c));

            return EncodeDirectoryPath(splited.Take(splited.Count() - 2).ToArray()) + "\\" + EncodeName(splited.Last());
        }

        public static string DecodeDirectoryPath(string path) => DecodeDirectoryPath(path.Split('\\'));

        public static string DecodeDirectoryPath(string[] paths)
        {
            StringBuilder sb = new StringBuilder();
            foreach (var part in paths)
            {
                if (string.IsNullOrEmpty(part)) continue;
                sb.Append(XORSTR(Program.EncKey, Encoding.UTF8.GetString(FromBase64Url(part))));
                sb.Append('\\');
            }
            return !paths[paths.Length - 1].EndsWith('\\') ? sb.ToString().TrimEnd('\\') : sb.ToString();
        }
    }
}