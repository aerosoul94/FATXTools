using System.Collections.Generic;
using System.Linq;

using FATX.FileSystem;

namespace FATXTools.Database
{
    public class FileDatabase
    {
        /// <summary>
        /// Map of files according to its offset
        /// </summary>
        Dictionary<long, DatabaseFile> _files;

        /// <summary>
        /// List of files at the root of the file system.
        /// </summary>
        List<DatabaseFile> _root;

        /// <summary>
        /// Volume associated with this database.
        /// </summary>
        Volume _volume;

        public FileDatabase(Volume volume)
        {
            _files = new Dictionary<long, DatabaseFile>();
            _root = new List<DatabaseFile>();
            _volume = volume;

            MergeActiveFileSystem(volume);
        }

        /// <summary>
        /// Returns the number of files in this database.
        /// </summary>
        /// <returns>Number of files in this database.</returns>
        public int Count()
        {
            return _files.Count;
        }

        /// <summary>
        /// Update the file system in this database. This should be called after 
        /// any modifications are made to the database.
        /// </summary>
        public void Update()
        {
            // TODO: Only update affected files

            // Construct a new file system.
            _root = new List<DatabaseFile>();

            // Link the file system together.
            LinkFileSystem();
        }

        public void Reset()
        {
            _files = new Dictionary<long, DatabaseFile>();

            MergeActiveFileSystem(_volume);
        }

        private void FindChildren(DatabaseFile parent)
        {
            var chainMap = parent.ClusterChain;
            foreach (var child in _files)
            {
                if (chainMap.Contains(child.Value.Cluster))
                {
                    //if (child.Value.HasParent())
                    //{
                    //    Console.WriteLine("Warning: {0} already has a parent", child.Value.FileName);
                    //}
                    // TODO: Use a HashSet or something..
                    // Add the file as a child of the parent.
                    if (!parent.Children.Contains(child.Value))
                        parent.Children.Add(child.Value);

                    // TODO: What if this file has multiple parents?
                    // Assign the parent file for this file.
                    if (child.Value.GetParent() != parent)
                        child.Value.SetParent(parent);
                }
            }
        }

        /// <summary>
        /// Build the file system.
        /// </summary>
        private void LinkFileSystem()
        {
            // Clear all previous links
            foreach (var file in _files.Values)
            {
                file.Children = new List<DatabaseFile>();
                file.SetParent(null);
            }

            // Link all of the files together
            foreach (var file in _files.Values)
                if (file.IsDirectory())
                    FindChildren(file);

            // Gather files at the root
            foreach (var file in _files.Values)
                if (!file.HasParent())
                    _root.Add(file);
        }

        /// <summary>
        /// Merge the active file system into this database.
        /// </summary>
        /// <param name="volume">The active file system.</param>
        private void MergeActiveFileSystem(Volume volume)
        {
            RegisterDirectoryEntries(volume.Root);
        }

        /// <summary>
        /// Register directory entries in bulk.
        /// </summary>
        /// <param name="dirents"></param>
        private void RegisterDirectoryEntries(List<DirectoryEntry> dirents)
        {
            foreach (var dirent in dirents)
            {
                if (dirent.IsDeleted())
                    AddFile(dirent, true);
                else
                {
                    AddFile(dirent, false);

                    if (dirent.IsDirectory())
                        RegisterDirectoryEntries(dirent.Children);
                }
            }
        }

        /// <summary>
        /// Generates a new cluster chain for a deleted file.
        /// </summary>
        /// <param name="dirent">The deleted file.</param>
        /// <returns></returns>
        private List<uint> GenerateArtificialClusterChain(DirectoryEntry dirent)
        {
            // TODO: Check for zeroed FirstCluster

            if (dirent.IsDirectory())
            {
                // NOTE: Directories with more than one 256 files would have multiple clusters
                return new List<uint>() { dirent.FirstCluster };
            }
            else
            {
                var clusterCount = (int)(((dirent.FileSize + (_volume.BytesPerCluster - 1)) &
                         ~(_volume.BytesPerCluster - 1)) / _volume.BytesPerCluster);

                return Enumerable.Range((int)dirent.FirstCluster, clusterCount).Select(i => (uint)i).ToList();
            }
        }

        /// <summary>
        /// Get a file by the offset into the file system.
        /// </summary>
        /// <param name="offset">File area offset</param>
        /// <returns>DatabaseFile from this database</returns>
        public DatabaseFile GetFile(long offset)
        {
            return _files.ContainsKey(offset) ? _files[offset] : null;
        }

        /// <summary>
        /// Get a file by an instance of a DirectoryEntry.
        /// </summary>
        /// <param name="dirent">DirectoryEntry instance</param>
        /// <returns>DatabaseFile from this database</returns>
        public DatabaseFile GetFile(DirectoryEntry dirent)
        {
            return _files.ContainsKey(dirent.Offset) ? _files[dirent.Offset] : null;
        }

        /// <summary>
        /// Create and initialize a new DatabaseFile for this DirectoryEntry.
        /// </summary>
        /// <param name="offset"></param>
        /// <param name="dirent"></param>
        /// <param name="deleted"></param>
        private DatabaseFile CreateDatabaseFile(DirectoryEntry dirent, bool deleted)
        {
            _files[dirent.Offset] = new DatabaseFile(dirent, deleted)
            {
                ClusterChain = deleted ? GenerateArtificialClusterChain(dirent) : _volume.FileAllocationTable.GetClusterChain(dirent)
            };

            return _files[dirent.Offset];
        }

        /// <summary>
        /// Add and initialize a new DatabaseFile.
        /// </summary>
        /// <param name="dirent">The associated DirectoryEntry</param>
        /// <param name="deleted">Whether or not this file was deleted</param>
        public DatabaseFile AddFile(DirectoryEntry dirent, bool deleted)
        {
            // Create the file if it was not already added
            return _files.ContainsKey(dirent.Offset) ? _files[dirent.Offset] : CreateDatabaseFile(dirent, deleted);
        }

        /// <summary>
        /// Get all files from this database.
        /// </summary>
        /// <returns></returns>
        public Dictionary<long, DatabaseFile> GetFiles()
        {
            return _files;
        }

        /// <summary>
        /// Get the root files from this database's file system.
        /// </summary>
        /// <returns></returns>
        public List<DatabaseFile> GetRootFiles()
        {
            return _root;
        }

        /// <summary>
        /// Get the volume associated with this database.
        /// </summary>
        /// <returns></returns>
        public Volume GetVolume()
        {
            return _volume;
        }
    }
}
