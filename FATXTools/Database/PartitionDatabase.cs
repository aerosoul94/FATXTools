using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

using FATX.Analyzers;
using FATX.FileSystem;

namespace FATXTools.Database
{
    public class PartitionDatabase
    {
        // We just want this for the volume info (offset, length, name)
        Volume volume;

        //FileCarver fileCarver;  // TODO: Get rid of this. We should be able to get this information from the FileDatabase.

        /// <summary>
        ///  Whether or not the MetadataAnalyzer's results are in.
        /// </summary>
        bool metadataAnalyzer;

        /// <summary>
        /// Database that stores analysis results.
        /// </summary>
        FileDatabase fileDatabase;

        /// <summary>
        /// The associated view for this PartitionDatabase
        /// </summary>
        PartitionView view; // TODO: Use events instead.

        List<CarvedFile> carvedFiles;

        public event EventHandler OnLoadRecoveryFromDatabase;

        public PartitionDatabase(Volume volume)
        {
            this.volume = volume;
            this.metadataAnalyzer = false;
            //this.fileCarver = null;

            this.fileDatabase = new FileDatabase(volume);
        }

        /// <summary>
        /// Get the partition name associated with this database.
        /// </summary>
        public string PartitionName => volume.Name;

        public Volume Volume => volume;

        /// <summary>
        /// Set the associated view for this database.
        /// </summary>
        /// <param name="view"></param>
        public void SetPartitionView(PartitionView view)
        {
            this.view = view;
        }

        /// <summary>
        /// Set whether or not analysis was performed.
        /// </summary>
        /// <param name="metadataAnalyzer"></param>
        public void SetMetadataAnalyzer(bool metadataAnalyzer)
        {
            this.metadataAnalyzer = metadataAnalyzer;
        }

        /// <summary>
        /// Get the FileDatabase for this partition.
        /// </summary>
        /// <returns></returns>
        public FileDatabase GetFileDatabase()
        {
            return this.fileDatabase;
        }

        public void Save(Dictionary<string, object> partitionObject)
        {
            partitionObject["Name"] = this.volume.Name;
            partitionObject["Offset"] = this.volume.Offset;
            partitionObject["Length"] = this.volume.Length;

            partitionObject["Analysis"] = new Dictionary<string, object>();
            var analysisObject = partitionObject["Analysis"] as Dictionary<string, object>;

            analysisObject["MetadataAnalyzer"] = new List<Dictionary<string, object>>();
            if (metadataAnalyzer)
            {
                var metadataAnalysisList = analysisObject["MetadataAnalyzer"] as List<Dictionary<string, object>>;
                SaveMetadataAnalysis(metadataAnalysisList);
            }

            analysisObject["FileCarver"] = new List<Dictionary<string, object>>();
            if (carvedFiles.Count > 0)
            {
                var fileCarverObject = analysisObject["FileCarver"] as List<Dictionary<string, object>>;
                SaveFileCarver(fileCarverObject);
            }
        }

        private void SaveMetadataAnalysis(List<Dictionary<string, object>> metadataAnalysisList)
        {
            foreach (var databaseFile in fileDatabase.GetRootFiles())
            {
                var directoryEntryObject = new Dictionary<string, object>();
                metadataAnalysisList.Add(directoryEntryObject);
                SaveDirectoryEntry(directoryEntryObject, databaseFile);
            }
        }

        private void SaveFileCarver(List<Dictionary<string, object>> fileCarverList)
        {
            foreach (var file in carvedFiles)
            {
                var fileCarverObject = new Dictionary<string, object>();

                fileCarverObject["Offset"] = file.Offset;
                fileCarverObject["Name"] = file.FileName;
                fileCarverObject["Size"] = file.FileSize;

                fileCarverList.Add(fileCarverObject);
            }
        }

        private void SaveDirectoryEntry(Dictionary<string, object> directoryEntryObject, DatabaseFile directoryEntry)
        {
            directoryEntryObject["Cluster"] = directoryEntry.Cluster;
            directoryEntryObject["Offset"] = directoryEntry.Offset;

            /*
             * At this moment, I believe this will only be used for debugging.
             */
            directoryEntryObject["FileNameLength"] = directoryEntry.FileNameLength;
            directoryEntryObject["FileAttributes"] = (byte)directoryEntry.FileAttributes;
            directoryEntryObject["FileName"] = directoryEntry.FileName;
            directoryEntryObject["FileNameBytes"] = directoryEntry.FileNameBytes;
            directoryEntryObject["FirstCluster"] = directoryEntry.FirstCluster;
            directoryEntryObject["FileSize"] = directoryEntry.FileSize;
            directoryEntryObject["CreationTime"] = directoryEntry.CreationTime.AsInteger();
            directoryEntryObject["LastWriteTime"] = directoryEntry.LastWriteTime.AsInteger();
            directoryEntryObject["LastAccessTime"] = directoryEntry.LastAccessTime.AsInteger();

            if (directoryEntry.IsDirectory())
            {
                directoryEntryObject["Children"] = new List<Dictionary<string, object>>();
                var childrenList = directoryEntryObject["Children"] as List<Dictionary<string, object>>;
                foreach (var child in directoryEntry.Children)
                {
                    var childObject = new Dictionary<string, object>();
                    childrenList.Add(childObject);
                    SaveDirectoryEntry(childObject, child);
                }
            }

            directoryEntryObject["Clusters"] = directoryEntry.ClusterChain;
        }

