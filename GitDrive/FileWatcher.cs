﻿using GitDrive.Files;
using GitDrive.Github;
using GitDrive.Helpers;
using FileMode = GitDrive.Github.FileMode;

namespace GitDrive
{
    internal class FileWatcher
    {
        private static FileSystemWatcher watcher = new FileSystemWatcher() { Path = Program.DefaultFilesPath, IncludeSubdirectories = true, EnableRaisingEvents = true, Filter = "*" };

        private static Task filesSyncronizer;

        public static List<string> filesToUpload = new List<string>();

        public static Dictionary<string, FileInfo> modifiedPaths = new Dictionary<string, FileInfo>(); // <Path,Directory>

        public static void Init()
        {
            watcher.Changed += OnChanged;
            watcher.Deleted += OnDeleted;
            watcher.Renamed += OnDeleted;
            watcher.Created += OnChanged;

            //SeachUnsyncedFiles(Program.DefaultFilesPath);

            filesSyncronizer = Task.Run(FilesSyncronizer);
        }

        /*private static async Task SeachUnsyncedFiles(string path)
        {
            foreach (var file in Directory.EnumerateFiles(path))
            {
                var parentPath = GetParent(Program.DefaultFilesPath, file);

                var encodedPath = DataEncoder.EncodeFilePath(parentPath);

                if (!File.Exists(Path.Combine(Program.DefaultSyncPath, encodedPath)))
                {
                    filesToUpload.Add(parentPath);
                }

                await Task.Delay(100);
            }

            foreach (var dir in Directory.EnumerateDirectories(path)) await SeachUnsyncedFiles(dir);
        }*/

        public static void OnDeleted(object source, FileSystemEventArgs e)
        {
            Console.WriteLine("{0}, with path {1} has been {2}", e.Name, e.FullPath, e.ChangeType);

        }

        public static async void OnChanged(object source, FileSystemEventArgs e)
        {
            modifiedPaths.Add(GetParent(Program.DefaultFilesPath, e.FullPath),new FileInfo(e.FullPath));

            Console.WriteLine("{0}, with path {1} has been {2}", e.Name, e.FullPath, e.ChangeType);

            FileInfo finfo = (File.Exists(e.FullPath) || Directory.Exists(e.FullPath)) ? new FileInfo(e.FullPath) : null;

            bool directory = finfo?.Attributes.HasFlag(FileAttributes.Directory) ?? false;

            string relativePath = DataEncoder.EncodeDirectoryPath(GetParent(Program.DefaultFilesPath, directory ? e.FullPath : Path.GetDirectoryName(e.FullPath)));

            if (directory)
            {
                string syncPath = Path.Join(Program.DefaultSyncPath, relativePath);
                if (!Directory.Exists(syncPath)) Directory.CreateDirectory(syncPath);
                return;
            }

            string fileNameHash = DataEncoder.EncodeName(Path.GetFileName(e.FullPath));

            string fileSyncPath = Path.Join(Program.DefaultSyncPath, relativePath, fileNameHash);

            var file = File.Exists(fileSyncPath) ? SerealizedFile.Decode(File.ReadAllText(fileSyncPath)) : new SerealizedFile();

            file.Directory = directory;
            file.OriginalPath = relativePath + "\\" + Path.GetFileName(e.FullPath);
            file.Path = relativePath + "\\" + fileNameHash;
            file.Attributes = finfo.Attributes;
            file.CreateTime = finfo.CreationTimeUtc;
            file.LastWriteTime = finfo.LastWriteTime;
            file.DeletedFile = e.ChangeType == WatcherChangeTypes.Deleted;
            file.Version++;

            string jsonPath = fileSyncPath + ".json";

            var fileData = File.ReadAllBytes(e.FullPath);

            if (fileData.Length > 0)
            {
                await File.WriteAllTextAsync(fileSyncPath + ".db", DataEncoder.EncodeData(fileData));
                file.DataChunks = [GetParent(Program.DefaultSyncPath, fileSyncPath + ".db")];
                await File.WriteAllTextAsync(jsonPath, file.Encode());
            }

            filesToUpload.Add(GetParent(Program.DefaultSyncPath, jsonPath));

            foreach (var f in file.DataChunks) filesToUpload.Add(f);
        }

      

        private static string GetParent(string parent, string path) => path.Substring(parent.Length).TrimStart('\\');

