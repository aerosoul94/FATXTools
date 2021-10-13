using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Windows.Forms;

using FATX.Analyzers;
using FATX.FileSystem;

using FATXTools.Database;
using FATXTools.Dialogs;
using FATXTools.Tasks;
using FATXTools.Utilities;

namespace FATXTools
{
    public partial class RecoveryResults : UserControl
    {
        private Volume _volume;

        /// <summary>
        /// Mapping of cluster index to it's directory entries.
        /// </summary>
        private Dictionary<uint, List<DatabaseFile>> clusterNodes =
            new Dictionary<uint, List<DatabaseFile>>();

        /// <summary>
        /// Leads to the node of the current cluster.
        /// </summary>
        private TreeNode currentClusterNode;

        private ListViewItemComparer listViewItemComparer;

        private IntegrityAnalyzer _integrityAnalyzer;

        private FileDatabase _fileDatabase;

        private Color[] statusColor = new Color[]
        {
            Color.FromArgb(150, 250, 150), // Green
            Color.FromArgb(200, 250, 150), // Yellow-Green
            Color.FromArgb(250, 250, 150),
            Color.FromArgb(250, 200, 150),
            Color.FromArgb(250, 150, 150),
        };

        public event EventHandler NotifyDatabaseChanged;

        public RecoveryResults(FileDatabase database, IntegrityAnalyzer integrityAnalyzer)
        {
            InitializeComponent();

            this._fileDatabase = database;
            this._integrityAnalyzer = integrityAnalyzer;
            this._volume = database.GetVolume();

            listViewItemComparer = new ListViewItemComparer();
            listView1.ListViewItemSorter = listViewItemComparer;

            PopulateTreeView(database.GetRootFiles());
        }

        private enum NodeType
        {
            Cluster,
            Dirent
        }

        private struct NodeTag
        {
            public object Tag;
            public NodeType Type;

            public NodeTag(object tag, NodeType type)
            {
                this.Tag = tag;
                this.Type = type;
            }
        }

        private void PopulateFolder(List<DatabaseFile> children, TreeNode parent)
        {
            foreach (var child in children)
            {
                if (child.IsDirectory())
                {
                    var childNode = parent.Nodes.Add(child.FileName);
                    childNode.Tag = new NodeTag(child, NodeType.Dirent);
                    PopulateFolder(child.Children, childNode);
                }
            }
        }

        private void RefreshTreeView()
        {
            PopulateTreeView(_fileDatabase.GetRootFiles());
        }

        public void PopulateTreeView(List<DatabaseFile> results)
        {
            // Remove all nodes
            treeView1.BeginUpdate();
            treeView1.Nodes.Clear();
            clusterNodes.Clear();

            foreach (var result in results)
            {
                var cluster = result.Cluster;
                if (!clusterNodes.ContainsKey(cluster))
                {
                    List<DatabaseFile> list = new List<DatabaseFile>()
                    {
                        result
                    };

                    clusterNodes.Add(cluster, list);
                }
                else
                {
                    var list = clusterNodes[cluster];
                    list.Add(result);
                }

                var clusterNodeText = "Cluster " + result.Cluster;
                TreeNode clusterNode;
                if (!treeView1.Nodes.ContainsKey(clusterNodeText))
                {
                    clusterNode = treeView1.Nodes.Add(clusterNodeText, clusterNodeText);
                    clusterNode.Tag = new NodeTag(clusterNodes[cluster], NodeType.Cluster);
                }
                else
                {
                    clusterNode = treeView1.Nodes[clusterNodeText];
                }

                if (result.IsDirectory())
                {
                    var rootNode = clusterNode.Nodes.Add(result.FileName);
                    rootNode.Tag = new NodeTag(result, NodeType.Dirent);
                    PopulateFolder(result.Children, rootNode);
                }
            }

            treeView1.EndUpdate();
        }

