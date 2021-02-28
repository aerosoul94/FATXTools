using FATX.FileSystem;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Windows.Forms;

namespace FATXTools.Tasks
{
    public class SaveContentTask
    {
        private CancellationToken cancellationToken;

        private IProgress<(int, string)> progress;

        private Volume volume;

        private string currentFile;

        private int numSaved;
        private long numFiles;

        public SaveContentTask(Volume volume, CancellationToken cancellationToken, IProgress<(int, string)> progress)
        {
            currentFile = String.Empty;

            this.cancellationToken = cancellationToken;
            this.progress = progress;
            this.volume = volume;

            this.numSaved = 0;
        }

        public void Save(string path, DirectoryEntry dirent)
        {
            numFiles = dirent.CountFiles();

            Console.WriteLine($"Saving {numFiles} files.");

            SaveDirectoryEntry(path, dirent);
        }

        public void SaveAll(string path, List<DirectoryEntry> dirents)
        {
            numFiles = volume.CountFiles();

            Console.WriteLine($"Saving {numFiles} files.");

            foreach (var dirent in dirents)
            {
                SaveDirectoryEntry(path, dirent);
            }
        }

        private DialogResult ShowIOErrorDialog(Exception e)
        {
            return MessageBox.Show($"{e.Message}",
                "Error", MessageBoxButtons.RetryCancel, MessageBoxIcon.Error);
        }

        private void WriteFile(string path, DirectoryEntry dirent, List<uint> chainMap)
        {
            using (FileStream outFile = File.OpenWrite(path))
            {
                uint bytesLeft = dirent.FileSize;

                foreach (uint cluster in chainMap)
                {
                    byte[] clusterData = this.volume.ClusterReader.ReadCluster(cluster);

                    var writeSize = Math.Min(bytesLeft, this.volume.BytesPerCluster);
                    outFile.Write(clusterData, 0, (int)writeSize);

                    bytesLeft -= writeSize;
                }
            }
        }
        private void FileSetTimeStamps(string path, DirectoryEntry dirent)
        {
            File.SetCreationTime(path, dirent.CreationTime.AsDateTime());
            File.SetLastWriteTime(path, dirent.LastWriteTime.AsDateTime());
            File.SetLastAccessTime(path, dirent.LastAccessTime.AsDateTime());
        }

        private void DirectorySetTimestamps(string path, DirectoryEntry dirent)
        {
            Directory.SetCreationTime(path, dirent.CreationTime.AsDateTime());
            Directory.SetLastWriteTime(path, dirent.LastWriteTime.AsDateTime());
            Directory.SetLastAccessTime(path, dirent.LastAccessTime.AsDateTime());
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

        private void ReportProgress()
        {
            var percent = (int)(((float)numSaved / (float)numFiles) * 100);
            progress.Report((percent, $"{numSaved}/{numFiles}: {currentFile}"));
        }

        private void SaveFile(string path, DirectoryEntry dirent)
        {
            path = path + "\\" + dirent.FileName;
            Console.WriteLine(path);

            // Report where we are at
            currentFile = dirent.FileName;
            numSaved++;
            ReportProgress();

            List<uint> chainMap = this.volume.FileAllocationTable.GetClusterChain(dirent);

            TryIOOperation(() =>
            {
                WriteFile(path, dirent, chainMap);

                FileSetTimeStamps(path, dirent);
            });

            if (cancellationToken.IsCancellationRequested)
            {
                cancellationToken.ThrowIfCancellationRequested();
            }
        }

        private void SaveDirectory(string path, DirectoryEntry dirent)
        {
            path = path + "\\" + dirent.FileName;
            Console.WriteLine(path);

            // Report where we are at
            currentFile = dirent.FileName;
            numSaved++;
            ReportProgress();

            Directory.CreateDirectory(path);

            foreach (DirectoryEntry child in dirent.Children)
            {
                SaveDirectoryEntry(path, child);
            }

            TryIOOperation(() =>
            {
                DirectorySetTimestamps(path, dirent);
            });

            if (cancellationToken.IsCancellationRequested)
            {
                cancellationToken.ThrowIfCancellationRequested();
            }
        }

        private void SaveDeleted(string path, DirectoryEntry dirent)
        {
            path = path + "\\" + dirent.FileName;

            currentFile = dirent.GetFullPath();

            Console.WriteLine($"{path}: Cannot save deleted files.");
        }

        private void SaveDirectoryEntry(string path, DirectoryEntry dirent)
        {
            if (dirent.IsDeleted())
            {
                SaveDeleted(path, dirent);
                return;
            }

            if (dirent.IsDirectory())
            {
                SaveDirectory(path, dirent);
            }
            else
            {
                SaveFile(path, dirent);
            }
        }
    }
}
