using System;
using System.Collections.Generic;
using System.IO;
using System.ComponentModel;
using System.Diagnostics;

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

        public List<DirectoryEntry> Analyze(BackgroundWorker worker)
        {
            var sw = new Stopwatch();
            sw.Start();
            RecoverMetadata(worker);
            sw.Stop();
            Console.WriteLine($"Execution Time: {sw.ElapsedMilliseconds} ms");
            Console.WriteLine($"Found {_dirents.Count} dirents.");
            LinkFileSystem(worker);

            return _root;
        }

        /// <summary>
        /// Searches for dirent's.
        /// </summary>
        /// <param name="worker"></param>
        private void RecoverMetadata(BackgroundWorker worker)
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
                            dirent.SetCluster(cluster);
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

                if ((cluster % 12) == 0)
                {
                    worker.ReportProgress((int)(cluster));
                }
            }
        }

        private void FindChildren(DirectoryEntry parent)
        {
            //var chainMap = _volume.GetClusterChain(parent.FirstCluster);
            var chainMap = new List<uint>() { parent.FirstCluster };
            foreach (var child in _dirents)
            {
                if (chainMap.Contains(child.GetCluster()))
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
        private void LinkFileSystem(BackgroundWorker worker)
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

            foreach (DirectoryEntry child in dirent.GetChildren())
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
                    _volume.Reader.Read(buf, (int)read);
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

            if (firstCluster == 0)
            {
                return false;
            }

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
