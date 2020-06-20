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

        private FileDatabase database;

        /// <summary>
        /// Mapping of cluster indexes to a list of entities that occupy it.
        /// </summary>
        private Dictionary<uint, List<DatabaseFile>> clusterMap;

        public IntegrityAnalyzer(Volume volume, FileDatabase database)
        {
            this.volume = volume;
            this.database = database;

            // Now that we have registered them, let's update the cluster map
            UpdateClusterMap();
        }

        private void UpdateClusters(DatabaseFile databaseFile)
        {
            foreach (var cluster in databaseFile.ClusterChain)
            {
                var occupants = clusterMap[(uint)cluster];
                if (!occupants.Contains(databaseFile))
                    occupants.Add(databaseFile);
            }
        }

        private void UpdateClusterMap()
        {
            clusterMap = new Dictionary<uint, List<DatabaseFile>>((int)volume.MaxClusters);
            for (uint i = 0; i < volume.MaxClusters; i++)
            {
                clusterMap[i] = new List<DatabaseFile>();
            }

            foreach (var pair in database.GetFiles())
            {
                var databaseFile = pair.Value;

                // We handle active cluster chains conventionally
                if (!databaseFile.IsDeleted)
                {
                    UpdateClusters(databaseFile);
                }
                // Otherwise, we generate an artificial cluster chain
                else
                {
                    // TODO: Add a blocklist setting
                    if (databaseFile.FileName.StartsWith("xdk_data") ||
                        databaseFile.FileName.StartsWith("xdk_file") ||
                        databaseFile.FileName.StartsWith("tempcda"))
                    {
                        // These are usually always large and/or corrupted
                        // TODO: still don't really know what these files are
                        continue;
                    }

                    UpdateClusters(databaseFile);
                }
            }
        }

        public void Update()
        {
            UpdateClusterMap(); // Update clusterMap
            UpdateCollisions(); // Update collisions (Do the collision check)
            PerformRanking();   // Rank all clusters
        }

        private void UpdateCollisions()
        {
            foreach (var databaseFile in database.GetFiles().Values)
            {
                databaseFile.SetCollisions(FindCollidingClusters(databaseFile));
            }
        }

        private List<uint> FindCollidingClusters(DatabaseFile databaseFile)
        {
            // Get a list of cluster who are possibly corrupted
            List<uint> collidingClusters = new List<uint>();

            // for each cluster used by this dirent, check if other dirents are
            // also claiming it.
            foreach (var cluster in databaseFile.ClusterChain)
            {
                if (clusterMap[(uint)cluster].Count > 1)
                {
                    collidingClusters.Add((uint)cluster);
                }
            }

            return collidingClusters;
        }

        private bool WasModifiedLast(DatabaseFile databaseFile, List<uint> collisions)
        {
            var dirent = databaseFile.GetDirent();
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

        private void DoRanking(DatabaseFile databaseFile)
        {
            var dirent = databaseFile.GetDirent();

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

            if (!databaseFile.IsDeleted)
            {
                if (!dirent.IsDeleted())
                {
                    databaseFile.SetRanking((0));
                }
            }
            else
            {
                // File was deleted
                var collisions = databaseFile.GetCollisions();
                if (collisions.Count == 0)
                {
                    databaseFile.SetRanking((1));
                }
                else
                {
                    // File has colliding clusters
                    if (WasModifiedLast(databaseFile, collisions))
                    {
                        // This file appears to have been written most recently.
                        databaseFile.SetRanking((2));
                    }
                    else
                    {
                        // File was predicted to be overwritten
                        var numClusters = (int)(((dirent.FileSize + (this.volume.BytesPerCluster - 1)) &
                            ~(this.volume.BytesPerCluster - 1)) / this.volume.BytesPerCluster);
                        if (collisions.Count != numClusters)
                        {
                            // Not every cluster was overwritten
                            databaseFile.SetRanking((3));
                        }
                        else
                        {
                            // Every cluster appears to have been overwritten
                            databaseFile.SetRanking((4));
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

        public List<DatabaseFile> GetClusterOccupants(uint cluster)
        {
            List<DatabaseFile> occupants;

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