        private void PopulateListView(List<DatabaseFile> dirents, DatabaseFile parent)
        {
            listView1.BeginUpdate();
            listView1.Items.Clear();

            var upDir = listView1.Items.Add("");

            upDir.SubItems.Add("...");
            if (parent != null)
            {
                upDir.Tag = new NodeTag(parent, NodeType.Dirent);
            }
            else
            {
                NodeTag nodeTag = (NodeTag)currentClusterNode.Tag;
                upDir.Tag = new NodeTag(nodeTag.Tag as List<DatabaseFile>, NodeType.Cluster);
            }

            List<ListViewItem> items = new List<ListViewItem>();
            int index = 1;
            foreach (DatabaseFile databaseFile in dirents)
            {
                ListViewItem item = new ListViewItem(index.ToString());
                item.Tag = new NodeTag(databaseFile, NodeType.Dirent);

                item.SubItems.Add(databaseFile.FileName);

                DateTime creationTime = databaseFile.CreationTime.AsDateTime();
                DateTime lastWriteTime = databaseFile.LastWriteTime.AsDateTime();
                DateTime lastAccessTime = databaseFile.LastAccessTime.AsDateTime();

                string sizeStr = "";
                if (!databaseFile.IsDirectory())
                {
                    item.ImageIndex = 1;
                    sizeStr = Utility.FormatBytes(databaseFile.FileSize);
                }
                else
                {
                    item.ImageIndex = 0;
                }

                item.SubItems.Add(sizeStr);
                item.SubItems.Add(creationTime.ToString());
                item.SubItems.Add(lastWriteTime.ToString());
                item.SubItems.Add(lastAccessTime.ToString());
                item.SubItems.Add("0x" + databaseFile.Offset.ToString("x"));
                item.SubItems.Add(databaseFile.Cluster.ToString());

                item.BackColor = statusColor[databaseFile.GetRanking()];

                index++;

                items.Add(item);
            }

            listView1.Items.AddRange(items.ToArray());
            listView1.EndUpdate();
        }

        private void treeView1_AfterSelect(object sender, TreeViewEventArgs e)
        {
            currentClusterNode = e.Node;
            while (currentClusterNode.Parent != null)
            {
                currentClusterNode = currentClusterNode.Parent;
            }

            //Console.WriteLine($"Current Cluster Node: {currentClusterNode.Text}");

            NodeTag nodeTag = (NodeTag)e.Node.Tag;
            switch (nodeTag.Type)
            {
                case NodeType.Cluster:
                    List<DatabaseFile> dirents = (List<DatabaseFile>)nodeTag.Tag;

                    PopulateListView(dirents, null);

                    break;
                case NodeType.Dirent:
                    DatabaseFile databaseFile = (DatabaseFile)nodeTag.Tag;

                    PopulateListView(databaseFile.Children, databaseFile.GetParent());

                    break;
            }
        }

        private void listView1_DoubleClick(object sender, EventArgs e)
        {
            if (listView1.SelectedItems.Count > 1)
                return;

            //Console.WriteLine($"Current Cluster Node: {currentClusterNode.Text}");

            NodeTag nodeTag = (NodeTag)listView1.SelectedItems[0].Tag;

            switch (nodeTag.Type)
            {
                case NodeType.Dirent:
                    DatabaseFile databaseFile = nodeTag.Tag as DatabaseFile;

                    if (databaseFile.IsDirectory())
                    {
                        PopulateListView(databaseFile.Children, databaseFile.GetParent());
                    }

                    break;
                case NodeType.Cluster:
                    List<DatabaseFile> dirents = nodeTag.Tag as List<DatabaseFile>;

                    PopulateListView(dirents, null);

                    break;
            }
        }

        private Action<CancellationToken, IProgress<(int, string)>> RunRecoverAllTask(string path, Dictionary<string, List<DatabaseFile>> clusters)
        {
            return (cancellationToken, progress) =>
            {
                new RecoveryTask(_volume, cancellationToken, progress)
                    .SaveClusters(path, clusters);
            };
        }