        public DatabaseFile LoadDirectoryEntryFromDatabase(JsonElement directoryEntryObject)
        {
            // Make sure that the offset property was stored for this file
            JsonElement offsetElement;
            if (!directoryEntryObject.TryGetProperty("Offset", out offsetElement))
            {
                Console.WriteLine("Failed to load metadata object from database: Missing offset field");
                return null;
            }

            // Make sure that the cluster property was stored for this file
            JsonElement clusterElement;
            if (!directoryEntryObject.TryGetProperty("Cluster", out clusterElement))
            {
                Console.WriteLine("Failed to load metadata object from database: Missing cluster field");
                return null;
            }

            long offset = offsetElement.GetInt64();
            uint cluster = clusterElement.GetUInt32();

            // Ensure that the offset is within the bounds of the partition.
            if (offset < this.volume.Offset || offset > this.volume.Offset + this.volume.Length)
            {
                Console.WriteLine($"Failed to load metadata object from database: Invalid offset {offset}");
                return null;
            }

            // Ensure that the cluster index is valid
            if (cluster < 0 || cluster > this.volume.MaxClusters)
            {
                Console.WriteLine($"Failed to load metadata object from database: Invalid cluster {cluster}");
                return null;
            }

            // Convert the offset to an offset relative to the File Area
            offset -= (this.volume.Offset + this.volume.FileAreaByteOffset);

            // Read the DirectoryEntry data
            byte[] data = new byte[0x40];
            this.volume.FileAreaStream.Seek(offset, SeekOrigin.Begin);
            this.volume.FileAreaStream.Read(data, 0, 0x40);

            // Create a DirectoryEntry
            var directoryEntry = new DirectoryEntry(this.volume.Platform, data, 0);
            directoryEntry.Cluster = cluster;
            directoryEntry.Offset = offset;

            // Add this file to the FileDatabase
            var databaseFile = fileDatabase.AddFile(directoryEntry, true);

            /*
             * Here we begin assigning our user-configurable modifications
             */

            if (directoryEntryObject.TryGetProperty("Children", out var childrenElement))
            {
                foreach (var childElement in childrenElement.EnumerateArray())
                {
                    LoadDirectoryEntryFromDatabase(childElement);
                }
            }

            if (directoryEntryObject.TryGetProperty("Clusters", out var clustersElement))
            {
                List<uint> clusterChain = new List<uint>();

                //if (databaseFile.FileName == "ears_godfather")
                //    System.Diagnostics.Debugger.Break();

                foreach (var clusterIndex in clustersElement.EnumerateArray())
                {
                    clusterChain.Add(clusterIndex.GetUInt32());
                }

                databaseFile.ClusterChain = clusterChain;
            }

            return databaseFile;
        }

        private bool LoadFromDatabase(JsonElement metadataAnalysisObject)
        {
            if (metadataAnalysisObject.GetArrayLength() == 0)
                return false;

            // Load each root file and its children from the json database
            foreach (var directoryEntryObject in metadataAnalysisObject.EnumerateArray())
            {
                LoadDirectoryEntryFromDatabase(directoryEntryObject);
            }

            return true;
        }

        public void LoadFileCarverResults(JsonElement fileCarverList)
        {
            carvedFiles = new List<CarvedFile>();

            foreach (var file in fileCarverList.EnumerateArray())
            {
                JsonElement offsetElement;
                if (!file.TryGetProperty("Offset", out offsetElement))
                {
                    Console.WriteLine("Failed to load signature from database: Missing offset field");
                    continue;
                }

                var carvedFile = new CarvedFile(offsetElement.GetInt64(), "");

                if (file.TryGetProperty("Name", out var nameElement))
                {
                    carvedFile.FileName = nameElement.GetString();
                }

                if (file.TryGetProperty("Size", out var sizeElement))
                {
                    carvedFile.FileSize = sizeElement.GetInt64();
                }

                carvedFiles.Add(carvedFile);
            }
        }

        public void LoadFromJson(JsonElement partitionElement)
        {
            // We are loading a new database so clear previous results
            fileDatabase.Reset();

            // Find the Analysis element, which contains analysis results
            JsonElement analysisElement;
            if (!partitionElement.TryGetProperty("Analysis", out analysisElement))
            {
                var name = partitionElement.GetProperty("Name").GetString();
                throw new FileLoadException($"Database: Partition ${name} is missing Analysis object!");
            }

            if (analysisElement.TryGetProperty("MetadataAnalyzer", out var metadataAnalysisList))
            {
                // Loads the files from the json into the FileDatabase
                // Only post the results if there actually was any
                if (LoadFromDatabase(metadataAnalysisList))
                {
                    OnLoadRecoveryFromDatabase?.Invoke(null, null);

                    // Mark that analysis was done
                    this.metadataAnalyzer = true;
                }
                else
                {
                    // Element was there but no analysis results were loaded
                    this.metadataAnalyzer = false;
                }
            }

            if (analysisElement.TryGetProperty("FileCarver", out var fileCarverList))
            {
                // TODO: We will begin replacing this when we start work on customizable "CarvedFiles"
                //var analyzer = new FileCarver(this.volume, FileCarverInterval.Cluster);

                LoadFileCarverResults(fileCarverList);

                if (carvedFiles.Count > 0)
                {
                    view.CreateCarverView(carvedFiles);
                }
            }
        }
    }
}
