using GitDrive.Github;
using GitDrive.Helpers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;

namespace GitDrive
{
    internal class Program
    {
        public static string EncKey;

        public static readonly string DefaultProgramPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "GitDrive");
        public static readonly string DefaultFilesPath = Path.Combine(DefaultProgramPath, "UserFiles");
        public static readonly string DefaultSyncPath = Path.Combine(DefaultProgramPath, "SyncFiles");

        private static void Main(string[] args) => Task.WaitAll(AsyncMain(args));

        private static async Task AsyncMain(string[] args)
        {
            if (!File.Exists("config.txt")) throw new FileNotFoundException("The config cant be found");

            JsonNode config = JsonNode.Parse(File.ReadAllText("config.txt"));

            GitHubApi.GitToken = (string)config["token"];
            GitHubApi.GitUsername = (string)config["username"];
            GitHubApi.GitRepoName = (string)config["repo"];

            EncKey = DataEncoder.ToBase64Url(SHA512.Create().ComputeHash(Encoding.UTF8.GetBytes((string)config["encoding_key"])));

            if (EncKey.Length < 64) throw new NotSupportedException("The encoding key is too short");

            var encodedPath = DataEncoder.EncodeDirectoryPath("Mrgaton\\OneDrive\\Documentos");

            Console.WriteLine(encodedPath);
            Console.WriteLine(DataEncoder.DecodeDirectoryPath(encodedPath));

            Console.WriteLine(DataEncoder.EncodeFilePath("Mrgaton\\OneDrive\\Documentos"));
            //Console.ReadLine();

            DriveInit.InitDrive();

            await GitHubApi.GetRateLimit();

            await GitHubApi.Init();

            FileWatcher.Init();

            //await SyncFiles.DownloadChanges();

            /*var treeSha = await GitHubApi.CreateTree(new Tree()
            {
                BaseTree = GitHubApi.TreeSha,
                Objects = [
                     new Tree.TreeObject()
                     {
                         Sha = null,
                         Mode = Tree.FileMode.ExecutableBlob,
                         Path = "cosa.ico",
                         Type = Tree.FileType.Commit
                     },
                 ]
            });

            var commitSha = await GitHubApi.CreateCommit(new Commit()
            {
                Tree = treeSha,
                Message = "Commit ID:" + new Random().Next(),
                Parrents = [GitHubApi.ComitSha]
            });

            await GitHubApi.CreateReference(new Reference()
            {
                Sha = commitSha,
                Force = true
            });*/

            Console.WriteLine();
            //GitHubApi.GetFile("");
            /*GitHubApi.UploadFile(new Commit()
            {
                Message = "TU mami amorcito",
            });*/

            Thread.Sleep(-1);
        }
    }
}