        private async void RunRecoverAllTaskAsync(string path, Dictionary<string, List<DatabaseFile>> clusters)
        {
            var options = new TaskDialogOptions() { Title = "Save File" };

            await TaskRunner.Instance.RunTaskAsync(ParentForm, options, RunRecoverAllTask(path, clusters));
        }

        private Action<CancellationToken, IProgress<(int, string)>> RunRecoverDirectoryEntryTask(string path, DatabaseFile file)
        {
            return (cancellationToken, progress) =>
            {
                new RecoveryTask(_volume, cancellationToken, progress)
                    .Save(path, file);
            };
        }

        private async void RunRecoverDirectoryEntryTaskAsync(string path, DatabaseFile databaseFile)
        {
            var options = new TaskDialogOptions() { Title = "Save File" };

            await TaskRunner.Instance.RunTaskAsync(ParentForm, options,
                RunRecoverDirectoryEntryTask(path, databaseFile));
        }

        private Action<CancellationToken, IProgress<(int, string)>> RunRecoverCluster(string path, List<DatabaseFile> files)
        {
            return (cancellationToken, progress) =>
            {
                try
                {
                    new RecoveryTask(_volume, cancellationToken, progress)
                        .SaveAll(path, files);
                }
                catch (OperationCanceledException)
                {
                    Console.WriteLine("Save all cancelled.");
                }
            };
        }

        private async void RunRecoverClusterTaskAsync(string path, List<DatabaseFile> files)
        {
            var options = new TaskDialogOptions() { Title = "Save All" };

            await TaskRunner.Instance.RunTaskAsync(ParentForm, options, RunRecoverCluster(path, files));
        }

        private void listRecoverSelectedToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (listView1.SelectedItems.Count == 0)
                return;