        private static List<TreeObject> ProcessChanges()
        {
            if (modifiedPaths.Count <= 0) return null;

            List<TreeObject> comitChanges = new List<TreeObject>();

            foreach(var path in modifiedPaths)
            {
                string parentPath = GetParent(Program.DefaultFilesPath, path.Key);

                string relativeFilePath = DataEncoder.EncodeFilePath(parentPath);

                string relativeFileName = Path.GetFileName(relativeFilePath);

                bool directory = Directory.GetFiles(Path.Combine(Program.DefaultSyncPath, Path.GetPathRoot(relativeFilePath))).Any(f => f.StartsWith(relativeFileName));

                string relativeDirectoryPath = directory ? DataEncoder.EncodeDirectoryPath(parentPath) : null;

                if (path.Value == null)
                {
                    //Deleted object

                    if (directory)
                    {
                        string syncDirectoryPath = Path.Join(Program.DefaultSyncPath, relativeDirectoryPath);

                        if (Directory.Exists(syncDirectoryPath)) Directory.Delete(syncDirectoryPath, true);
                        RemoveSimpleFile(ref comitChanges, syncDirectoryPath);
                        continue;
                    }

                    foreach (var p in Directory.EnumerateFiles(Path.Join(Program.DefaultSyncPath, relativeDirectoryPath), relativeFileName + ".*"))
                    {
                        File.Delete(p);

                        RemoveSimpleFile(ref comitChanges, relativeFilePath);
                        filesToUpload.Add(GetParent(Program.DefaultSyncPath, p));
                    }

                    continue;
                }
                else
                {
                    //Added object
                }
            }

            return comitChanges;
        }
        public static async Task FilesSyncronizer()
        {
            while (true)
            {
                try
                {
                    await Task.Delay(5000);

                    if (filesToUpload.Count > 0)
                    {
                        List<TreeObject> comitChanges = new List<TreeObject>();

                        foreach (var file in filesToUpload)
                        {
                            string joinedFile = Path.Join(Program.DefaultSyncPath, file);

                            if (File.Exists(joinedFile)) AddSimpleFile(ref comitChanges, Program.DefaultFilesPath, joinedFile);
                            else RemoveSimpleFile(ref comitChanges, Program.DefaultSyncPath, joinedFile);
                        }

                        var treeSha = await GitHubApi.CreateTree(new Tree()
                        {
                            BaseTree = GitHubApi.TreeSha,
                            Objects = comitChanges.ToArray()
                        });

                        var commitSha = await GitHubApi.CreateCommit(new Commit()
                        {
                            Tree = treeSha,
                            Message = $"Commit Changes:{filesToUpload.Count()} ID:{new Random().Next()}",
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

                        Console.WriteLine("Uploaded successfully");

                        filesToUpload.Clear();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                }
            }
        }

        public static void AddSimpleFile(ref List<TreeObject> list, string filePath) => AddSimpleFile(ref list, filePath, Program.DefaultSyncPath);
        public static void AddSimpleFile(ref List<TreeObject> list, string filePath, string relativePath)
        {
            list.Add(new TreeObject()
            {
                Content = File.ReadAllText(filePath),
                Path = filePath.Substring(relativePath.Length).Replace('\\', '/').TrimStart('/'),
                Mode = FileMode.FileBlob,
                Type = FileType.Blob
            });
        }
        public static void RemoveSimpleFile(ref List<TreeObject> list, string filePath) => RemoveSimpleFile(ref list, filePath, Program.DefaultSyncPath);
        public static void RemoveSimpleFile(ref List<TreeObject> list, string filePath, string relativePath)
        {
            list.Add(new TreeObject()
            {
                Path = filePath.Substring(relativePath.Length).Replace('\\', '/').TrimStart('/'),
                Mode = FileMode.FileBlob,
                Type = FileType.Commit,
                Sha = null
            });
        }
        public static void RemoveSimpleDirectory(ref List<TreeObject> list, string filePath) => RemoveSimpleDirectory(ref list, filePath, Program.DefaultSyncPath);

        public static void RemoveSimpleDirectory(ref List<TreeObject> list, string filePath, string relativePath)
        {
            list.Add(new TreeObject()
            {
                Path = filePath.Substring(relativePath.Length).Replace('\\', '/').TrimStart('/'),
                Mode = FileMode.SubdirectoryTree,
                Type = FileType.Blob,
                Sha = null
            });
        }
    }
}