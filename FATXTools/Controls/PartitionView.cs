using FATX.Analyzers;
using FATX.FileSystem;
using FATXTools.Controls;
using FATXTools.Database;
using FATXTools.Utilities;
using System;
using System.Windows.Forms;

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
            partitionDatabase.OnLoadRecoveryFromDatabase += PartitionDatabase_OnLoadNewDatabase;

            explorerPage = new TabPage("File Explorer");
            FileExplorer explorer = new FileExplorer(taskRunner, volume);
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

        private void PartitionDatabase_OnLoadNewDatabase(object sender, EventArgs e)
        {
            // At this point the files will be loaded into the file database.
            var fileDatabase = partitionDatabase.GetFileDatabase();

            Console.WriteLine($"Loaded {fileDatabase.Count()} files for {PartitionName}.");

            fileDatabase.Update();          // Update the file system
            integrityAnalyzer.Update();     // Update the integrity analyzer
            clusterViewer.UpdateClusters(); // Update the cluster viewer

            CreateRecoveryView();
        }

        public string PartitionName => volume.Name;
        public Volume Volume => volume;

        public void CreateCarverView(FileCarver carver)
        {
            if (carverResultsPage != null)
            {
                tabControl1.TabPages.Remove(carverResultsPage);
            }

            partitionDatabase.SetFileCarver(carver);
            carverResultsPage = new TabPage("Carver View");
            CarverResults carverResults = new CarverResults(carver, this.taskRunner);
            carverResults.Dock = DockStyle.Fill;
            carverResultsPage.Controls.Add(carverResults);
            tabControl1.TabPages.Add(carverResultsPage);
            tabControl1.SelectedTab = carverResultsPage;
        }

        public void CreateRecoveryView()
        {
            if (recoveryResultsPage != null)
            {
                tabControl1.TabPages.Remove(recoveryResultsPage);
            }

            recoveryResultsPage = new TabPage("Recovery View");
            RecoveryResults recoveryResults = new RecoveryResults(partitionDatabase.GetFileDatabase(), integrityAnalyzer, taskRunner);
            recoveryResults.Dock = DockStyle.Fill;
            recoveryResults.NotifyDatabaseChanged += RecoveryResults_NotifyDatabaseChanged;
            recoveryResultsPage.Controls.Add(recoveryResults);
            tabControl1.TabPages.Add(recoveryResultsPage);
            tabControl1.SelectedTab = recoveryResultsPage;
        }

        private void RecoveryResults_NotifyDatabaseChanged(object sender, EventArgs e)
        {
            RefreshViews();
        }

        private void RefreshViews()
        {
            var fileDatabase = partitionDatabase.GetFileDatabase();

            fileDatabase.Update();          // Update the file system
            integrityAnalyzer.Update();     // Update the integrity analyzer
            clusterViewer.UpdateClusters(); // Update the cluster viewer
        }

        private void Explorer_OnFileCarverCompleted(object sender, EventArgs e)
        {
            FileCarverResults results = (FileCarverResults)e;
            fileCarver = results.carver;
            CreateCarverView(fileCarver);
        }

        private void Explorer_OnMetadataAnalyzerCompleted(object sender, EventArgs e)
        {
            MetadataAnalyzerResults results = (MetadataAnalyzerResults)e;
            metadataAnalyzer = results.analyzer;
            partitionDatabase.SetMetadataAnalyzer(true);

            var fileDatabase = partitionDatabase.GetFileDatabase();

            // We've got new analysis results, we need to clear any previous work
            fileDatabase.Reset();

            // Add in the new results
            foreach (var dirent in metadataAnalyzer.Results)
            {
                fileDatabase.AddFile(dirent, true);
            }

            fileDatabase.Update();          // Update the file system
            integrityAnalyzer.Update();     // Update the integrity analyzer
            clusterViewer.UpdateClusters(); // Update the cluster viewer

            CreateRecoveryView();
        }
    }
}
