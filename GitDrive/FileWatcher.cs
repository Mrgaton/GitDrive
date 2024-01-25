using GitDrive.Files;
using GitDrive.Github;
using GitDrive.Helpers;
using System.Data;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using FileMode = GitDrive.Github.FileMode;

namespace GitDrive
{
    internal class FileWatcher
    {

        private static FileSystemWatcher watcher = new FileSystemWatcher() { Path = Program.DefaultFilesPath, IncludeSubdirectories = true, EnableRaisingEvents = true, Filter = "*" };

        private static Task filesSyncronizer;

        public static List<string> changedFiles = new List<string>();

        public static void Init()
        {
            watcher.Changed += OnChanged;
            watcher.Deleted += OnDeleted;

            filesSyncronizer = Task.Run(FilesSyncronizer);
        }
        private static void SeachUnsyncedFiles(string path)
        {
            foreach (var file in Directory.EnumerateFiles(path))
            {
                var a = GetParent(Program.DefaultFilesPath, file);

                if (!File.Exists(Path.Combine(Program.DefaultSyncPath, )))
                {

                }
            }
          
            foreach(var dir in Directory.EnumerateDirectories(path)) SeachUnsyncedFiles(dir);
        }

        private static string GetParent(string parent, string path)
        {
            return path.Substring(parent.Length).TrimStart('\\');
        }

        public static void OnDeleted(object source, FileSystemEventArgs e)
        {
            FileInfo finfo = (File.Exists(e.FullPath) || Directory.Exists(e.FullPath)) ? new FileInfo(e.FullPath) : null;
            bool directory = finfo?.Attributes.HasFlag(FileAttributes.Directory) ?? false;

            string relativePath = string.Empty;

            if (directory)
            {
                relativePath = DataEncoder.EncodePath(GetParent(Program.DefaultFilesPath,e.FullPath));

                string syncPath = Path.Join(Program.DefaultSyncPath, relativePath);

                if (Directory.Exists(syncPath)) Directory.Delete(syncPath, true);

                changedFiles.Add(relativePath);
                return;
            }

            relativePath = Path.GetDirectoryName(e.FullPath).Substring(Program.DefaultFilesPath.Length).Trim('\\');

            string fileNameHash = DataEncoder.EncodePart(Path.GetFileName(e.FullPath));

            foreach (var path in Directory.EnumerateFiles(Path.Join(Program.DefaultSyncPath, relativePath), fileNameHash + ".*"))
            {
                changedFiles.Add(GetParent(Program.DefaultSyncPath, path));
                File.Delete(path);
            }

            Console.WriteLine("{0}, with path {1} has been {2}", e.Name, e.FullPath, e.ChangeType);
        }

        public static async void OnChanged(object source, FileSystemEventArgs e)
        {
            FileInfo finfo = (File.Exists(e.FullPath) || Directory.Exists(e.FullPath)) ? new FileInfo(e.FullPath) : null;

            bool directory = finfo?.Attributes.HasFlag(FileAttributes.Directory) ?? false;

            string relativePath = string.Empty;

            if (directory)
            {
                relativePath = e.FullPath.Substring(Program.DefaultFilesPath.Length);
                string syncPath = Path.Join(Program.DefaultSyncPath, relativePath);
                if (!Directory.Exists(syncPath)) Directory.CreateDirectory(syncPath);
                return;
            }

            relativePath = Path.GetDirectoryName(e.FullPath).Substring(Program.DefaultFilesPath.Length).Trim('\\');

            string fileNameHash = BitConverter.ToString(hashAlg.ComputeHash(Encoding.UTF8.GetBytes(Path.GetFileName(e.FullPath)))).Replace("-", "");


            string fileSyncPath = Path.Join(Program.DefaultSyncPath, relativePath, fileNameHash);

            var file = File.Exists(fileSyncPath) ? SerealizedFile.Decode(File.ReadAllText(fileSyncPath)) : new SerealizedFile();
           
            file.Directory = directory;
            file.OriginalPath = relativePath + "\\" + Path.GetFileName(e.FullPath);
            file.Path = relativePath + "\\" + fileNameHash;
            file.Attributes = finfo.Attributes;
            file.CreateTime = finfo.CreationTimeUtc;
            file.LastWriteTime = finfo.LastWriteTime;
            file.DeletedFile = e.ChangeType == WatcherChangeTypes.Deleted;
            file.Version = file.Version + 1;

            string jsonPath = fileSyncPath + ".json";

            if (!file.DeletedFile)
            {
                await File.WriteAllTextAsync(fileSyncPath + ".0.db", DataEncoder.EncodeData(File.ReadAllBytes(e.FullPath)));
                file.DataChunks = [GetParent(Program.DefaultSyncPath, fileSyncPath + ".0.db")];
                await File.WriteAllTextAsync(jsonPath, file.Encode());
            }
            else
            {
                if (File.Exists(jsonPath)) File.Delete(jsonPath);
            }

            changedFiles.Add(GetParent(Program.DefaultSyncPath, jsonPath));

            foreach (var f in file.DataChunks) changedFiles.Add(f);

            Console.WriteLine("{0}, with path {1} has been {2}", e.Name, e.FullPath, e.ChangeType);
        }

