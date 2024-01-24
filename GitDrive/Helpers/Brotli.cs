using System.IO.Compression;

namespace GitDrive.Helpers
{
    public static class Brotli
    {
        private static CompressionLevel GetCompressionLevel(int dataLengh)
        {
            if (dataLengh < 512 * 1024 * 1024) return CompressionLevel.Optimal;
            return CompressionLevel.Fastest;
        }

        public static byte[] Compress(byte[] data)
        {
            if (data == null || data.Length == 0)
            {
                Console.WriteLine("Error buffer nulo comp");
                return null;
            }

            using (MemoryStream output = new MemoryStream())
            {
                using (BrotliStream dstream = new BrotliStream(output, GetCompressionLevel(data.Length)))
                {
                    dstream.Write(data, 0, data.Length);
                    dstream.Flush();
                }

                return output.ToArray();
            }
        }

        public static byte[] Decompress(byte[] data)
        {
            if (data == null || data.Length == 0)
            {
                Console.WriteLine("Error buffer nulo decomp");
                return null;
            }

            using (MemoryStream input = new MemoryStream(data))
            {
                using (MemoryStream output = new MemoryStream())
                {
                    using (BrotliStream dstream = new BrotliStream(input, CompressionMode.Decompress))
                    {
                        dstream.CopyTo(output);
                        dstream.Flush();
                    }

                    return output.ToArray();
                }
            }
        }
    }
}