using System;
using System.Collections.Generic;
using System.IO;
using System.ComponentModel;

namespace FATX
{
    public class MetadataAnalyzer
    {
        private Volume _volume;
        private long _interval;
        private long _length;

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
        }

        public List<DirectoryEntry> Analyze(BackgroundWorker worker)
        {
            RecoverMetadata(worker);
            LinkFileSystem(worker);

            return _root;
        }

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

        public List<DirectoryEntry> GetRoot()
        {
            return _root;
        }

        private void RecoverMetadata(BackgroundWorker worker)
        {
            var maxClusters = _length / _interval;
            for (long cluster = 0; cluster < maxClusters; cluster++)
            {
                var clusterOffset = cluster * _interval;
                for (int i = 0; i < 256; i++)
                {
                    var direntOffset = clusterOffset + (i * 0x40);
                    _volume.SeekFileArea(direntOffset);
                    try
                    {
                        DirectoryEntry dirent = new DirectoryEntry(_volume);

                        if (IsValidDirent(dirent))
                        {
                            Console.WriteLine(String.Format("0x{0:X8}: {1}", direntOffset, dirent.FileName));
                            dirent.SetCluster((uint)cluster + 1);
                            dirent.Offset = direntOffset;
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
            var chainMap = _volume.GetClusterChain(parent.FirstCluster);
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

        private bool IsValidFileName(string fileName)
        {
            if (fileName == null)
            {
                return false;
            }

            foreach (char c in fileName)
            {
                if (VALID_CHARS.IndexOf(c) == -1)
                {
                    return false;
                }
            }

            return true;
        }

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

        private bool IsValidDateTime(TimeStamp dateTime)
        {
            if (dateTime == null)
            {
                return false;
            }

            // TODO: create settings to customize these specifics
            if (dateTime.Year > DateTime.Now.Year)
            {
                return false;
            }

            try
            {
                new DateTime(dateTime.Year, dateTime.Month, dateTime.Day, 
                             dateTime.Hour, dateTime.Minute, dateTime.Second);
            }
            catch (Exception e)
            {
                return false;
            }

            return true;
        }

        private bool IsValidFirstCluster(uint firstCluster)
        {
            if (firstCluster > _volume.MaxClusters)
            {
                return false;
            }

            return true;
        }

        private bool IsValidDirent(DirectoryEntry dirent)
        {
            if (!IsValidFirstCluster(dirent.FirstCluster))
            {
                return false;
            }

            if (!IsValidFileName(dirent.FileName))
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
