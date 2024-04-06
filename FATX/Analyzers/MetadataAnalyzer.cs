using FATX.FileSystem;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace FATX.Analyzers
{
    public class MetadataAnalyzer
    {
        private Volume _volume;
        private long _interval;
        private long _length;

        private int _currentYear;

        private List<DirectoryEntry> _dirents = new List<DirectoryEntry>();

        private const string VALID_CHARS = "abcdefghijklmnopqrstuvwxyz" +
                                           "ABCDEFGHIJKLMNOPQRSTUVWXYZ" +
                                           "0123456789" +
                                           "!#$%&\'()-.@[]^_`{}~ " +
                                           "\xff";

        public MetadataAnalyzer(Volume volume, long interval, long length)
        {
            if (length == 0 || length > volume.FileAreaLength)
            {
                length = volume.FileAreaLength;
            }

            _volume = volume;
            _interval = interval;
            _length = length;

            _currentYear = DateTime.Now.Year;
        }

        public List<DirectoryEntry> Analyze(CancellationToken cancellationToken, IProgress<int> progress)
        {
            var sw = new Stopwatch();
            sw.Start();
            RecoverMetadata(cancellationToken, progress);
            sw.Stop();
            Console.WriteLine($"Execution Time: {sw.ElapsedMilliseconds} ms");
            Console.WriteLine($"Found {_dirents.Count} dirents.");

            return _dirents;
        }

        /// <summary>
        /// Searches for dirent's.
        /// </summary>
        /// <param name="worker"></param>
        private void RecoverMetadata(CancellationToken cancellationToken, IProgress<int> progress)
        {
            var maxClusters = _length / _interval;
            for (uint cluster = 1; cluster < maxClusters; cluster++)
            {
                byte[] data;

                try
                {
                    data = _volume.ReadCluster(cluster);
                }
                catch (IOException exception)
                {
                    // Failed to read data
                    Console.WriteLine(exception.Message);
                    Console.WriteLine($"Failed to read cluster {cluster}, skipping...");
                    continue;
                }
                
                var clusterOffset = (cluster - 1) * _interval;
                for (int i = 0; i < 256; i++)
                {
                    var direntOffset = i * 0x40;
                    try
                    {
                        DirectoryEntry dirent = new DirectoryEntry(_volume.Platform, data, direntOffset);

                        if (IsValidDirent(dirent))
                        {
                            Console.WriteLine(string.Format("0x{0:X8}: {1}", clusterOffset + direntOffset, dirent.FileName));
                            dirent.Cluster = cluster;
                            dirent.Offset = _volume.ClusterToPhysicalOffset(cluster) + direntOffset;
                            _dirents.Add(dirent);
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.Message);
                        Console.WriteLine(e.StackTrace);
                    }
                }

                if (cluster % 0x100 == 0)
                    progress?.Report((int)cluster);

                if (cancellationToken.IsCancellationRequested)
                {
                    // NOTE: even though this part of the analyzer returns,
                    //   it will continue on with the linking step to clean
                    //   up the results while keeping the progress bar
                    //   running to show that we are still working.
                    break;
                }
            }

            progress?.Report((int)maxClusters);
        }

        /// <summary>
        /// Dump a directory to path.
        /// </summary>
        /// <param name="dirent"></param>
        /// <param name="path"></param>
        private void DumpDirectory(DirectoryEntry dirent, string path)
        {
            path = path + "/" + dirent.FileName;
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            foreach (DirectoryEntry child in dirent.Children)
            {
                Dump(child, path);
            }

            Directory.SetCreationTime(path, dirent.CreationTime.AsDateTime());
            Directory.SetLastWriteTime(path, dirent.LastWriteTime.AsDateTime());
            Directory.SetLastAccessTime(path, dirent.LastAccessTime.AsDateTime());
        }

        /// <summary>
        /// Dump a file to path.
        /// </summary>
        /// <param name="dirent"></param>
        /// <param name="path"></param>
        private void DumpFile(DirectoryEntry dirent, string path)
        {
            path = path + "/" + dirent.FileName;
            const int bufsize = 0x100000;
            var remains = dirent.FileSize;
            _volume.SeekToCluster(dirent.FirstCluster);

            using (FileStream file = new FileStream(path, FileMode.Create))
            {
                while (remains > 0)
                {
                    var read = Math.Min(remains, bufsize);
                    remains -= read;
                    byte[] buf = new byte[read];
                    _volume.GetReader().Read(buf, (int)read);
                    file.Write(buf, 0, (int)read);
                }
            }

            File.SetCreationTime(path, dirent.CreationTime.AsDateTime());
            File.SetLastWriteTime(path, dirent.LastWriteTime.AsDateTime());
            File.SetLastAccessTime(path, dirent.LastAccessTime.AsDateTime());
        }

        /// <summary>
        /// Dumps a DirectoryEntry to path.
        /// </summary>
        /// <param name="dirent"></param>
        /// <param name="path"></param>
        public void Dump(DirectoryEntry dirent, string path)
        {
            if (dirent.IsDirectory())
            {
                DumpDirectory(dirent, path);
            }
            else
            {
                DumpFile(dirent, path);
            }
        }

        public List<DirectoryEntry> GetDirents()
        {
            return _dirents;
        }

        public Volume GetVolume()
        {
            return _volume;
        }

        /// <summary>
        /// Validate FileNameBytes.
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>
        private bool IsValidFileNameBytes(byte[] bytes)
        {
            foreach (byte b in bytes)
            {
                if (VALID_CHARS.IndexOf((char)b) == -1)
                {
                    return false;
                }
            }

            return true;
        }

        private const int ValidAttributes = 55;

        /// <summary>
        /// Validate FileAttributes.
        /// </summary>
        /// <param name="attributes"></param>
        /// <returns></returns>
        private bool IsValidAttributes(FileAttribute attributes)
        {
            if (attributes == 0)
            {
                return true;
            }

            if (!Enum.IsDefined(typeof(FileAttribute), attributes))
            {
                return false;
            }

            return true;
        }

        private int[] MaxDays =
        {
              31, // Jan
              29, // Feb
              31, // Mar
              30, // Apr
              31, // May
              30, // Jun
              31, // Jul
              31, // Aug
              30, // Sep
              31, // Oct
              30, // Nov
              31  // Dec
        };

        /// <summary>
        /// Validate a TimeStamp.
        /// </summary>
        /// <param name="dateTime"></param>
        /// <returns></returns>
        private bool IsValidDateTime(TimeStamp dateTime)
        {
            if (dateTime == null)
            {
                return false;
            }

            // TODO: create settings to customize these specifics
            if (dateTime.Year > _currentYear)
            {
                return false;
            }

            if (dateTime.Month > 12 || dateTime.Month < 1)
            {
                return false;
            }

            if (dateTime.Day > MaxDays[dateTime.Month - 1])
            {
                return false;
            }

            if (dateTime.Hour > 23 || dateTime.Hour < 0)
            {
                return false;
            }

            if (dateTime.Minute > 59 || dateTime.Minute < 0)
            {
                return false;
            }

            if (dateTime.Second > 59 || dateTime.Second < 0)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Validate FirstCluster.
        /// </summary>
        /// <param name="firstCluster"></param>
        /// <returns></returns>
        private bool IsValidFirstCluster(uint firstCluster)
        {
            if (firstCluster > _volume.MaxClusters)
            {
                return false;
            }

            // NOTE: deleted files have been found with firstCluster set to 0
            //  To be as thorough as we can, let's include those.
            //if (firstCluster == 0)
            //{
            //    return false;
            //}

            return true;
        }

        /// <summary>
        /// Validate FileNameLength.
        /// </summary>
        /// <param name="fileNameLength"></param>
        /// <returns></returns>
        private bool IsValidFileNameLength(uint fileNameLength)
        {
            if (fileNameLength == 0x00 || fileNameLength == 0x01 || fileNameLength == 0xff)
            {
                return false;
            }

            if (fileNameLength > 0x2a && fileNameLength != 0xe5)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Check if dirent is actually a dirent.
        /// </summary>
        /// <param name="dirent"></param>
        /// <returns></returns>
        private bool IsValidDirent(DirectoryEntry dirent)
        {
            if (!IsValidFileNameLength(dirent.FileNameLength))
            {
                return false;
            }

            if (!IsValidFirstCluster(dirent.FirstCluster))
            {
                return false;
            }

            if (!IsValidFileNameBytes(dirent.FileNameBytes))
            {
                return false;
            }

            if (!IsValidAttributes(dirent.FileAttributes))
            {
                return false;
            }

            if (!IsValidDateTime(dirent.CreationTime) ||
                !IsValidDateTime(dirent.LastAccessTime) ||
                !IsValidDateTime(dirent.LastWriteTime))
            {
                return false;
            }

            return true;
        }
    }
}