            var selectedItems = listView1.SelectedItems;
            using (var dialog = new FolderBrowserDialog())
            {
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    List<DatabaseFile> selectedFiles = new List<DatabaseFile>();

                    foreach (ListViewItem selectedItem in selectedItems)
                    {
                        NodeTag nodeTag = (NodeTag)selectedItem.Tag;

                        switch (nodeTag.Type)
                        {
                            case NodeType.Dirent:
                                DatabaseFile databaseFile = nodeTag.Tag as DatabaseFile;

                                selectedFiles.Add(databaseFile);

                                break;
                        }
                    }

                    RunRecoverClusterTaskAsync(dialog.SelectedPath, selectedFiles);
                }
            }
        }

        private void listRecoverCurrentDirectoryToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (var dialog = new FolderBrowserDialog())
            {
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    // TODO: Create `DirectoryEntry currentDirectory` for this class
                    //  so that we don't go through the list items.
                    //  Also, should we be dumping to a cluster directory?
                    List<DatabaseFile> selectedFiles = new List<DatabaseFile>();

                    foreach (ListViewItem item in listView1.Items)
                    {
                        if (item.Index == 0)
                        {
                            continue;
                        }

                        NodeTag nodeTag = (NodeTag)item.Tag;

                        switch (nodeTag.Type)
                        {
                            case NodeType.Dirent:
                                DatabaseFile databaseFile = nodeTag.Tag as DatabaseFile;

                                selectedFiles.Add(databaseFile);

                                break;
                        }
                    }

                    RunRecoverClusterTaskAsync(dialog.SelectedPath, selectedFiles);
                }
            }
        }

        private void listRecoverAllToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (var dialog = new FolderBrowserDialog())
            {
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    RunRecoverClusterTaskAsync(dialog.SelectedPath, _fileDatabase.GetRootFiles());

                    Console.WriteLine("Finished recovering files.");
                }
            }
        }

        private void listRecoverCurrentClusterToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (var dialog = new FolderBrowserDialog())
            {
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    var clusterNode = currentClusterNode;

                    NodeTag nodeTag = (NodeTag)clusterNode.Tag;

                    string clusterDir = dialog.SelectedPath + "/" + clusterNode.Text;

                    Directory.CreateDirectory(clusterDir);

                    switch (nodeTag.Type)
                    {
                        case NodeType.Cluster:
                            List<DatabaseFile> dirents = nodeTag.Tag as List<DatabaseFile>;

                            RunRecoverClusterTaskAsync(clusterDir, dirents);

                            break;
                    }

                    Console.WriteLine("Finished recovering files.");
                }
            }
        }

        private void treeRecoverSelectedToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (var dialog = new FolderBrowserDialog())
            {
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    var selectedNode = treeView1.SelectedNode;

                    NodeTag nodeTag = (NodeTag)selectedNode.Tag;
                    switch (nodeTag.Type)
                    {
                        case NodeType.Cluster:
                            List<DatabaseFile> dirents = nodeTag.Tag as List<DatabaseFile>;

                            string clusterDir = dialog.SelectedPath + "/" + selectedNode.Text;
                            Directory.CreateDirectory(clusterDir);
                            RunRecoverClusterTaskAsync(clusterDir, dirents);

                            break;

                        case NodeType.Dirent:
                            DatabaseFile dirent = nodeTag.Tag as DatabaseFile;

                            RunRecoverClusterTaskAsync(dialog.SelectedPath, new List<DatabaseFile> { dirent });

                            break;
                    }

                    Console.WriteLine("Finished recovering files.");
                }
            }
        }

        private void treeRecoverAllToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (var dialog = new FolderBrowserDialog())
            {
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    Dictionary<string, List<DatabaseFile>> clusterList = new Dictionary<string, List<DatabaseFile>>();

                    foreach (TreeNode clusterNode in treeView1.Nodes)
                    {
                        NodeTag nodeTag = (NodeTag)clusterNode.Tag;
                        switch (nodeTag.Type)
                        {
                            case NodeType.Cluster:
                                List<DatabaseFile> dirents = nodeTag.Tag as List<DatabaseFile>;

                                clusterList[clusterNode.Text] = dirents;

                                break;
                        }
                    }

                    RunRecoverAllTaskAsync(dialog.SelectedPath, clusterList);
                }
            }
        }

        private void listView1_ColumnClick(object sender, ColumnClickEventArgs e)
        {
            listViewItemComparer.Column = (ColumnIndex)e.Column;

            if (listViewItemComparer.Order == SortOrder.Ascending)
            {
                listViewItemComparer.Order = SortOrder.Descending;
            }
            else
            {
                listViewItemComparer.Order = SortOrder.Ascending;
            }

            listView1.Sort();
        }

        public enum ColumnIndex
        {
            Index,
            Name,
            Size,
            Created,
            Modified,
            Accessed,
            Offset,
            Cluster
        }

        class ListViewItemComparer : IComparer
        {
            private ColumnIndex column;
            private SortOrder order;

            public ColumnIndex Column
            {
                get => column;
                set => column = value;
            }

            public SortOrder Order
            {
                get => order;
                set => order = value;
            }

            public ListViewItemComparer()
            {
                this.order = SortOrder.Ascending;
                this.column = 0;
            }

            public ListViewItemComparer(ColumnIndex column)
            {
                this.column = column;
            }

            public int Compare(object x, object y)
            {
                // Default, don't swap order.
                int result = 0;

                ListViewItem itemX = (ListViewItem)x;
                ListViewItem itemY = (ListViewItem)y;

                if (itemX.Tag == null ||
                    itemY.Tag == null)
                {
                    return result;
                }

                if (itemX.Index == 0)
                {
                    // Skip "up" item
                    return result;
                }

                DatabaseFile direntX = (DatabaseFile)((NodeTag)itemX.Tag).Tag;
                DatabaseFile direntY = (DatabaseFile)((NodeTag)itemY.Tag).Tag;

                switch (column)
                {
                    case ColumnIndex.Index:
                        result = UInt32.Parse(itemX.Text).CompareTo(UInt32.Parse(itemY.Text));
                        break;
                    case ColumnIndex.Name:
                        result = String.Compare(direntX.FileName, direntY.FileName);
                        break;
                    case ColumnIndex.Size:
                        result = direntX.FileSize.CompareTo(direntY.FileSize);
                        break;
                    case ColumnIndex.Created:
                        result = direntX.CreationTime.AsDateTime().CompareTo(direntY.CreationTime.AsDateTime());
                        break;
                    case ColumnIndex.Modified:
                        result = direntX.LastWriteTime.AsDateTime().CompareTo(direntY.LastWriteTime.AsDateTime());
                        break;
                    case ColumnIndex.Accessed:
                        result = direntX.LastAccessTime.AsDateTime().CompareTo(direntY.LastAccessTime.AsDateTime());
                        break;
                    case ColumnIndex.Offset:
                        result = direntX.Offset.CompareTo(direntY.Offset);
                        break;
                    case ColumnIndex.Cluster:
                        result = direntX.Cluster.CompareTo(direntY.Cluster);
                        break;
                }

                if (order == SortOrder.Ascending)
                {
                    return result;
                }
                else
                {
                    return -result;
                }
            }
        }

        private void viewInformationToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (listView1.SelectedItems.Count == 0)
                return;

            NodeTag nodeTag = (NodeTag)listView1.SelectedItems[0].Tag;

            switch (nodeTag.Type)
            {
                case NodeType.Dirent:
                    DatabaseFile databaseFile = (DatabaseFile)nodeTag.Tag;

                    FileInfoDialog dialog = new FileInfoDialog(this._volume, databaseFile.GetDirent());
                    dialog.ShowDialog();

                    break;
            }
        }

        private void viewCollisionsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (listView1.SelectedItems.Count == 0)
                return;

            // TODO: Create a new view or dialog for this.
            NodeTag nodeTag = (NodeTag)listView1.SelectedItems[0].Tag;

            switch (nodeTag.Type)
            {
                case NodeType.Dirent:
                    DatabaseFile databaseFile = (DatabaseFile)nodeTag.Tag;

                    foreach (var collision in databaseFile.GetCollisions())
                    {
                        Console.WriteLine($"Cluster: {collision} (Offset: {_volume.ClusterReader.ClusterToPhysicalOffset(collision)})");
                        var occupants = _integrityAnalyzer.GetClusterOccupants(collision);
                        foreach (var occupant in occupants)
                        {
                            var o = occupant.GetDirent();
                            Console.WriteLine($"{o.GetRootDirectoryEntry().Cluster}/{o.GetFullPath()}");
                        }
                    }

                    break;
            }
        }

        private void editClusterChainToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (listView1.SelectedItems.Count == 0)
                return;

            NodeTag nodeTag = (NodeTag)listView1.SelectedItems[0].Tag;

            switch (nodeTag.Type)
            {
                case NodeType.Dirent:
                    DatabaseFile file = (DatabaseFile)nodeTag.Tag;

                    ClusterChainDialog dialog = new ClusterChainDialog(this._volume, file);
                    if (dialog.ShowDialog() == DialogResult.OK)
                    {
                        file.ClusterChain = dialog.NewClusterChain;

                        NotifyDatabaseChanged?.Invoke(null, null);

                        RefreshTreeView();
                    }

                    break;

            }
        }

        private void testToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // Target: 291581
            ListViewItem selectedItem = listView1.SelectedItems[0];

            NodeTag nodeTag = (NodeTag)selectedItem.Tag;

            switch (nodeTag.Type)
            {
                case NodeType.Dirent:
                    DatabaseFile databaseFile = (DatabaseFile)nodeTag.Tag;

                    if (!databaseFile.ClusterChain.Contains(291581))
                        databaseFile.ClusterChain.Add(291581);

                    break;
            }

            _fileDatabase.Update();
            PopulateTreeView(_fileDatabase.GetRootFiles());
        }

    }
}
