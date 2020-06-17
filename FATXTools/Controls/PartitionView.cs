using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows.Forms;
using FATX;
using FATX.Analyzers;
using FATXTools.Controls;
using FATXTools.Database;
using FATXTools.Utilities;

namespace FATXTools
{
    public partial class PartitionView : UserControl
    {
        private TabPage explorerPage;
        private TabPage clusterViewerPage;
        private TabPage carverResultsPage;
        private TabPage recoveryResultsPage;

        private PartitionDatabase partitionDatabase;
        private IntegrityAnalyzer integrityAnalyzer;
        private ClusterViewer clusterViewer;
        private TaskRunner taskRunner;

        private Volume volume;
        private MetadataAnalyzer metadataAnalyzer;
        private FileCarver fileCarver;

        public PartitionView(TaskRunner taskRunner, Volume volume, PartitionDatabase partitionDatabase)
        {
            InitializeComponent();

            integrityAnalyzer = new IntegrityAnalyzer(volume, partitionDatabase.GetFileDatabase());

            this.taskRunner = taskRunner;
            this.volume = volume;
            this.partitionDatabase = partitionDatabase;

            // TODO: Use events instead of passing view to database
            partitionDatabase.SetPartitionView(this);

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
        }

        public string PartitionName => volume.Name;

        public void AddFileCarverPage(FileCarver carver)
        {
            if (carverResultsPage != null)
            {
                tabControl1.TabPages.Remove(carverResultsPage);
            }

            partitionDatabase.SetFileCarver(carver);
            carverResultsPage = new TabPage("File Carver Results");
            CarverResults carverResults = new CarverResults(carver);
            carverResults.Dock = DockStyle.Fill;
            carverResultsPage.Controls.Add(carverResults);
            tabControl1.TabPages.Add(carverResultsPage);
            tabControl1.SelectedTab = carverResultsPage;
        }

        public void AddMetadataAnalyzerPage()
        {
            if (recoveryResultsPage != null)
            {
                tabControl1.TabPages.Remove(recoveryResultsPage);
            }

            // TODO: We need to do a few things after we load a database or if analysis is completed
            // 1. Reset or update the IntegrityAnalyzer with the new FileDatabase
            // 2. Notify the ClusterViewer that we have started/loaded a new database
            integrityAnalyzer = new IntegrityAnalyzer(this.volume, partitionDatabase.GetFileDatabase());
            integrityAnalyzer.Update();
            recoveryResultsPage = new TabPage("Metadata Analyzer Results");
            RecoveryResults recoveryResults = new RecoveryResults(partitionDatabase.GetFileDatabase(), integrityAnalyzer, taskRunner);
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
            partitionDatabase.SetMetadataAnalyzer(true);
            partitionDatabase.GetFileDatabase().MergeMetadataAnalysis(metadataAnalyzer);
            AddMetadataAnalyzerPage();
        }
    }
}
