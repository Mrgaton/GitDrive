using GitDrive.Github;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GitDrive.Files
{
    internal class SyncFiles
    {
        private static string SerealizePath(string path) => Path.Join(Program.DefaultSyncPath, path.Replace('/', '\\'));
        public static async Task DownloadChanges()
        {
            var tree = await GitHubApi.GetTree();

            foreach(var obj in tree.Objects.Where(o => o.Path.EndsWith(".json",StringComparison.InvariantCultureIgnoreCase)))
            {
                string fileInfoPath = SerealizePath(obj.Path);

                if (!File.Exists(fileInfoPath))
                {
                    await DownloadFile(obj.Path);
                    await DownloadSyncChunks(fileInfoPath);
                }
                else
                {
                    var downloaded = await DownloadData(obj.Path);

                    var remoteConfig = SerealizedFile.Decode(Encoding.UTF8.GetString(downloaded));
                    var localConfig = SerealizedFile.Decode(File.ReadAllText(fileInfoPath));

                    Console.ReadLine();
                }
            }
        }
        private static async Task DownloadSyncChunks(string syncPath)
        {
            var fileInfo = SerealizedFile.Decode(File.ReadAllText(syncPath));

            foreach(var chunk in fileInfo.DataChunks)
            {
                await DownloadFile(chunk);
            }
        }
        private static async Task DownloadFile(string path)
        {
            string filePath = SerealizePath(path);
            File.WriteAllBytes(filePath, await GitHubApi.GetFileRaw(path));
        }
        private static async Task<byte[]> DownloadData(string path)
        {
            return await GitHubApi.GetFileRaw(path);
        }

        private static void CreateUserFile(string syncPath)
        {
            var fileInfo = SerealizedFile.Decode(File.ReadAllText(syncPath));
        }
    }
}
