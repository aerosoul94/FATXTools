using FATXTools.Database;
using System.Collections.Generic;
using System.Linq;

namespace FATX.Analyzers
{
    public class IntegrityAnalyzer
    {
        /// <summary>
        /// Active volume to analyze against.
        /// </summary>
        private Volume volume;

        //private Dictionary<long, RankedDirectoryEntry> direntList;
        private FileDatabase database;

        /// <summary>
        /// Mapping of cluster indexes to a list of entities that occupy it.
        /// </summary>
        private Dictionary<uint, List<RecoveredFile>> clusterMap;

        public IntegrityAnalyzer(Volume volume, FileDatabase database)
        {
            this.volume = volume;
            //direntList = new Dictionary<long, RankedDirectoryEntry>();
            this.database = database;

            clusterMap = new Dictionary<uint, List<RecoveredFile>>((int)volume.MaxClusters);
            for (uint i = 0; i < volume.MaxClusters; i++)
            {
                clusterMap[i] = new List<RecoveredFile>();
            }

            // Now that we have registered them, let's update the cluster map
            UpdateClusterMap();
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

        private void UpdateClusters(RecoveredFile rankedDirent)
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
            foreach (var pair in database.GetFiles())
            {
                var rankedDirent = pair.Value;

                // We handle active cluster chains conventionally
                if (!rankedDirent.IsDeleted)
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

        public void Update()
        {
            UpdateClusterMap();
            UpdateCollisions();
            PerformRanking();
        }

        private void UpdateCollisions()
        {
            foreach (var pair in database.GetFiles())
            {
                var rankedDirent = pair.Value;
                rankedDirent.Collisions = FindCollidingClusters(rankedDirent);
            }
        }

        private List<uint> FindCollidingClusters(RecoveredFile rankedDirent)
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

        private bool WasModifiedLast(RecoveredFile rankedDirent, List<uint> collisions)
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

        private void DoRanking(RecoveredFile recoveredFile)
        {
            var dirent = recoveredFile.GetDirent();

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

            if (!recoveredFile.IsDeleted)
            {
                if (!dirent.IsDeleted())
                {
                    recoveredFile.Ranking = (0);
                }
            }
            else
            {
                // File was deleted
                var collisions = recoveredFile.Collisions;
                if (collisions.Count == 0)
                {
                    recoveredFile.Ranking = (1);
                }
                else
                {
                    // File has colliding clusters
                    if (WasModifiedLast(recoveredFile, collisions))
                    {
                        // This file appears to have been written most recently.
                        recoveredFile.Ranking = (2);
                    }
                    else
                    {
                        // File was predicted to be overwritten
                        var numClusters = (int)(((dirent.FileSize + (this.volume.BytesPerCluster - 1)) &
                            ~(this.volume.BytesPerCluster - 1)) / this.volume.BytesPerCluster);
                        if (collisions.Count != numClusters)
                        {
                            // Not every cluster was overwritten
                            recoveredFile.Ranking = (3);
                        }
                        else
                        {
                            // Every cluster appears to have been overwritten
                            recoveredFile.Ranking = (4);
                        }
                    }
                }
            }
        }

        private void PerformRanking()
        {
            foreach (var pair in database.GetFiles())
            {
                DoRanking(pair.Value);
            }
        }

        public RecoveredFile GetRankedDirectoryEntry(DirectoryEntry dirent)
        {
            foreach (var pair in database.GetFiles())
            {
                if (dirent.Offset == pair.Value.GetDirent().Offset)
                {
                    return pair.Value;
                }
            }

            // Dirent was not registered into ranking system
            return null;
        }

        public List<RecoveredFile> GetClusterOccupants(uint cluster)
        {
            List<RecoveredFile> occupants;

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
