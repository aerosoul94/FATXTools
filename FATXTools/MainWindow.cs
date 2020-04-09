using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Security.Principal;
using System.IO;
using FATXTools.DiskTypes;
using FATX;
using Microsoft.Win32.SafeHandles;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;

namespace FATXTools
{
    public partial class MainWindow : Form
    {
        AnalyzerProgress taskProgress;

        private Controls.ClusterViewer clusterViewer;

        public MainWindow()
        {
            InitializeComponent();
            Console.SetOut(new LogWriter(this.textBox1));
            Console.WriteLine("FATX-Tools v0.1");
            Console.WriteLine();
        }

        enum NodeType
        {
            Drive,
            Partition,
            Dirent
        }

        struct NodeTag
        {
            public object Tag;
            public NodeType Type;

            public NodeTag(object tag, NodeType type)
            {
                this.Tag = tag;
                this.Type = type;
            }
        }

        public class LogWriter : TextWriter
        {
            private TextBox textBox;
            private delegate void SafeCallDelegate(string text);
            public LogWriter(TextBox textBox)
            {
                this.textBox = textBox;
            }

            public override void Write(char value)
            {
                textBox.Text += value;
            }

            public override void Write(string value)
            {
                textBox.AppendText(value);
            }

            public override void WriteLine()
            {
                textBox.AppendText(NewLine);
            }

            public override void WriteLine(string value)
            {
                if (textBox.InvokeRequired)
                {
                    var d = new SafeCallDelegate(WriteLine);
                    textBox.BeginInvoke(d, new object[] { value });
                }
                else
                {
                    textBox.AppendText(value + NewLine);
                }
            }

            public override Encoding Encoding
            {
                get { return Encoding.ASCII; }
            }
        }

        private void openImageToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                string fileName = ofd.FileName;

                Console.WriteLine("Mounting drive: {0} ({1})", Path.GetFileName(fileName), fileName);
                RawImage rawImage = new RawImage(fileName);

                var driveNode = treeView1.Nodes.Add(Path.GetFileName(fileName));
                NodeTag driveTag = new NodeTag(null, NodeType.Drive);
                driveNode.Tag = driveTag;
                driveNode.ImageIndex = 2;
                driveNode.SelectedImageIndex = 2;

                foreach (Volume volume in rawImage.GetPartitions())
                {
                    var volumeNode = driveNode.Nodes.Add(volume.Name);
                    NodeTag nodeTag = new NodeTag(volume, NodeType.Partition);
                    volumeNode.Tag = nodeTag;

                    try
                    {
                        volume.Mount();

                        Console.WriteLine("Successfully mounted volume: {0}", volume.Name);
                        PopulateTreeNodeDirectory(volumeNode, volume.GetRoot());
                    }
                    catch (Exception exception)
                    {
                        Console.WriteLine(String.Format("Failed to mount partition {0}", volume.Name));
                        Console.WriteLine(exception.Message);
                    }
                }

