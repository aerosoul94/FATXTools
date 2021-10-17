using System.Collections.Generic;

using FATX.FileSystem;

using FATXTools.Database;

namespace FATX.Analyzers
{
    public class IntegrityAnalyzer
    {
        /// <summary>
        /// Active volume to analyze against.
        /// </summary>
        private Volume _volume;

        private FileDatabase _database;

        /// <summary>
        /// Mapping of cluster indexes to a list of entities that occupy it.
        /// </summary>
        private Dictionary<uint, List<DatabaseFile>> _clusterMap;

        public IntegrityAnalyzer(Volume volume, FileDatabase database)
        {
            _volume = volume;
            _database = database;

            // Now that we have registered them, let's update the cluster map
            UpdateClusterMap();
        }

        private void UpdateClusters(DatabaseFile databaseFile)
        {
            foreach (var cluster in databaseFile.ClusterChain)
            {
                var occupants = _clusterMap[(uint)cluster];
                if (!occupants.Contains(databaseFile))
                    occupants.Add(databaseFile);
            }
        }

        private void UpdateClusterMap()
        {
            _clusterMap = new Dictionary<uint, List<DatabaseFile>>((int)_volume.MaxClusters);

            for (uint i = 0; i < _volume.MaxClusters; i++)
                _clusterMap[i] = new List<DatabaseFile>();

            foreach (var pair in _database.GetFiles())
            {
                var databaseFile = pair.Value;

                // We handle active cluster chains conventionally
                if (!databaseFile.IsDeleted)
                    UpdateClusters(databaseFile);
                // Otherwise, we generate an artificial cluster chain
                else
                {
                    // TODO: Add a blocklist setting
                    if (databaseFile.FileName.StartsWith("xdk_data") ||
                        databaseFile.FileName.StartsWith("xdk_file") ||
                        databaseFile.FileName.StartsWith("tempcda"))
                        // These are usually always large and/or corrupted
                        // TODO: still don't really know what these files are
                        continue;

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
            foreach (var databaseFile in _database.GetFiles().Values)
                databaseFile.SetCollisions(FindCollidingClusters(databaseFile));
        }

        private List<uint> FindCollidingClusters(DatabaseFile databaseFile)
        {
            // Get a list of cluster who are possibly corrupted
            //List<uint> collidingClusters = new List<uint>();

            //// for each cluster used by this dirent, check if other dirents are
            //// also claiming it.
            //foreach (var cluster in databaseFile.ClusterChain)
            //    if (_clusterMap[(uint)cluster].Count > 1)
            //        collidingClusters.Add((uint)cluster);

            return databaseFile.ClusterChain.FindAll(cluster => _clusterMap[(uint)cluster].Count > 1);
        }

        private bool WasModifiedLast(DatabaseFile databaseFile, List<uint> collisions)
        {
            var dirent = databaseFile.GetDirent();
            foreach (var cluster in collisions)
            {
                var clusterEnts = _clusterMap[(uint)cluster];
                foreach (var ent in clusterEnts)
                {
                    var entDirent = ent.GetDirent();

                    // Skip when we encounter the same dirent
                    if (dirent.Offset == entDirent.Offset)
                        continue;

                    // Return false if the file's last access time is earlier than one of it's occupants.
                    if (dirent.LastAccessTime.AsDateTime() < entDirent.LastAccessTime.AsDateTime())
                        return false;
                }
            }

            return true;
        }

        enum FileStatus
        {
            /// <summary>
            /// Rank 1
            ///  - Part of active file system
            ///  - Not deleted
            /// </summary>
            Green = 0,

            /// <summary>
            /// Rank 2
            ///  - Recovered
            ///  - No conflicting clusters detected
            /// </summary>
            YellowGreen = 1,

            /// <summary>
            /// Rank 3
            ///  - Recovered
            ///  - Some conflicting clusters
            ///  - Most recent data written
            /// </summary>
            Yellow = 2,

            /// <summary>
            /// Rank 4
            ///  - Recovered
            ///  - Some conflicting clusters
            ///  - Not most recent data written
            /// </summary>
            Orange = 3,

            /// <summary>
            /// Rank 5
            ///  - Recovered
            ///  - All clusters overwritten
            /// </summary>
            Red = 4
        }

        private FileStatus RankFile(DatabaseFile databaseFile)
        {
            var dirent = databaseFile.GetDirent();

            // If file was not deleted
            if (!databaseFile.IsDeleted && !dirent.IsDeleted())
                return FileStatus.Green;

            var collisions = databaseFile.GetCollisions();

            // If file has no colliding claimed clusters
            if (collisions.Count == 0)
                return FileStatus.YellowGreen;

            // The file has collisions, but it was modified last
            if (WasModifiedLast(databaseFile, collisions))
                return FileStatus.Yellow;

            // The file was determined to be overwritten, check if it was only partially overwritten
            var numClusters = (int)(((dirent.FileSize + (_volume.BytesPerCluster - 1)) &
                ~(_volume.BytesPerCluster - 1)) / _volume.BytesPerCluster);

            if (collisions.Count != numClusters)
                return FileStatus.Orange;

            // The file appears to be entirely overwritten
            return FileStatus.Red;
        }

        private void DoRanking(DatabaseFile databaseFile)
        {
            var ranking = (int)RankFile(databaseFile);

            databaseFile.SetRanking(ranking);
        }

        private void PerformRanking()
        {
            foreach (var pair in _database.GetFiles())
                DoRanking(pair.Value);
        }

        public List<DatabaseFile> GetClusterOccupants(uint cluster)
        {
            return _clusterMap.ContainsKey(cluster) ? _clusterMap[cluster] : null;
        }
    }
}
