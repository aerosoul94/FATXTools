using System;
using System.Collections.Generic;
using System.Windows.Forms;

using FATX.Analyzers;
using FATX.FileSystem;

using FATXTools.Controls;
using FATXTools.Database;

namespace FATXTools
{
    public partial class PartitionView : UserControl
    {
        private TabPage _explorerPage;
        private TabPage _clusterViewerPage;
        private TabPage _carverResultsPage;
        private TabPage _recoveryResultsPage;

        private PartitionDatabase _partitionDatabase;
        private IntegrityAnalyzer _integrityAnalyzer;
        private ClusterViewer _clusterViewer;

        private Volume _volume;

        public PartitionView(Volume volume, PartitionDatabase partitionDatabase)
        {
            InitializeComponent();

            _integrityAnalyzer = new IntegrityAnalyzer(volume, partitionDatabase.GetFileDatabase());

            _volume = volume;
            _partitionDatabase = partitionDatabase;

            // TODO: Use events instead of passing view to database
            partitionDatabase.SetPartitionView(this);
            partitionDatabase.OnLoadRecoveryFromDatabase += PartitionDatabase_OnLoadNewDatabase;

            _explorerPage = new TabPage("File Explorer");
            FileExplorer explorer = new FileExplorer(volume)
            {
                Dock = DockStyle.Fill
            };
            explorer.OnMetadataAnalyzerCompleted += Explorer_OnMetadataAnalyzerCompleted;
            explorer.OnFileCarverCompleted += Explorer_OnFileCarverCompleted;
            _explorerPage.Controls.Add(explorer);
            tabControl1.TabPages.Add(_explorerPage);

            _clusterViewerPage = new TabPage("Cluster Viewer");
            _clusterViewer = new ClusterViewer(volume, _integrityAnalyzer)
            {
                Dock = DockStyle.Fill
            };
            _clusterViewerPage.Controls.Add(_clusterViewer);
            tabControl1.TabPages.Add(_clusterViewerPage);
        }

        private void PartitionDatabase_OnLoadNewDatabase(object sender, EventArgs e)
        {
            // At this point the files will be loaded into the file database.
            var fileDatabase = _partitionDatabase.GetFileDatabase();

            Console.WriteLine($"Loaded {fileDatabase.Count()} files for {PartitionName}.");

            fileDatabase.Update();          // Update the file system
            _integrityAnalyzer.Update();     // Update the integrity analyzer
            _clusterViewer.UpdateClusters(); // Update the cluster viewer

            CreateRecoveryView();
        }

        public string PartitionName => _volume.Name;
        public Volume Volume => _volume;

        public void CreateCarverView(List<CarvedFile> files)
        {
            if (_carverResultsPage != null)
                tabControl1.TabPages.Remove(_carverResultsPage);

            //partitionDatabase.SetFileCarver(carver);
            _carverResultsPage = new TabPage("Carver View");
            CarverResults carverResults = new CarverResults(_volume, files)
            {
                Dock = DockStyle.Fill
            };
            _carverResultsPage.Controls.Add(carverResults);
            tabControl1.TabPages.Add(_carverResultsPage);
            tabControl1.SelectedTab = _carverResultsPage;
        }

        public void CreateRecoveryView()
        {
            if (_recoveryResultsPage != null)
                tabControl1.TabPages.Remove(_recoveryResultsPage);

            _recoveryResultsPage = new TabPage("Recovery View");
            RecoveryResults recoveryResults = new RecoveryResults(_partitionDatabase.GetFileDatabase(), _integrityAnalyzer)
            {
                Dock = DockStyle.Fill
            };
            recoveryResults.NotifyDatabaseChanged += RecoveryResults_NotifyDatabaseChanged;
            _recoveryResultsPage.Controls.Add(recoveryResults);
            tabControl1.TabPages.Add(_recoveryResultsPage);
            tabControl1.SelectedTab = _recoveryResultsPage;
        }

        private void RecoveryResults_NotifyDatabaseChanged(object sender, EventArgs e)
        {
            RefreshViews();
        }

        private void RefreshViews()
        {
            var fileDatabase = _partitionDatabase.GetFileDatabase();

            fileDatabase.Update();          // Update the file system
            _integrityAnalyzer.Update();     // Update the integrity analyzer
            _clusterViewer.UpdateClusters(); // Update the cluster viewer
        }

        private void Explorer_OnFileCarverCompleted(object sender, EventArgs e)
        {
            FileCarverResults results = (FileCarverResults)e;
            CreateCarverView(results.Results);
        }

        private void Explorer_OnMetadataAnalyzerCompleted(object sender, EventArgs e)
        {
            MetadataAnalyzerResults results = (MetadataAnalyzerResults)e;
            _partitionDatabase.SetMetadataAnalyzer(true);

            var fileDatabase = _partitionDatabase.GetFileDatabase();

            // We've got new analysis results, we need to clear any previous work
            fileDatabase.Reset();

            // Add in the new results
            foreach (var dirent in results.Results)
                fileDatabase.AddFile(dirent, true);

            fileDatabase.Update();          // Update the file system
            _integrityAnalyzer.Update();     // Update the integrity analyzer
            _clusterViewer.UpdateClusters(); // Update the cluster viewer

            CreateRecoveryView();
        }
    }
}
