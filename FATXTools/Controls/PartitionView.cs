using System;
using System.Windows.Forms;
using FATX;
using FATX.Analyzers;
using FATXTools.Controls;

namespace FATXTools
{
    public partial class PartitionView : UserControl
    {
        private TabPage explorerPage;
        private TabPage clusterViewerPage;
        private TabPage carverResultsPage;
        private TabPage recoveryResultsPage;

        private IntegrityAnalyzer integrityAnalyzer;

        public PartitionView(Volume volume)
        {
            InitializeComponent();

            integrityAnalyzer = new IntegrityAnalyzer(volume);

            explorerPage = new TabPage("File Explorer");
            FileExplorer explorer = new FileExplorer(this, volume);
            explorer.Dock = DockStyle.Fill;
            explorer.OnMetadataAnalyzerCompleted += Explorer_OnMetadataAnalyzerCompleted;
            explorer.OnFileCarverCompleted += Explorer_OnFileCarverCompleted;
            explorerPage.Controls.Add(explorer);
            this.tabControl1.TabPages.Add(explorerPage);

            clusterViewerPage = new TabPage("Cluster Viewer");
            ClusterViewer clusterViewer = new ClusterViewer(volume);
            clusterViewer.Dock = DockStyle.Fill;
            clusterViewerPage.Controls.Add(clusterViewer);
            this.tabControl1.TabPages.Add(clusterViewerPage);
            //splitContainer2.Panel1.Controls.Add(clusterViewer);
        }

        private void Explorer_OnFileCarverCompleted(object sender, EventArgs e)
        {
            if (carverResultsPage != null)
            {
                tabControl1.TabPages.Remove(carverResultsPage);
            }

            FileCarverResults results = (FileCarverResults)e;
            carverResultsPage = new TabPage("File Carver Results");
            CarverResults carverResults = new CarverResults(results.carver);
            carverResults.Dock = DockStyle.Fill;
            carverResultsPage.Controls.Add(carverResults);
            tabControl1.TabPages.Add(carverResultsPage);
            tabControl1.SelectedTab = carverResultsPage;
        }

        private void Explorer_OnMetadataAnalyzerCompleted(object sender, EventArgs e)
        {
            if (recoveryResultsPage != null)
            {
                tabControl1.TabPages.Remove(recoveryResultsPage);
            }

            MetadataAnalyzerResults results = (MetadataAnalyzerResults)e;
            integrityAnalyzer.AddFileSystem(results.analyzer.GetDirents());
            recoveryResultsPage = new TabPage("Metadata Analyzer Results");
            RecoveryResults recoveryResults = new RecoveryResults(results.analyzer, integrityAnalyzer);
            recoveryResults.Dock = DockStyle.Fill;
            recoveryResultsPage.Controls.Add(recoveryResults);
            tabControl1.TabPages.Add(recoveryResultsPage);
            tabControl1.SelectedTab = recoveryResultsPage;
        }

        public void DoMetadataAnalysis(Volume volume)
        {
            if (backgroundWorker1.IsBusy)
            {
                MessageBox.Show("Must wait for analyzer to finish!");
                return;
            }

            
            MetadataAnalyzer analyzer = new MetadataAnalyzer(volume, volume.BytesPerCluster, volume.Length);
        }
    }
}
