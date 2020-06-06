using System.Collections.Generic;
using System.Linq;

namespace FATX.Analyzers
{
    /// <summary>
    /// This may be used in the future for applying modifications at run-time
    /// for DirectoryEntries
    /// </summary>
    public class RankedDirectoryEntry
    {
        private bool isActive;
        private int ranking;
        private DirectoryEntry dirent;
        private List<uint> collisions;
        private List<uint> clusterChain;

        public RankedDirectoryEntry(DirectoryEntry dirent, bool isOriginal)
        {
            this.isActive = isOriginal;
            this.dirent = dirent;
            this.clusterChain = null;
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

        public List<uint> ClusterChain
        {
            get => clusterChain;
            set => clusterChain = value;
        }
    }

    public class IntegrityAnalyzer
    {
        /// <summary>
        /// Active volume to analyze against.
        /// </summary>
        private Volume volume;

        private Dictionary<long, RankedDirectoryEntry> direntList;

        /// <summary>
        /// Mapping of cluster indexes to a list of entities that occupy it.
        /// </summary>
        private Dictionary<uint, List<RankedDirectoryEntry>> clusterMap;

        public IntegrityAnalyzer(Volume volume)
        {
            this.volume = volume;
            direntList = new Dictionary<long, RankedDirectoryEntry>();

            clusterMap = new Dictionary<uint, List<RankedDirectoryEntry>>((int)volume.MaxClusters);
            for (uint i = 0; i < volume.MaxClusters; i++)
            {
                clusterMap[i] = new List<RankedDirectoryEntry>();
            }

            RegisterActiveDirectoryEntries(volume.GetRoot());

            // Now that we have registered them, let's update the cluster map
            UpdateClusterMap();
        }

        private void RegisterDirectoryEntry(DirectoryEntry dirent, bool active)
        {
            long key = dirent.Offset;
            // If the dirent is already registered, then we don't need to register it again
            if (!direntList.ContainsKey(key))
            {
                direntList[key] = new RankedDirectoryEntry(dirent, active);
            }
        }

        private void RegisterActiveDirectoryEntries(List<DirectoryEntry> dirents)
        {
            // Here we will first create RankedDirectoryEntry objects
            foreach (var dirent in dirents)
            {
                if (dirent.IsDeleted())
                {
                    // If it's deleted, then its not active
                    RegisterDirectoryEntry(dirent, false);
                }
                else
                {
                    RegisterDirectoryEntry(dirent, true);
                }

                if (dirent.IsDirectory())
                {
                    RegisterActiveDirectoryEntries(dirent.Children);
                }
            }
        }

        private List<uint> GenerateArtificialClusterChain(DirectoryEntry dirent)
        {
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

        private void UpdateClusters(RankedDirectoryEntry rankedDirent)
        {
            foreach (var cluster in rankedDirent.ClusterChain)
            {
                var occupants = clusterMap[(uint)cluster];
                if (!occupants.Contains(rankedDirent))
                    occupants.Add(rankedDirent);
            }
        }

        private void UpdateClusterMap()
        {
            // For each dirent in dirent list
            foreach (var pair in direntList)
            {
                var rankedDirent = pair.Value;

                // We handle active cluster chains conventionally
                if (rankedDirent.IsActive)
                {
                    if (rankedDirent.ClusterChain == null)
                    {
                        rankedDirent.ClusterChain = volume.GetClusterChain(rankedDirent.GetDirent());
                    }

                    UpdateClusters(rankedDirent);
                }
                // Otherwise, we generate an artificial cluster chain
                else
                {
                    var dirent = rankedDirent.GetDirent();

                    if (dirent.FileName.StartsWith("xdk_data") ||
                        dirent.FileName.StartsWith("xdk_file") ||
                        dirent.FileName.StartsWith("tempcda"))
                    {
                        // These are usually always large and/or corrupted
                        // TODO: still don't really know what these files are
                        rankedDirent.ClusterChain = new List<uint>();
                        continue;
                    }

                    if (rankedDirent.ClusterChain == null)
                    {
                        // Generate an artificial cluster chain
                        rankedDirent.ClusterChain = GenerateArtificialClusterChain(dirent);
                    }

                    UpdateClusters(rankedDirent);
                }
            }
        }

        private void RegisterInactiveDirectoryEntries(List<DirectoryEntry> dirents)
        {
            foreach (var dirent in dirents)
            {
                RegisterDirectoryEntry(dirent, false);

                if (dirent.IsDirectory())
                {
                    RegisterInactiveDirectoryEntries(dirent.Children);
                }
            }
        }

        public void MergeMetadataAnalysis(List<DirectoryEntry> recovered)
        {
            // Find new dirents
            RegisterInactiveDirectoryEntries(recovered);
            UpdateClusterMap();
            UpdateCollisions();
            PerformRanking();
        }

        private void UpdateCollisions()
        {
            foreach (var pair in direntList)
            {
                var rankedDirent = pair.Value;
                rankedDirent.Collisions = FindCollidingClusters(rankedDirent);
            }
        }

        private List<uint> FindCollidingClusters(RankedDirectoryEntry rankedDirent)
        {
            // Get a list of cluster who are possibly corrupted
            List<uint> collidingClusters = new List<uint>();

            // for each cluster used by this dirent, check if other dirents are
            // also claiming it.
            foreach (var cluster in rankedDirent.ClusterChain)
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
            foreach (var pair in direntList)
            {
                DoRanking(pair.Value);
            }
        }

        public RankedDirectoryEntry GetRankedDirectoryEntry(DirectoryEntry dirent)
        {
            foreach (var pair in direntList)
            {
                if (dirent.Offset == pair.Value.GetDirent().Offset)
                {
                    return pair.Value;
                }
            }

            // Dirent was not registered into ranking system
            return null;
        }

        public List<RankedDirectoryEntry> GetClusterOccupants(uint cluster)
        {
            List<RankedDirectoryEntry> occupants;

            if (clusterMap.ContainsKey(cluster))
            {
                occupants = clusterMap[cluster];
            }
            else
            {
                occupants = null;
            }

            return occupants;
        }
    }
}
