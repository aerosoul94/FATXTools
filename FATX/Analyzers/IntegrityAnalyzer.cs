using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FATX.Analyzers
{
    public class RankedDirectoryEntry
    {
        private bool isActive;
        private int ranking;
        private DirectoryEntry dirent;
        private List<uint> collisions;

        public RankedDirectoryEntry(DirectoryEntry dirent, bool isOriginal)
        {
            this.isActive = isOriginal;
            this.dirent = dirent;
        }

        public void GiveRanking(int rank)
        {
            this.ranking = rank;
        }

        public DirectoryEntry GetDirent()
        {
            return dirent;
        }

        public bool IsActive
        {
            get => isActive;
        }

        public int Ranking
        {
            get => ranking;
            set => ranking = value;
        }

        public List<uint> Collisions
        {
            get => collisions;
            set => collisions = value;
        }
    }

    public class IntegrityAnalyzer
    {
        /// <summary>
        /// Active volume to analyze against.
        /// </summary>
        private Volume volume;

        /// <summary>
        /// Directory entries that are part of the active (were not deleted) file system.
        /// </summary>
        private List<DirectoryEntry> activeDirents;

        /// <summary>
        /// List of all directory entries with assigned ranks;
        /// </summary>
        private List<RankedDirectoryEntry> rankedDirents;

        /// <summary>
        /// Mapping of cluster indexes to a list of entities that occupy it.
        /// </summary>
        private Dictionary<uint, List<RankedDirectoryEntry>> clusterMap;

        public IntegrityAnalyzer(Volume volume)
        {
            this.volume = volume;
            activeDirents = new List<DirectoryEntry>();
            rankedDirents = new List<RankedDirectoryEntry>();

            clusterMap = new Dictionary<uint, List<RankedDirectoryEntry>>((int)volume.MaxClusters);
            for (uint i = 0; i < volume.MaxClusters; i++)
            {
                clusterMap[i] = new List<RankedDirectoryEntry>();
            }

            RegisterDirents(volume.GetRoot());
        }

        private void RegisterDirents(List<DirectoryEntry> dirents)
        {
            foreach (var dirent in dirents)
            {
                activeDirents.Add(dirent);
                if (dirent.IsDeleted())
                {
                    rankedDirents.Add(new RankedDirectoryEntry(dirent, false));
                }
                else
                {
                    rankedDirents.Add(new RankedDirectoryEntry(dirent, true));
                }

                if (dirent.IsDirectory())
                {
                    RegisterDirents(dirent.GetChildren());
                }
            }
        }

        private bool IsInOriginalFileSystem(DirectoryEntry dirent)
        {
            foreach (var originalDirent in activeDirents)
            {
                if (originalDirent.Offset == dirent.Offset)
                {
                    return true;
                }
            }

            return false;
        }

        public void AddFileSystem(List<DirectoryEntry> newfs)
        {
            // Find new dirents
            foreach (var dirent in newfs)
            {
                // Should only add those that aren't already registered
                if (!IsInOriginalFileSystem(dirent))
                {
                    rankedDirents.Add(new RankedDirectoryEntry(dirent, false));
                }
            }

            InitializeClusterMap();
            InitializeRankedDirents();
            PerformRanking();
        }

        private void InitializeRankedDirents()
        {
            foreach (var rankedDirent in rankedDirents)
            {
                rankedDirent.Collisions = FindCollidingClusters(rankedDirent);
            }
        }

        private void InitializeClusterMap()
        {
            foreach (var rankedDirent in rankedDirents)
            {
                if (rankedDirent.IsActive)
                {
                    foreach (var cluster in volume.GetClusterChain(rankedDirent.GetDirent()))
                    {
                        clusterMap[cluster].Add(rankedDirent);
                    }
                }
                else
                {
                    var dirent = rankedDirent.GetDirent();

                    if (dirent.FileName.StartsWith("xdk_data"))
                    {
                        continue;
                    }

                    var firstCluster = rankedDirent.GetDirent().FirstCluster;
                    var numClusters = (int)(((dirent.FileSize + (this.volume.BytesPerCluster - 1)) &
                             ~(this.volume.BytesPerCluster - 1)) / this.volume.BytesPerCluster);
                    var clusterChain = Enumerable.Range((int)dirent.FirstCluster, (int)numClusters).ToList();
                    foreach (var cluster in clusterChain)
                    {
                        clusterMap[(uint)cluster].Add(rankedDirent);
                    }
                }
            }
        }

        private List<uint> FindCollidingClusters(RankedDirectoryEntry rankedDirent)
        {
            List<uint> collidingClusters = new List<uint>();
            var dirent = rankedDirent.GetDirent();
            var numClusters = (int)(((dirent.FileSize + (this.volume.BytesPerCluster - 1)) &
                     ~(this.volume.BytesPerCluster - 1)) / this.volume.BytesPerCluster);
            var clusterChain = Enumerable.Range((int)dirent.FirstCluster, (int)numClusters).ToList();
            foreach (var cluster in clusterChain)
            {
                if (clusterMap[(uint)cluster].Count > 1)
                {
                    collidingClusters.Add((uint)cluster);
                }
            }
            return collidingClusters;
        }

        private bool WasModifiedLast(RankedDirectoryEntry rankedDirent, List<uint> collisions)
        {
            var dirent = rankedDirent.GetDirent();
            foreach (var cluster in collisions)
            {
                var clusterEnts = clusterMap[(uint)cluster];
                foreach (var ent in clusterEnts)
                {
                    var entDirent = ent.GetDirent();

                    // Skip when we encounter the same dirent
                    if (dirent.Offset == entDirent.Offset)
                    {
                        continue;
                    }

                    if (dirent.LastAccessTime.AsDateTime() < entDirent.LastAccessTime.AsDateTime())
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private void DoRanking(RankedDirectoryEntry rankedDirent)
        {
            var dirent = rankedDirent.GetDirent();

            // Rank 1
            // - Part of active file system
            // - Not deleted
            // Rank 2
            // - Recovered
            // - No conflicting clusters detected
            // Rank 3
            // - Recovered
            // - Some conflicting clusters
            // - Most recent data written
            // Rank 4
            // - Recovered
            // - Some conflicting clusters
            // - Not most recent data written
            // Rank 5
            // - Recovered
            // - All clusters overwritten

            if (rankedDirent.IsActive)
            {
                if (!dirent.IsDeleted())
                {
                    rankedDirent.Ranking = (0);
                }
            }
            else
            {
                // File was deleted
                var collisions = rankedDirent.Collisions;
                if (collisions.Count == 0)
                {
                    rankedDirent.Ranking = (1);
                }
                else
                {
                    // File has colliding clusters
                    if (WasModifiedLast(rankedDirent, collisions))
                    {
                        // This file appears to have been written most recently.
                        rankedDirent.Ranking = (2);
                    }
                    else
                    {
                        // File was predicted to be overwritten
                        var numClusters = (int)(((dirent.FileSize + (this.volume.BytesPerCluster - 1)) &
                            ~(this.volume.BytesPerCluster - 1)) / this.volume.BytesPerCluster);
                        if (collisions.Count != numClusters)
                        {
                            // Not every cluster was overwritten
                            rankedDirent.Ranking = (3);
                        }
                        else
                        {
                            // Every cluster appears to have been overwritten
                            rankedDirent.Ranking = (4);
                        }
                    }
                }
            }
        }

        private void PerformRanking()
        {
            foreach (var rankedDirent in rankedDirents)
            {
                DoRanking(rankedDirent);
            }
        }

        public RankedDirectoryEntry GetRankedDirectoryEntry(DirectoryEntry dirent)
        {
            foreach (var rankedDirent in rankedDirents)
            {
                if (dirent.Offset == rankedDirent.GetDirent().Offset)
                {
                    return rankedDirent;
                }
            }

            // Dirent was not registered into ranking system
            return null;
        }

        public List<RankedDirectoryEntry> GetClusterOccupants(uint cluster)
        {
            return clusterMap[cluster];
        }
    }
}
