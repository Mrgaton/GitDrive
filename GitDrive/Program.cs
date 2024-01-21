using System.Text;
using System.Text.Json.Nodes;

namespace GitDrive
{
    internal class Program
    {
        public static readonly string DefaultProgramPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "GitDrive");
        public static readonly string DefaultFilesPath = Path.Combine(DefaultProgramPath, "UserFiles");
        public static readonly string DefaultDataPath = Path.Combine(DefaultProgramPath, "DefaultData");

        private static void Main(string[] args) => Task.WaitAll(AsyncMain(args));
        private static async Task AsyncMain(string[] args)
        {
            if (!File.Exists("config.txt")) throw new FileNotFoundException("The config cant be found");

            JsonNode config = JsonNode.Parse(File.ReadAllText("config.txt"));

            GitHubApi.GitToken = (string)config["token"];
            GitHubApi.GitUsername = (string)config["username"];
            GitHubApi.GitRepoName = (string)config["repo"];

            //DriveInit.InitDrive();


            await GitHubApi.Init();

            await GitHubApi.GetRateLimit();

           var sha =  await GitHubApi.CreateTree(new Tree()
            {
                BaseTree = GitHubApi.TreeSha,
                Objects = [ 
                    new Tree.TreeObjects()
                    {
                        Content = Encoding.UTF8.GetBytes("tu abuela sabes?"),
                        Mode = Tree.FileMode.FileBlob,
                        Path = "tuabuela.txt",
                        Type = Tree.FileType.Blob
                    }
                ]
            });

            Console.WriteLine(sha);
            //GitHubApi.GetFile("");
            /*GitHubApi.UploadFile(new Commit()
            {
                Message = "TU mami amorcito",
                
            });*/

            Thread.Sleep(-1);


        }
    }
}