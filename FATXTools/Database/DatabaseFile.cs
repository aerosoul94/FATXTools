using FATX;
using System.Collections.Generic;

namespace FATXTools.Database
{
    public class DatabaseFile
    {
        /// <summary>
        /// Whether or not this file has been deleted.
        /// </summary>
        private bool deleted;

        /// <summary>
        /// The status ranking given to this file.
        /// </summary>
        private int ranking;

        /// <summary>
        /// The DirectoryEntry this DatabaseFile represents.
        /// </summary>
        private DirectoryEntry dirent;

        /// <summary>
        /// List of files that this file collides with.
        /// </summary>
        private List<uint> collisions;

        /// <summary>
        /// This file's cluster chain.
        /// </summary>
        private List<uint> clusterChain;

        /// <summary>
        /// This file's parent.
        /// </summary>
        private DatabaseFile parent;

        public DatabaseFile(DirectoryEntry dirent, bool deleted)
        {
            this.deleted = deleted;
            this.dirent = dirent;
            this.clusterChain = null;
            this.parent = null;
            this.Children = new List<DatabaseFile>();
        }

        /// <summary>
        /// Counts and returns the number of files contained within this file.
        /// </summary>
        /// <returns>Number of files in this file</returns>
        public long CountFiles()
        {
            if (this.dirent.IsDeleted())
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
            return ranking;
        }

        public void SetRanking(int value)
        {
            ranking = value;
        }

        public List<uint> GetCollisions()
        {
            return collisions;
        }

        public void SetCollisions(List<uint> value)
        {
            collisions = value;
        }

        public DirectoryEntry GetDirent()
        {
            return dirent;
        }

        public void SetParent(DatabaseFile parent)
        {
            this.parent = parent;
        }

        public DatabaseFile GetParent()
        {
            return this.parent;
        }

        public bool HasParent()
        {
            return this.parent != null;
        }

        public bool IsDirectory()
        {
            return dirent.IsDirectory();
        }

        // TODO: Rename to IsRecovered? This conflicts with DirectoryEntry::IsDeleted()
        public bool IsDeleted => deleted;

        public List<DatabaseFile> Children
        {
            get;
            set;
        }

        public List<uint> ClusterChain
        {
            get => clusterChain;
            set => clusterChain = value;
        }

        public uint Cluster => dirent.Cluster;
        public long Offset => dirent.Offset;
        public uint FileNameLength => dirent.FileNameLength;
        public FileAttribute FileAttributes => dirent.FileAttributes;
        public string FileName => dirent.FileName;
        public byte[] FileNameBytes => dirent.FileNameBytes;
        public uint FirstCluster => dirent.FirstCluster;
        public uint FileSize => dirent.FileSize;
        public TimeStamp CreationTime => dirent.CreationTime;
        public TimeStamp LastWriteTime => dirent.LastWriteTime;
        public TimeStamp LastAccessTime => dirent.LastAccessTime;
    }
}
