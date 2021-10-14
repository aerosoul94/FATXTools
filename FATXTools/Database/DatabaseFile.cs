using System.Collections.Generic;

using FATX.FileSystem;

namespace FATXTools.Database
{
    public class DatabaseFile
    {
        /// <summary>
        /// Whether or not this file has been deleted.
        /// </summary>
        private bool _deleted;

        /// <summary>
        /// The status ranking given to this file.
        /// </summary>
        private int _ranking;

        /// <summary>
        /// The DirectoryEntry this DatabaseFile represents.
        /// </summary>
        private DirectoryEntry _dirent;

        /// <summary>
        /// List of files that this file collides with.
        /// </summary>
        private List<uint> _collisions;

        /// <summary>
        /// This file's cluster chain.
        /// </summary>
        private List<uint> _clusterChain;

        /// <summary>
        /// This file's parent.
        /// </summary>
        private DatabaseFile _parent;

        public DatabaseFile(DirectoryEntry dirent, bool deleted)
        {
            _deleted = deleted;
            _dirent = dirent;
            _clusterChain = null;
            _parent = null;

            Children = new List<DatabaseFile>();
        }

        /// <summary>
        /// Counts and returns the number of files contained within this file.
        /// </summary>
        /// <returns>Number of files in this file</returns>
        public long CountFiles()
        {
            if (_dirent.IsDeleted())
            {
                return 0;
            }

            if (IsDirectory())
            {
                long numFiles = 1;

                foreach (var dirent in Children)
                {
                    numFiles += dirent.CountFiles();
                }

                return numFiles;
            }
            else
            {
                return 1;
            }
        }

        public int GetRanking()
        {
            return _ranking;
        }

        public void SetRanking(int value)
        {
            _ranking = value;
        }

        public List<uint> GetCollisions()
        {
            return _collisions;
        }

        public void SetCollisions(List<uint> value)
        {
            _collisions = value;
        }

        public DirectoryEntry GetDirent()
        {
            return _dirent;
        }

        public void SetParent(DatabaseFile parent)
        {
            _parent = parent;
        }

        public DatabaseFile GetParent()
        {
            return _parent;
        }

        public bool HasParent()
        {
            return _parent != null;
        }

        public bool IsDirectory()
        {
            return _dirent.IsDirectory();
        }

        // TODO: Rename to IsRecovered? This conflicts with DirectoryEntry::IsDeleted()
        public bool IsDeleted => _deleted;

        public List<DatabaseFile> Children
        {
            get;
            set;
        }

        public List<uint> ClusterChain
        {
            get => _clusterChain;
            set => _clusterChain = value;
        }

        public uint Cluster => _dirent.Cluster;
        public long Offset => _dirent.Offset;
        public uint FileNameLength => _dirent.FileNameLength;
        public FileAttribute FileAttributes => _dirent.FileAttributes;
        public string FileName => _dirent.FileName;
        public byte[] FileNameBytes => _dirent.FileNameBytes;
        public uint FirstCluster => _dirent.FirstCluster;
        public uint FileSize => _dirent.FileSize;
        public TimeStamp CreationTime => _dirent.CreationTime;
        public TimeStamp LastWriteTime => _dirent.LastWriteTime;
        public TimeStamp LastAccessTime => _dirent.LastAccessTime;
    }
}
