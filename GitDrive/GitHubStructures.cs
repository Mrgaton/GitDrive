using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace GitDrive
{
    public class Commit
    {
        [JsonPropertyName("message")]
        public string Message { get; set; }

      
    }
    public class GitFile
    {
        [JsonIgnore]
        public string filePath { get; set; }

        [JsonPropertyName("content")]
        public byte[] fileData { get; set; }
    }

    public class Tree
    {
        [JsonPropertyName("base_tree")]
        public string BaseTree { get; set; }

        [JsonPropertyName("tree")]
        public TreeObjects[] Objects { get; set; }
        
        public class TreeObjects
        {
            [JsonPropertyName("path")]
            public string Path { get; set; }

            [JsonPropertyName("mode")]
            [JsonConverter(typeof(FileModeConverter))]
            public FileMode Mode { get; set; }

            [JsonPropertyName("type")]
            public FileType Type { get; set; }

            [JsonPropertyName("content")]
            public byte[] Content { get; set; }
        }
        public class FileModeConverter : JsonConverter<FileMode>
        {
            public override FileMode Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) => Enum.Parse<FileMode>(reader.GetString(), true);
            public override void Write(Utf8JsonWriter writer, FileMode value, JsonSerializerOptions options) => writer.WriteStringValue(((int)value).ToString());
        }

        public enum FileMode
        {
            FileBlob = 100644,
            ExecutableBlob = 100755,
            SubdirectoryTree = 040000,
            SubmoduleCommit = 160000,
            SymlinkBlob = 120000,
        }
        public enum FileType
        {
            [JsonPropertyName("blob")]
            Blob,

            [JsonPropertyName("tree")]
            Tree,

            [JsonPropertyName("commit")]
            Commit
        }
    }
}
