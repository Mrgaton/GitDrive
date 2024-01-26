using GitDrive.Github;
using GitDrive.Helpers;
using System.Text;

namespace GitDrive.Files
{
    internal class SyncFiles
    {
        private static string SerealizePath(string path) => Path.Join(Program.DefaultSyncPath, path.Replace('/', '\\'));

        public static async Task DownloadChanges()
        {
            var tree = await GitHubApi.GetTree();

            foreach (var obj in tree.Objects.Where(o => o.Path.EndsWith(".json", StringComparison.InvariantCultureIgnoreCase)))
            {
                string fileInfoPath = SerealizePath(obj.Path);

                if (!File.Exists(fileInfoPath))
                {
                    await DownloadFile(obj.Path);

                    await CreateUserFile(fileInfoPath);
                }
                else
                {
                    var data = Hash.HashData(await File.ReadAllBytesAsync(fileInfoPath));

                    if (obj.Sha != data)
                    {
                        var downloaded = await DownloadData(obj.Path);
                        var remoteConfig = SerealizedFile.Decode(Encoding.UTF8.GetString(downloaded));
                        var localConfig = SerealizedFile.Decode(File.ReadAllText(fileInfoPath));

                        if (remoteConfig.LastWriteTime > localConfig.LastWriteTime)
                        {
                            await DownloadFile(obj.Path);
                            await CreateUserFile(fileInfoPath);
                        }
                        else
                        {
                            // TODO: Upload the newer local file
                        }
                    }

                    Console.ReadLine();
                }
            }
        }

        private static async Task<byte[]> DownloadData(string path) => await GitHubApi.GetFileRaw(path);

        private static async Task DownloadFile(string path)
        {
            string filePath = SerealizePath(path);
            File.WriteAllBytes(filePath, await GitHubApi.GetFileRaw(path));
        }

        private static async Task DownloadSyncChunks(SerealizedFile syncFile)
        {
            foreach (var chunk in syncFile.DataChunks)
            {
                await DownloadFile(chunk);
            }
        }

        private static async Task CreateUserFile(string syncPath)
        {
            var fileInfo = SerealizedFile.Decode(File.ReadAllText(syncPath));

            await DownloadSyncChunks(fileInfo);

            using (FileStream fs = File.Open(Path.Combine(Program.DefaultFilesPath, fileInfo.OriginalPath.TrimStart('\\')), System.IO.FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None))
            {
                foreach (var chunk in fileInfo.DataChunks)
                {
                    string path = SerealizePath(chunk);

                    var chunkData = DataEncoder.DecodeData(File.ReadAllText(path));

                    await fs.WriteAsync(chunkData, 0, chunkData.Length);

                    File.Delete(path);
                }

                await fs.FlushAsync();
            }
        }
    }
}