                Console.WriteLine();
            }
        }

        private void PopulateTreeNodeDirectory(TreeNode node, List<DirectoryEntry> dirents)
        {
            foreach (DirectoryEntry dirent in dirents)
            {
                if (dirent.IsDirectory())
                {
                    var childNode = node.Nodes.Add(dirent.FileName);
                    NodeTag nodeTag = new NodeTag(dirent, NodeType.Dirent);
                    childNode.Tag = nodeTag;
                    childNode.ImageIndex = 0;
                    childNode.SelectedImageIndex = 0;
                    PopulateTreeNodeDirectory(childNode, dirent.GetChildren());
                }
            }
        }

        private static string FormatBytes(long bytes)
        {
            string[] Suffix = { "B", "KB", "MB", "GB", "TB" };
            int i;
            double dblSByte = bytes;
            for (i = 0; i < Suffix.Length && bytes >= 1024; i++, bytes /= 1024)
            {
                dblSByte = bytes / 1024.0;
            }

            return String.Format("{0:0.##} {1}", dblSByte, Suffix[i]);
        }

        private void PopulateListView(List<DirectoryEntry> dirents, DirectoryEntry parent)
        {
            listView1.Items.Clear();

            var upDir = listView1.Items.Add("");
            upDir.SubItems.Add("...");
            if (parent != null)
            {
                if (parent.GetParent() != null)
                {
                    upDir.Tag = parent.GetParent();
                }
            }

            int index = 1;
            foreach (DirectoryEntry dirent in dirents)
            {
                ListViewItem item = listView1.Items.Add(index.ToString());
                item.Tag = dirent;

                item.SubItems.Add(dirent.FileName);

                DateTime creationTime = dirent.CreationTime.AsDateTime();
                DateTime lastWriteTime = dirent.LastWriteTime.AsDateTime();
                DateTime lastAccessTime = dirent.LastAccessTime.AsDateTime();

                string sizeStr = "";
                if (!dirent.IsDirectory())
                {
                    item.ImageIndex = 1;
                    sizeStr = FormatBytes(dirent.FileSize);
                }
                else
                {
                    item.ImageIndex = 0;
                }

                item.SubItems.Add(sizeStr);
                item.SubItems.Add(creationTime.ToString());
                item.SubItems.Add(lastWriteTime.ToString());
                item.SubItems.Add(lastAccessTime.ToString());
                item.SubItems.Add("0x" + dirent.Offset.ToString("x"));
                item.SubItems.Add(dirent.GetCluster().ToString());

                index++;
            }
        }

        private void treeView1_AfterSelect(object sender, TreeViewEventArgs e)
        {
            NodeTag tag = (NodeTag)e.Node.Tag;
            List<DirectoryEntry> dirents;
            Volume volume;

            switch (tag.Type)
            {
                case NodeType.Dirent:
                    DirectoryEntry dirent = (DirectoryEntry)tag.Tag;
                    volume = dirent.GetVolume();
                    dirents = dirent.GetChildren();
                    PopulateListView(dirents, dirent.GetParent());
                    break;
                case NodeType.Partition:
                    volume = (Volume)tag.Tag;
                    dirents = volume.GetRoot();
                    PopulateListView(dirents, null);
                    break;
                case NodeType.Drive:
                    return;
            }
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show(
                "Developed by aerosoul94\n" + 
                "Source code: https://github.com/aerosoul94/FATXTools\n" + 
                "Please report any bugs\n",
                "About", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void openDeviceToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var isAdmin = new WindowsPrincipal(WindowsIdentity.GetCurrent())
                .IsInRole(WindowsBuiltInRole.Administrator);

            if (!isAdmin)
            {
                MessageBox.Show("You must re-run this program with Administrator priveleges\n" + 
                                "in order to read from physical drives.", 
                                "Cannot perform operation", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            DeviceSelector ds = new DeviceSelector(this);
            if (ds.ShowDialog() == DialogResult.OK)
            {
                SafeFileHandle handle = DeviceSelector.CreateFile(ds.SelectedDevice,
                    FileAccess.Read,
                    FileShare.None,
                    IntPtr.Zero,
                    FileMode.Open,
                    0, IntPtr.Zero);
                long length = DeviceSelector.GetDiskCapactity(handle);
                PhysicalDisk disk = new PhysicalDisk(handle, length);
                List<Volume> volumes = disk.GetPartitions();

                var driveNode = treeView1.Nodes.Add(ds.SelectedDevice);
                driveNode.ImageIndex = 2;
                driveNode.SelectedImageIndex = 2;

                foreach (Volume volume in disk.GetPartitions())
                {
                    var volumeNode = driveNode.Nodes.Add(volume.Name);

                    try
                    {
                        volume.Mount();

                        PopulateTreeNodeDirectory(volumeNode, volume.GetRoot());
                    }
                    catch (Exception exception)
                    {
                        Console.WriteLine(String.Format("Failed to mount partition {0}", volume.Name));
                        Console.WriteLine(exception.Message);
                    }
                }
            }
        }

        private void toolStripMenuItem1_Click(object sender, EventArgs e)
        {
            if (taskProgress != null)
            {
                MessageBox.Show("Must wait for current task to finish!");
                return;
            }

            NodeTag nodeTag = (NodeTag)treeView1.SelectedNode.Tag;
            Volume volume = null;

            switch (nodeTag.Type)
            {
                case NodeType.Partition:
                    volume = (Volume)nodeTag.Tag;
                    break;
                case NodeType.Dirent:
                    DirectoryEntry dirent = (DirectoryEntry)nodeTag.Tag;
                    volume = dirent.GetVolume();
                    break;
                case NodeType.Drive:
                    return;
            }

            taskProgress = new AnalyzerProgress(this, volume.FileAreaLength, volume.BytesPerCluster);
            taskProgress.Show();
            backgroundWorker2.RunWorkerAsync(volume);
        }

        private void toolStripMenuItem2_Click(object sender, EventArgs e)
        {
            if (taskProgress != null)
            {
                MessageBox.Show("Must wait for current task to finish!");
                return;
            }

            NodeTag nodeTag = (NodeTag)treeView1.SelectedNode.Tag;
            Volume volume = null;

            switch (nodeTag.Type)
            {
                case NodeType.Partition:
                    volume = (Volume)nodeTag.Tag;
                    break;
                case NodeType.Dirent:
                    DirectoryEntry dirent = (DirectoryEntry)nodeTag.Tag;
                    volume = dirent.GetVolume();
                    break;
                case NodeType.Drive:
                    return;
            }

            var length = volume.FileAreaLength;
            taskProgress = new AnalyzerProgress(this, 0x100000, (long)FileCarverInterval.Sector);
            taskProgress.Show();
            backgroundWorker1.RunWorkerAsync(volume);
        }

        private void toolStripMenuItem6_Click(object sender, EventArgs e)
        {
            DirectoryEntry dirent = (DirectoryEntry)listView1.SelectedItems[0].Tag;
            FileInfo infoDlg = new FileInfo(dirent);
            infoDlg.ShowDialog();
        }

        private void toolStripMenuItem5_Click(object sender, EventArgs e)
        {
            using (var fbd = new FolderBrowserDialog())
            {
                if (fbd.ShowDialog() == DialogResult.OK)
                {
                    foreach (ListViewItem item in listView1.SelectedItems)
                    {
                        DirectoryEntry dirent = (DirectoryEntry)item.Tag;
                        dirent.GetVolume().DumpDirent(fbd.SelectedPath, dirent);
                    }
                }
            }
        }

        private void listView1_DoubleClick(object sender, EventArgs e)
        {
            if (listView1.SelectedItems.Count > 1)
            {
                return;
            }

            DirectoryEntry dirent = (DirectoryEntry)listView1.SelectedItems[0].Tag;
            if (dirent == null)
            {
                return;
            }

            if (dirent.IsDirectory())
            {
                PopulateListView(dirent.GetChildren(), dirent);
            }
        }

        private void toolStripMenuItem3_Click(object sender, EventArgs e)
        {
            NodeTag nodeTag = (NodeTag)treeView1.SelectedNode.Tag;
            Volume volume = null;
            switch (nodeTag.Type)
            {
                case NodeType.Dirent:
                    DirectoryEntry dirent = (DirectoryEntry)nodeTag.Tag;
                    volume = dirent.GetVolume();
                    break;
                case NodeType.Partition:
                    volume = (Volume)nodeTag.Tag;
                    break;
                case NodeType.Drive:
                    MessageBox.Show("Please select a partition to dump!");
                    return;
            }

            if (volume != null)
            {
                using (var fbd = new FolderBrowserDialog())
                {
                    if (fbd.ShowDialog() == DialogResult.OK)
                    {
                        foreach (DirectoryEntry dirent in volume.GetRoot())
                        {
                            dirent.GetVolume().DumpDirent(fbd.SelectedPath, dirent);
                        }
                    }
                }
            }
        }

        private void backgroundWorker1_DoWork(object sender, System.ComponentModel.DoWorkEventArgs e)
        {
            BackgroundWorker worker = sender as BackgroundWorker;
            Volume volume = (Volume)e.Argument;
            FileCarver analyzer = new FileCarver(volume, FileCarverInterval.Sector, 0x100000);
            analyzer.Analyze(worker);
            e.Result = analyzer;
        }

        private void backgroundWorker1_ProgressChanged(object sender, System.ComponentModel.ProgressChangedEventArgs e)
        {
            taskProgress.UpdateProgress(e.ProgressPercentage);
        }

        private void backgroundWorker1_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            taskProgress.Close();
            taskProgress = null;
            FileCarver analyzer = (FileCarver)e.Result;

            var page = new TabPage("File Carver Results");
            var resultsControl = new CarverResults(analyzer);
            resultsControl.Dock = DockStyle.Fill;
            page.Controls.Add(resultsControl);
            tabControl1.TabPages.Add(page);
            tabControl1.SelectedTab = page;
        }

        private void backgroundWorker2_DoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker worker = sender as BackgroundWorker;
            Volume volume = (Volume)e.Argument;
            MetadataAnalyzer analyzer = new MetadataAnalyzer(volume, volume.BytesPerCluster, volume.FileAreaLength);
            analyzer.Analyze(worker);
            e.Result = analyzer;
        }

        private void backgroundWorker2_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            taskProgress.UpdateProgress(e.ProgressPercentage);
        }

        private void backgroundWorker2_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            taskProgress.Close();
            taskProgress = null;
            MetadataAnalyzer analyzer = (MetadataAnalyzer)e.Result;

            var page = new TabPage("Results");
            var resultsControl = new RecoveryResults(analyzer);
            resultsControl.Dock = DockStyle.Fill;
            page.Controls.Add(resultsControl);
            tabControl1.TabPages.Add(page);
            tabControl1.SelectedTab = page;
            // TODO: Handle a situation where clusterViewer does not yet exist.
            // TODO: Will need to move data map cell info to a new class.
            //clusterViewer.UpdateClusters(analyzer.GetRoot());
        }

        private void toolStripMenuItem7_Click(object sender, EventArgs e)
        {
            NodeTag nodeTag = (NodeTag)treeView1.SelectedNode.Tag;
            Volume volume = null;
            switch (nodeTag.Type)
            {
                case NodeType.Dirent:
                    DirectoryEntry dirent = (DirectoryEntry)nodeTag.Tag;
                    volume = dirent.GetVolume();
                    break;
                case NodeType.Partition:
                    volume = (Volume)nodeTag.Tag;
                    break;
                case NodeType.Drive:
                    MessageBox.Show("Please select a partition to dump!");
                    return;
            }

            if (volume != null)
            {
                var page = new TabPage(volume.Name + " Cluster Map");
                clusterViewer = new Controls.ClusterViewer(volume);
                clusterViewer.Dock = DockStyle.Fill;
                page.Controls.Add(clusterViewer);
                tabControl1.TabPages.Add(page);
                tabControl1.SelectedTab = page;
            }
        }
    }
}
