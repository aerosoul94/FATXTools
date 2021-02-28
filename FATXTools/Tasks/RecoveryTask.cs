using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Windows.Forms;

using FATX.FileSystem;

using FATXTools.Database;

namespace FATXTools.Tasks
{
    public class RecoveryTask
    {
        private CancellationToken cancellationToken;
        private IProgress<(int, string)> progress;
        private Volume volume;

        private string currentFile = String.Empty;
        private int numSaved = 0;
        private long numFiles;

        public RecoveryTask(Volume volume, CancellationToken cancellationToken, IProgress<(int, string)> progress)
        {
            this.volume = volume;
            this.cancellationToken = cancellationToken;
            this.progress = progress;
        }

        public void Save(string path, DatabaseFile databaseFile)
        {
            numFiles = databaseFile.CountFiles();

            if (databaseFile.IsDirectory())
            {
                SaveDirectory(databaseFile, path);
            }
            else
            {
                SaveFile(databaseFile, path);
            }
        }

        public void SaveAll(string path, List<DatabaseFile> dirents)
        {
            numFiles = CountFiles(dirents);

            foreach (var databaseFile in dirents)
            {
                if (databaseFile.IsDirectory())
                {
                    SaveDirectory(databaseFile, path);
                }
                else
                {
                    SaveFile(databaseFile, path);
                }
            }
        }

        public void SaveClusters(string path, Dictionary<string, List<DatabaseFile>> clusters)
        {
            numFiles = 0;
            foreach (var cluster in clusters)
            {
                numFiles += CountFiles(cluster.Value);
            }

            foreach (var cluster in clusters)
            {
                string clusterDir = path + "\\" + cluster.Key;

                Directory.CreateDirectory(clusterDir);

                foreach (var file in cluster.Value)
                {
                    if (file.IsDirectory())
                    {
                        SaveDirectory(file, path);
                    }
                    else
                    {
                        SaveFile(file, path);
                    }
                }
            }
        }

        private long CountFiles(List<DatabaseFile> dirents)
        {
            // DirectoryEntry.CountFiles does not count deleted files
            long n = 0;

            foreach (var databaseFile in dirents)
            {
                if (databaseFile.IsDirectory())
                {
                    n += CountFiles(databaseFile.Children) + 1;
                }
                else
                {
                    n++;
                }
            }

            return n;
        }

        private void ReportProgress()
        {
            var percent = (int)(((float)numSaved / (float)numFiles) * 100);
            progress.Report((percent, $"{numSaved}/{numFiles}: {currentFile}"));
        }

        private void SaveDirectory(DatabaseFile databaseFile, string path)
        {
            path = path + "\\" + databaseFile.FileName;
            //Console.WriteLine($"{path}");

            currentFile = databaseFile.FileName;
            numSaved++;
            ReportProgress();

            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            foreach (DatabaseFile child in databaseFile.Children)
            {
                if (child.IsDirectory())
                {
                    SaveDirectory(child, path);
                }
                else
                {
                    SaveFile(child, path);
                }
            }

            TryIOOperation(() =>
            {
                DirectorySetTimestamps(path, databaseFile);
            });
        }

        private void SaveFile(DatabaseFile databaseFile, string path)
        {
            path = path + "\\" + databaseFile.FileName;
            //Console.WriteLine($"{path}");

            currentFile = databaseFile.FileName;
            numSaved++;
            ReportProgress();

            volume.ClusterReader.ReadCluster(databaseFile.FirstCluster);

            TryIOOperation(() =>
            {
                WriteFile(path, databaseFile);

                FileSetTimeStamps(path, databaseFile);
            });
        }

        private DialogResult ShowIOErrorDialog(Exception e)
        {
            return MessageBox.Show($"{e.Message}",
                "Error", MessageBoxButtons.RetryCancel, MessageBoxIcon.Error);
        }

        private void WriteFile(string path, DatabaseFile databaseFile)
        {
            using (FileStream outFile = File.OpenWrite(path))
            {
                uint bytesLeft = databaseFile.FileSize;

                foreach (uint cluster in databaseFile.ClusterChain)
                {
                    byte[] clusterData = this.volume.ClusterReader.ReadCluster(cluster);

                    var writeSize = Math.Min(bytesLeft, this.volume.BytesPerCluster);
                    outFile.Write(clusterData, 0, (int)writeSize);

                    bytesLeft -= writeSize;
                }
            }
        }

        private void FileSetTimeStamps(string path, DatabaseFile databaseFile)
        {
            File.SetCreationTime(path, databaseFile.CreationTime.AsDateTime());
            File.SetLastWriteTime(path, databaseFile.LastWriteTime.AsDateTime());
            File.SetLastAccessTime(path, databaseFile.LastAccessTime.AsDateTime());
        }

        private void DirectorySetTimestamps(string path, DatabaseFile databaseFile)
        {
            Directory.SetCreationTime(path, databaseFile.CreationTime.AsDateTime());
            Directory.SetLastWriteTime(path, databaseFile.LastWriteTime.AsDateTime());
            Directory.SetLastAccessTime(path, databaseFile.LastAccessTime.AsDateTime());
        }

        private void TryIOOperation(Action action)
        {
            try
            {
                action();
            }
            catch (IOException e)
            {
                while (true)
                {
                    var dialogResult = ShowIOErrorDialog(e);

                    if (dialogResult == DialogResult.Retry)
                    {
                        try
                        {
                            action();
                        }
                        catch (Exception)
                        {
                            continue;
                        }
                    }

                    break;
                }
            }
        }
    }
}
