using System.Security.Cryptography;

namespace GitDrive.Helpers
{
    internal class Hash
    {
        private static SHA1 hashAlg = SHA1.Create();

        public static string HashData(byte[] data) => Convert.ToHexString(hashAlg.ComputeHash(data)).ToLower();
    }
}