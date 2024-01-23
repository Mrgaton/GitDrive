using System;
using System.Buffers.Text;
using System.Collections.Generic;
using System.Linq;
using System.Security.AccessControl;
using System.Text;
using System.Threading.Tasks;

namespace GitDrive.Helpers
{
    internal class DataEncoder
    {
        private static Base91 base91 = new Base91();
        public static string EncodeDataStr(string data) => EncodeData(Encoding.UTF8.GetBytes(data));
        public static string EncodeData(byte[] data )
        {
           return base91.Encode(Brotli.Compress(data));
        }

        public static string DecodeDataStr(string data) => Encoding.UTF8.GetString(DecodeData(data));
        public static byte[] DecodeData(string data)
        {
            return Brotli.Decompress(base91.Decode(data));
        }
    }
}
