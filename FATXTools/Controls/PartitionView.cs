using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows.Forms;
using FATX;
using FATX.Analyzers;
using FATXTools.Controls;
using FATXTools.Utilities;

namespace FATXTools
{
    public partial class PartitionView : UserControl
    {
        private TabPage explorerPage;
        private TabPage clusterViewerPage;
        private TabPage carverResultsPage;
        private TabPage recoveryResultsPage;

        private IntegrityAnalyzer integrityAnalyzer;
        private ClusterViewer clusterViewer;
        private TaskRunner taskRunner;

        private Volume volume;
        private MetadataAnalyzer metadataAnalyzer;
        private FileCarver fileCarver;

        public PartitionView(TaskRunner taskRunner, Volume volume)
        {
            InitializeComponent();

            integrityAnalyzer = new IntegrityAnalyzer(volume);
            this.taskRunner = taskRunner;

            this.volume = volume;

            explorerPage = new TabPage("File Explorer");
            FileExplorer explorer = new FileExplorer(this, taskRunner, volume);
            explorer.Dock = DockStyle.Fill;
            explorer.OnMetadataAnalyzerCompleted += Explorer_OnMetadataAnalyzerCompleted;
            explorer.OnFileCarverCompleted += Explorer_OnFileCarverCompleted;
            explorerPage.Controls.Add(explorer);
            this.tabControl1.TabPages.Add(explorerPage);

            clusterViewerPage = new TabPage("Cluster Viewer");
            clusterViewer = new ClusterViewer(volume, integrityAnalyzer);
            clusterViewer.Dock = DockStyle.Fill;
            clusterViewerPage.Controls.Add(clusterViewer);
            this.tabControl1.TabPages.Add(clusterViewerPage);
            //splitContainer2.Panel1.Controls.Add(clusterViewer);
        }

        public string PartitionName => volume.Name;

        public static PartitionView FromJson(JsonElement partitionObject, TaskRunner taskRunner, Volume volume)
        {
            var partitionView = new PartitionView(taskRunner, volume);

            partitionView.LoadFromJson(partitionObject);

            return partitionView;
        }

        // TODO: Might want to add this to some utility class since its used often
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

        public void Save(Dictionary<string, object> partitionObject)
        {
            partitionObject["Name"] = this.volume.Name;
            partitionObject["Offset"] = this.volume.Offset;
            partitionObject["Length"] = this.volume.Length;

            partitionObject["Analysis"] = new Dictionary<string, object>();
            var analysisObject = partitionObject["Analysis"] as Dictionary<string, object>;

            analysisObject["MetadataAnalyzer"] = new List<Dictionary<string, object>>();
            if (recoveryResultsPage != null)
            {
                var metadataAnalysisList = analysisObject["MetadataAnalyzer"] as List<Dictionary<string, object>>;
                SaveMetadataAnalysis(metadataAnalysisList);
            }

            analysisObject["FileCarver"] = new List<Dictionary<string, object>>();
            if (carverResultsPage != null)
            {
                var fileCarverObject = analysisObject["FileCarver"] as List<Dictionary<string, object>>;
                SaveFileCarver(fileCarverObject);
            }
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

                AddMetadataAnalyzerPage(analyzer);

                this.metadataAnalyzer = analyzer;
            }

            if (analysisElement.TryGetProperty("FileCarver", out var fileCarverList))
            {
                var analyzer = new FileCarver(this.volume, FileCarverInterval.Cluster, this.volume.Length);

                analyzer.LoadFromDatabase(fileCarverList);

                if (analyzer.GetCarvedFiles() != null)
                {
                    AddFileCarverPage(analyzer);
                    this.fileCarver = analyzer;
                }
            }
        }

        private void AddFileCarverPage(FileCarver carver)
        {
            if (carverResultsPage != null)
            {
                tabControl1.TabPages.Remove(carverResultsPage);
            }

            carverResultsPage = new TabPage("File Carver Results");
            CarverResults carverResults = new CarverResults(carver);
            carverResults.Dock = DockStyle.Fill;
            carverResultsPage.Controls.Add(carverResults);
            tabControl1.TabPages.Add(carverResultsPage);
            tabControl1.SelectedTab = carverResultsPage;
        }

        private void AddMetadataAnalyzerPage(MetadataAnalyzer analyzer)
        {
            if (recoveryResultsPage != null)
            {
                tabControl1.TabPages.Remove(recoveryResultsPage);
            }

            integrityAnalyzer.MergeMetadataAnalysis(analyzer.GetDirents());
            recoveryResultsPage = new TabPage("Metadata Analyzer Results");
            RecoveryResults recoveryResults = new RecoveryResults(analyzer, integrityAnalyzer, taskRunner);
            recoveryResults.Dock = DockStyle.Fill;
            recoveryResultsPage.Controls.Add(recoveryResults);
            tabControl1.TabPages.Add(recoveryResultsPage);
            tabControl1.SelectedTab = recoveryResultsPage;

            clusterViewer.UpdateClusters();
        }

        private void Explorer_OnFileCarverCompleted(object sender, EventArgs e)
        {
            FileCarverResults results = (FileCarverResults)e;
            fileCarver = results.carver;
            AddFileCarverPage(fileCarver);
        }

        private void Explorer_OnMetadataAnalyzerCompleted(object sender, EventArgs e)
        {
            MetadataAnalyzerResults results = (MetadataAnalyzerResults)e;
            metadataAnalyzer = results.analyzer;
            AddMetadataAnalyzerPage(metadataAnalyzer);
        }
    }
}