        public static void AddSimpleFile(ref List<TreeObject> list, string relativePath, string filePath)
        {
            list.Add(new TreeObject()
            {
                Content = File.ReadAllText(filePath),
                Path = filePath.Substring(relativePath.Length).Replace('\\', '/').TrimStart('/'),
                Mode = FileMode.FileBlob,
                Type = FileType.Blob
            });
        }

        public static void RemoveSimpleFile(ref List<TreeObject> list, string relativePath, string filePath)
        {
            list.Add(new TreeObject()
            {
                Path = filePath.Substring(relativePath.Length).Replace('\\', '/').TrimStart('/'),
                Mode = FileMode.FileBlob,
                Type = FileType.Commit,
                Sha = null
            });
        }

        public static void RemoveSimpleDirectory(ref List<TreeObject> list, string relativePath, string filePath)
        {
            list.Add(new TreeObject()
            {
                Path = filePath.Substring(relativePath.Length).Replace('\\', '/').TrimStart('/'),
                Mode = FileMode.SubdirectoryTree,
                Type = FileType.Blob,
                Sha = null
            });
        }

        public static async Task FilesSyncronizer()
        {
            while (true)
            {
                try
                {
                    await Task.Delay(5000);

                    if (changedFiles.Count() > 0)
                    {
                        List<TreeObject> comitChanges = new List<TreeObject>();

                        foreach (var file in changedFiles)
                        {
                            string joinedFile = Path.Join(Program.DefaultSyncPath, file);

                            if (File.Exists(joinedFile))
                            {
                                AddSimpleFile(ref comitChanges, Program.DefaultFilesPath, joinedFile);
                            }
                            else
                            {
                                RemoveSimpleFile(ref comitChanges, Program.DefaultSyncPath, joinedFile);
                            }
                        }

                        var treeSha = await GitHubApi.CreateTree(new Tree()
                        {
                            BaseTree = GitHubApi.TreeSha,
                            Objects = comitChanges.ToArray()
                        });

                        var commitSha = await GitHubApi.CreateCommit(new Commit()
                        {
                            Tree = treeSha,
                            Message = $"Commit Changes:{changedFiles.Count()} ID:{new Random().Next()}",
                            Parrents = [GitHubApi.ComitSha]
                        });

                        if (!string.IsNullOrWhiteSpace(treeSha) && !string.IsNullOrWhiteSpace(commitSha))
                        {
                            GitHubApi.TreeSha = treeSha;
                            GitHubApi.ComitSha = commitSha;
                        }

                        var result = await GitHubApi.CreateReference(new Reference()
                        {
                            Sha = commitSha,
                            Force = true
                        });

                        Console.WriteLine("jejej");

                        changedFiles.Clear();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                }
            }
        }
    }
}