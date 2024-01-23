using GitDrive.Helpers;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace GitDrive.Files
{
    public class ModifiedFiles
    {
        public SerealizedFile[] Files { get; set; }
    }

    public class SerealizedFile
    {
        public bool DeletedFile { get; set; }
        public bool Directory { get; set; }
        public string OriginalPath { get; set; }
        public string Path { get; set; }
        public FileAttributes Attributes { get; set; }
        public DateTime CreateTime { get; set; }
        public DateTime LastWriteTime { get; set; }
        public string[] DataChunks { get; set; }
        public string Encode() => DataEncoder.EncodeDataStr(JsonSerializer.Serialize(this, GetType()));
        public static SerealizedFile Decode(string encoded) => JsonSerializer.Deserialize<SerealizedFile>(DataEncoder.DecodeDataStr(encoded));
    }
}