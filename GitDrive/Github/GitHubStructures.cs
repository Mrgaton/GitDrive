using System.Text.Json;
using System.Text.Json.Serialization;

namespace GitDrive.Github
{
    public class Reference
    {
        public override string ToString() => JsonSerializer.Serialize(this, GetType());

        [JsonPropertyName("ref")]
        public string Ref { get; set; }

        [JsonPropertyName("sha")]
        public string Sha { get; set; }

        [JsonPropertyName("force")]
        public bool Force { get; set; }
    }

    public class Commit
    {
        public override string ToString() => JsonSerializer.Serialize(this, GetType());

        [JsonPropertyName("message")]
        public string Message { get; set; }

        [JsonPropertyName("tree")]
        public string Tree { get; set; }

        [JsonPropertyName("parents")]
        public string[] Parrents { get; set; }
    }

    public class GitFile
    {
        public override string ToString() => JsonSerializer.Serialize(this, GetType());

        [JsonIgnore]
        public string filePath { get; set; }

        [JsonPropertyName("content")]
        public byte[] fileData { get; set; }
    }

    public class Tree
    {
        public override string ToString() => JsonSerializer.Serialize(this, GetType());

        [JsonPropertyName("base_tree")]
        public string BaseTree { get; set; }

        [JsonPropertyName("tree")]
        public TreeObject[] Objects { get; set; }
    }
    public class RemoteTree
    {
        public override string ToString() => JsonSerializer.Serialize(this, GetType());

        [JsonPropertyName("sha")]
        public string Sha { get; set; }

        [JsonPropertyName("url")]
        public string Url { get; set; }

        [JsonPropertyName("tree")]
        public TreeObject[] Objects { get; set; }

        [JsonPropertyName("truncated")]
        public bool Truncated { get; set; }
    }


    public class TreeObject
    {
        public override string ToString() => JsonSerializer.Serialize(this, GetType());

        [JsonPropertyName("path")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public string? Path { get; set; }

        [JsonPropertyName("sha")]
        public string? Sha { get; set; }

        [JsonPropertyName("mode")]
        [JsonConverter(typeof(FileModeConverter))]
        public FileMode Mode { get; set; }

        [JsonPropertyName("type")]
        public FileType Type { get; set; }

        [JsonPropertyName("content")]
        public string Content { get; set; }

        [JsonPropertyName("size")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public int Size { get; set; }

        [JsonPropertyName("url")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public string Url { get; set; }
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

    [JsonConverter(typeof(JsonStringEnumConverter))]
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