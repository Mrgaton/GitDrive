using GitDrive.Github;
using System;
using System.Collections.Generic;
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

            foreach(var obj in tree.Objects)
            {
                string filePath = SerealizePath(obj.Path);

                if (!filePath.EndsWith(".db")&&!File.Exists(filePath))
                {
                    await DownloadFile(obj.Path);

                    await DownloadSyncChunks(filePath);
                }
            }
        }
        private static async Task DownloadSyncChunks(string path)
        {
            var fileInfo = SerealizedFile.Decode(File.ReadAllText(path));

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
    }
}
