using System;
using System.Collections.Generic;
using System.IO;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading;
using System.Text.Json;

namespace FATX
{
    public class MetadataAnalyzer
    {
        private Volume _volume;
        private long _interval;
        private long _length;

        private int _currentYear;

        private List<DirectoryEntry> _dirents = new List<DirectoryEntry>();
        private List<DirectoryEntry> _root = new List<DirectoryEntry>();

        private const string VALID_CHARS = "abcdefghijklmnopqrstuvwxyz" +
                                           "ABCDEFGHIJKLMNOPQRSTUVWXUZ" +
                                           "0123456789" +
                                           "!#$%&\'()-.@[]^_`{}~ " +
                                           "\xff";

        public MetadataAnalyzer(Volume volume, long interval, long length)
        {
            if (length == 0 || length > volume.FileAreaLength)
            {
                length = volume.FileAreaLength;
            }

            this._volume = volume;
            this._interval = interval;
            this._length = length;

            this._currentYear = DateTime.Now.Year;
        }

        public List<DirectoryEntry> Analyze(CancellationToken cancellationToken, IProgress<int> progress)
        {
            var sw = new Stopwatch();
            sw.Start();
            RecoverMetadata(cancellationToken, progress);
            LinkFileSystem();
            sw.Stop();
            Console.WriteLine($"Execution Time: {sw.ElapsedMilliseconds} ms");
            Console.WriteLine($"Found {_dirents.Count} dirents.");

            return _root;
        }

        public DirectoryEntry LoadDirectoryEntryFromDatabase(JsonElement directoryEntryObject)
        {
            JsonElement offsetElement;
            if (!directoryEntryObject.TryGetProperty("Offset", out offsetElement))
            {
                Console.WriteLine("Failed to load metadata object from database: Missing offset field");
                return null;
            }

            JsonElement clusterElement;
            if (!directoryEntryObject.TryGetProperty("Cluster", out clusterElement))
            {
                Console.WriteLine("Failed to load metadata object from database: Missing cluster field");
                return null;
            }

            long offset = offsetElement.GetInt64();
            uint cluster = clusterElement.GetUInt32();

            if (offset < this._volume.Offset || offset > this._volume.Offset + this._volume.Length)
            {
                Console.WriteLine($"Failed to load metadata object from database: Invalid offset {offset}");
                return null;
            }

            if (cluster < 0 || cluster > this._volume.MaxClusters)
            {
                Console.WriteLine($"Failed to load metadata object from database: Invalid cluster {cluster}");
                return null;
            }

            byte[] data = new byte[0x40];
            this._volume.GetReader().Seek(offset);
            this._volume.GetReader().Read(data, 0x40);

            var directoryEntry = new DirectoryEntry(this._volume, data, 0);
            directoryEntry.Offset = offset;
            directoryEntry.Cluster = cluster;

            _dirents.Add(directoryEntry);

            if (directoryEntryObject.TryGetProperty("Children", out var childrenElement))
            {
                foreach (var childElement in childrenElement.EnumerateArray())
                {
                    var child = LoadDirectoryEntryFromDatabase(childElement);
                    directoryEntry.AddChild(child);
                }
            }

            if (directoryEntryObject.TryGetProperty("Clusters", out var clustersElement))
            {
                // TODO: load cluster chain from file
                // Currently we always generate or load from the fat
                //var clusterList = directoryEntryObject["Clusters"] as List<uint>;
                //directoryEntry.ClusterChain = clusterList;
            }

            return directoryEntry;
        }

        public void LoadFromDatabase(JsonElement metadataAnalysisObject)
        {
            foreach (var directoryEntryObject in metadataAnalysisObject.EnumerateArray())
            {
                var directoryEntry = LoadDirectoryEntryFromDatabase(directoryEntryObject);

                _root.Add(directoryEntry);
            }
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
                var data = _volume.ReadCluster(cluster);
                var clusterOffset = (cluster - 1) * _interval;
                for (int i = 0; i < 256; i++)
                {
                    var direntOffset = (i * 0x40);
                    try
                    {
                        DirectoryEntry dirent = new DirectoryEntry(_volume, data, direntOffset);

                        if (IsValidDirent(dirent))
                        {
                            Console.WriteLine(String.Format("0x{0:X8}: {1}", clusterOffset + direntOffset, dirent.FileName));
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

        private void FindChildren(DirectoryEntry parent)
        {
            //var chainMap = _volume.GetClusterChain(parent.FirstCluster);
            var chainMap = new List<uint>() { parent.FirstCluster };
            foreach (var child in _dirents)
            {
                if (chainMap.Contains(child.Cluster))
                {
                    if (child.HasParent())
                    {
                        Console.WriteLine("{0} already has a parent", child.FileName);
                    }
                    parent.AddChild(child);
                    child.SetParent(parent);
                }
            }
        }

        /// <summary>
        /// Links all dirent's with their child dirent's.
        /// </summary>
        /// <param name="worker"></param>
        private void LinkFileSystem()
        {
            foreach (var dirent in _dirents)
            {
                if (dirent.IsDirectory())
                {
                    FindChildren(dirent);
                }
            }

            foreach (var dirent in _dirents)
            {
                if (!dirent.HasParent())
                {
                    _root.Add(dirent);
                }
            }
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

        /// <summary>
        /// Get the recovered root dirent's.
        /// </summary>
        /// <returns></returns>
        public List<DirectoryEntry> GetRootDirectory()
        {
            return _root;
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
            //  To be as thourough as we can, let's include those.
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
