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

            {

                {
                    await DownloadFile(obj.Path);
                    await DownloadSyncChunks(fileInfoPath);
                }
                else
                {
                    var downloaded = await DownloadData(obj.Path);

                }
            }
        }
        {

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
