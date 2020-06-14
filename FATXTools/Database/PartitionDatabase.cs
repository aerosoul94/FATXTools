using FATX;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace FATXTools.Database
{
    public class PartitionDatabase
    {
        // We just want this for the volume info (offset, length, name)
        Volume volume;
        // TODO: get rid of these
        // We should be able to get this information from the FileDatabase
        MetadataAnalyzer metadataAnalyzer;
        FileCarver fileCarver;

        FileDatabase fileDatabase;

        PartitionView view;

        public PartitionDatabase(Volume volume)
        {
            this.volume = volume;
            this.metadataAnalyzer = null;
            this.fileCarver = null;

            this.fileDatabase = new FileDatabase(volume);
        }

        public string PartitionName => volume.Name;

        public void SetPartitionView(PartitionView view)
        {
            this.view = view;
        }

        public void SetMetadataAnalyzer(MetadataAnalyzer metadataAnalyzer)
        {
            this.metadataAnalyzer = metadataAnalyzer;
        }

        public void SetFileCarver(FileCarver fileCarver)
        {
            this.fileCarver = fileCarver;
        }

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
            if (metadataAnalyzer != null)
            {
                var metadataAnalysisList = analysisObject["MetadataAnalyzer"] as List<Dictionary<string, object>>;
                SaveMetadataAnalysis(metadataAnalysisList);
            }

            analysisObject["FileCarver"] = new List<Dictionary<string, object>>();
            if (fileCarver != null)
            {
                var fileCarverObject = analysisObject["FileCarver"] as List<Dictionary<string, object>>;
                SaveFileCarver(fileCarverObject);
            }
        }

        private void SaveMetadataAnalysis(List<Dictionary<string, object>> metadataAnalysisList)
        {
            foreach (var directoryEntry in metadataAnalyzer.GetRootDirectory())
            {
                var directoryEntryObject = new Dictionary<string, object>();
                metadataAnalysisList.Add(directoryEntryObject);
                SaveDirectoryEntry(directoryEntryObject, directoryEntry);
            }
        }

        private void SaveFileCarver(List<Dictionary<string, object>> fileCarverList)
        {
            foreach (var file in fileCarver.GetCarvedFiles())
            {
                var fileCarverObject = new Dictionary<string, object>();

                fileCarverObject["Offset"] = file.Offset;
                fileCarverObject["Name"] = file.FileName;
                fileCarverObject["Size"] = file.FileSize;

                fileCarverList.Add(fileCarverObject);
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

        private void SaveDirectoryEntry(Dictionary<string, object> directoryEntryObject, DirectoryEntry directoryEntry)
        {
            directoryEntryObject["Cluster"] = directoryEntry.Cluster;
            directoryEntryObject["Offset"] = directoryEntry.Offset;

            // At this moment, I believe this will only be used for debugging.
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

            // TODO: I don't know why I hadn't thought of this before.
            // We need to make sure we're using the FAT for active files!!!!!
            directoryEntryObject["Clusters"] = GenerateArtificialClusterChain(directoryEntry);
        }

        public void LoadFromJson(JsonElement partitionElement)
        {
            var metadataAnalyzer = new MetadataAnalyzer(this.volume, this.volume.BytesPerCluster, this.volume.Length);

            JsonElement analysisElement;
            if (!partitionElement.TryGetProperty("Analysis", out analysisElement))
            {
                var name = partitionElement.GetProperty("Name").GetString();
                throw new FileLoadException($"Database: Partition ${name} is missing Analysis object!");
            }

            //var analysisObject = partitionObject["Analysis"] as Dictionary<string, object>;

            if (analysisElement.TryGetProperty("MetadataAnalyzer", out var metadataAnalysisList))
            {
                var analyzer = new MetadataAnalyzer(this.volume, this.volume.BytesPerCluster, this.volume.Length);

                analyzer.LoadFromDatabase(metadataAnalysisList);

                if (analyzer.GetDirents().Count > 0)
                {
                    view.AddMetadataAnalyzerPage(analyzer);

                    this.metadataAnalyzer = analyzer;
                }
            }

            if (analysisElement.TryGetProperty("FileCarver", out var fileCarverList))
            {
                var analyzer = new FileCarver(this.volume, FileCarverInterval.Cluster, this.volume.Length);

                analyzer.LoadFromDatabase(fileCarverList);

                if (analyzer.GetCarvedFiles().Count > 0)
                {
                    view.AddFileCarverPage(analyzer);
                    this.fileCarver = analyzer;
                }
            }
        }
    }
}
