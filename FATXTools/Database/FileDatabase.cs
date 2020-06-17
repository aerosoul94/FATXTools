using FATX;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace FATXTools.Database
{
    public class FileDatabase
    {
        /// <summary>
        /// Map of files according to its offset
        /// </summary>
        Dictionary<long, DatabaseFile> files;

        /// <summary>
        /// List of files at the root of the file system.
        /// </summary>
        List<DatabaseFile> root;

        /// <summary>
        /// Volume associated with this database.
        /// </summary>
        Volume volume;

        public FileDatabase(Volume volume)
        {
            this.files = new Dictionary<long, DatabaseFile>();
            this.root = new List<DatabaseFile>();
            this.volume = volume;

            MergeActiveFileSystem(volume);
        }

        /// <summary>
        /// Returns the number of files in this database.
        /// </summary>
        /// <returns>Number of files in this database.</returns>
        public int Count()
        {
            return files.Count;
        }

        /// <summary>
        /// Update the file system in this database. This should be called after 
        /// any modifications are made to the database.
        /// </summary>
        public void Update()
        {
            // TODO: Only update affected files

            // Construct a new file system.
            root = new List<DatabaseFile>();

            // Link the file system together.
            LinkFileSystem();
        }

        private void FindChildren(DatabaseFile parent)
        {
            //if (parent.FileName == "ears_godfather")
            //    Debugger.Break();

            var chainMap = parent.ClusterChain;
            foreach (var child in files)
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

        private void LinkFileSystem()
        {
            foreach (var file in files)
            {
                if (file.Value.IsDirectory())
                {
                    FindChildren(file.Value);
                }
            }

            foreach (var file in files)
            {
                if (!file.Value.HasParent())
                {
                    root.Add(file.Value);
                }
            }
        }

        /// <summary>
        /// Merge the active file system into this database.
        /// </summary>
        /// <param name="volume">The active file system.</param>
        public void MergeActiveFileSystem(Volume volume)
        {
            RegisterDirectoryEntries(volume.GetRoot());
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
                var clusterCount = (int)(((dirent.FileSize + (this.volume.BytesPerCluster - 1)) &
                         ~(this.volume.BytesPerCluster - 1)) / this.volume.BytesPerCluster);

                return Enumerable.Range((int)dirent.FirstCluster, clusterCount).Select(i => (uint)i).ToList();
            }
        }

        private void RegisterDirectoryEntries(List<DirectoryEntry> dirents)
        {
            foreach (var dirent in dirents)
            {
                if (dirent.IsDeleted())
                {
                    AddFile(dirent, true);
                }
                else
                {
                    AddFile(dirent, false);

                    if (dirent.IsDirectory())
                    {
                        RegisterDirectoryEntries(dirent.Children);
                    }
                }
            }
        }

        private void RegisterDeletedDirectoryEntries(List<DirectoryEntry> dirents)
        {
            foreach (var dirent in dirents)
            {
                AddFile(dirent, true);
            }
        }

        /// <summary>
        /// Merge results of the MetadataAnalyzer.
        /// </summary>
        /// <param name="analyzer">MetadataAnalyzer instance</param>
        public void MergeMetadataAnalysis(MetadataAnalyzer analyzer)
        {
            RegisterDeletedDirectoryEntries(analyzer.GetDirents());

            LinkFileSystem();
        }

        public void MergeFileCarver(FileCarver carver)
        {
            // TODO
        }

        /// <summary>
        /// Get a file by the offset into the file system.
        /// </summary>
        /// <param name="offset">File area offset</param>
        /// <returns>DatabaseFile from this database</returns>
        public DatabaseFile GetFile(long offset)
        {
            if (files.ContainsKey(offset))
            {
                return files[offset];
            }

            return null;
        }

        /// <summary>
        /// Get a file by an instance of a DirectoryEntry.
        /// </summary>
        /// <param name="dirent">DirectoryEntry instance</param>
        /// <returns>DatabaseFile from this database</returns>
        public DatabaseFile GetFile(DirectoryEntry dirent)
        {
            if (files.ContainsKey(dirent.Offset))
            {
                return files[dirent.Offset];
            }

            return null;
        }

        /// <summary>
        /// Create and initialize a new DatabaseFile for this DirectoryEntry.
        /// </summary>
        /// <param name="offset"></param>
        /// <param name="dirent"></param>
        /// <param name="deleted"></param>
        private DatabaseFile CreateDatabaseFile(DirectoryEntry dirent, bool deleted)
        {
            files[dirent.Offset] = new DatabaseFile(dirent, deleted);
            if (deleted)
            {
                files[dirent.Offset].ClusterChain = GenerateArtificialClusterChain(dirent);
            }
            else
            {
                files[dirent.Offset].ClusterChain = this.volume.GetClusterChain(dirent);
            }

            return files[dirent.Offset];
        }

        /// <summary>
        /// Add and intialize a new DatabaseFile.
        /// </summary>
        /// <param name="dirent">The associated DirectoryEntry</param>
        /// <param name="deleted">Whether or not this file was deleted</param>
        public DatabaseFile AddFile(DirectoryEntry dirent, bool deleted)
        {
            // Create the file if it was not already added
            if (!files.ContainsKey(dirent.Offset))
            {
                return CreateDatabaseFile(dirent, deleted);
            }

            return files[dirent.Offset];
        }

        /// <summary>
        /// Add an already instantiated DatabaseFile to this database.
        /// </summary>
        /// <param name="databaseFile"></param>
        public DatabaseFile AddFile(DatabaseFile databaseFile)
        {
            // Add the file if it does not already exist
            if (!files.ContainsKey(databaseFile.Offset))
            {
                return files[databaseFile.Offset] = databaseFile;
            }

            return files[databaseFile.Offset];
        }

        /// <summary>
        /// Get all files from this database.
        /// </summary>
        /// <returns></returns>
        public Dictionary<long, DatabaseFile> GetFiles()
        {
            return files;
        }

        /// <summary>
        /// Get the root files from this database's file system.
        /// </summary>
        /// <returns></returns>
        public List<DatabaseFile> GetRootFiles()
        {
            return root;
        }

        /// <summary>
        /// Get the volume associated with this database.
        /// </summary>
        /// <returns></returns>
        public Volume GetVolume()
        {
            return volume;
        }
    }
}
