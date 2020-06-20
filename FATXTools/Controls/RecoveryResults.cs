using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using FATX;
using FATX.Analyzers;
using FATXTools.Database;
using FATXTools.Utilities;

namespace FATXTools
{
    public partial class RecoveryResults : UserControl
    {
        private Volume _volume;
        private TaskRunner _taskRunner;

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

        public RecoveryResults(FileDatabase database, IntegrityAnalyzer integrityAnalyzer, TaskRunner taskRunner)
        {
            InitializeComponent();

            this._fileDatabase = database;
            this._integrityAnalyzer = integrityAnalyzer;
            this._taskRunner = taskRunner;
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

        public void PopulateTreeView(List<DatabaseFile> results)
        {
            // Remove all nodes
            treeView1.Nodes.Clear();

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
        private long CountFiles(List<DatabaseFile> dirents)
        {
            // DirectoryEntry.CountFiles does not count deleted files
            long numFiles = 0;

            foreach (var databaseFile in dirents)
            {
                if (databaseFile.IsDirectory())
                {
                    numFiles += CountFiles(databaseFile.Children) + 1;
                }
                else
                {
                    numFiles++;
                }
            }

            return numFiles;
        }

        private async void RunRecoverAllTaskAsync(string path, Dictionary<string, List<DatabaseFile>> clusters)
        {
            // TODO: There should be a better way to run this.
            RecoveryTask recoverTask = null;

            long numFiles = 0;

            foreach (var cluster in clusters)
            {
                numFiles += CountFiles(cluster.Value);
            }

            _taskRunner.Maximum = numFiles;
            _taskRunner.Interval = 1;

            await _taskRunner.RunTaskAsync("Save File",
                (CancellationToken cancellationToken, Progress<int> progress) =>
                {
                    recoverTask = new RecoveryTask(this._volume, cancellationToken, progress);
                    foreach (var cluster in clusters)
                    {
                        string clusterDir = path + "\\" + cluster.Key;

                        Directory.CreateDirectory(clusterDir);

                        recoverTask.SaveAll(clusterDir, cluster.Value);
                    }
                },
                (int progress) =>
                {
                    string currentFile = recoverTask.GetCurrentFile();
                    _taskRunner.UpdateLabel($"{progress}/{numFiles}: {currentFile}");
                    _taskRunner.UpdateProgress(progress);
                },
                () =>
                {
                    Console.WriteLine("Finished saving files.");
                });
        }

        private async void RunRecoverDirectoryEntryTaskAsync(string path, DatabaseFile databaseFile)
        {
            RecoveryTask recoverTask = null;

            var numFiles = databaseFile.CountFiles();
            _taskRunner.Maximum = numFiles;
            _taskRunner.Interval = 1;

            await _taskRunner.RunTaskAsync("Save File",
                (CancellationToken cancellationToken, Progress<int> progress) =>
                {
                    recoverTask = new RecoveryTask(this._volume, cancellationToken, progress);
                    recoverTask.Save(path, databaseFile);
                },
                (int progress) =>
                {
                    string currentFile = recoverTask.GetCurrentFile();
                    _taskRunner.UpdateLabel($"{progress}/{numFiles}: {currentFile}");
                    _taskRunner.UpdateProgress(progress);
                },
                () =>
                {
                    Console.WriteLine("Finished saving files.");
                });
        }

        private async void RunRecoverClusterTaskAsync(string path, List<DatabaseFile> dirents)
        {
            RecoveryTask recoverTask = null;

            long numFiles = CountFiles(dirents);

            _taskRunner.Maximum = numFiles;
            _taskRunner.Interval = 1;

            await _taskRunner.RunTaskAsync("Save All",
                (CancellationToken cancellationToken, Progress<int> progress) =>
                {
                    try
                    {
                        recoverTask = new RecoveryTask(this._volume, cancellationToken, progress);
                        recoverTask.SaveAll(path, dirents);
                    }
                    catch (OperationCanceledException)
                    {
                        Console.WriteLine("Save all cancelled");
                    }
                },
                (int progress) =>
                {
                    string currentFile = recoverTask.GetCurrentFile();
                    _taskRunner.UpdateLabel($"{progress}/{numFiles}: {currentFile}");
                    _taskRunner.UpdateProgress(progress);
                },
                () =>
                {
                    Console.WriteLine("Finished saving files.");
                });
        }

        private void listRecoverSelectedToolStripMenuItem_Click(object sender, EventArgs e)
        {
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
                    var clusterNode = treeView1.SelectedNode;

                    string clusterDir = dialog.SelectedPath + "/" + clusterNode.Text;

                    Directory.CreateDirectory(clusterDir);

                    NodeTag nodeTag = (NodeTag)clusterNode.Tag;
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

        private class RecoveryTask
        {
            private CancellationToken cancellationToken;
            private IProgress<int> progress;
            private Volume volume;

            private string currentFile = String.Empty;
            private int numSaved = 0;

            public RecoveryTask(Volume volume, CancellationToken cancellationToken, IProgress<int> progress)
            {
                this.volume = volume;
                this.cancellationToken = cancellationToken;
                this.progress = progress;
            }

            public string GetCurrentFile()
            {
                return currentFile;
            }

            private DialogResult ShowIOErrorDialog(Exception e)
            {
                return MessageBox.Show($"{e.Message}",
                    "Error", MessageBoxButtons.RetryCancel, MessageBoxIcon.Error);
            }

            private void WriteFile(string path, DatabaseFile databaseFile)
            {
                using (FileStream outFile = File.OpenWrite(path))
                {
                    uint bytesLeft = databaseFile.FileSize;

                    foreach (uint cluster in databaseFile.ClusterChain)
                    {
                        byte[] clusterData = this.volume.ReadCluster(cluster);

                        var writeSize = Math.Min(bytesLeft, this.volume.BytesPerCluster);
                        outFile.Write(clusterData, 0, (int)writeSize);

                        bytesLeft -= writeSize;
                    }
                }
            }

            private void FileSetTimeStamps(string path, DatabaseFile databaseFile)
            {
                File.SetCreationTime(path, databaseFile.CreationTime.AsDateTime());
                File.SetLastWriteTime(path, databaseFile.LastWriteTime.AsDateTime());
                File.SetLastAccessTime(path, databaseFile.LastAccessTime.AsDateTime());
            }

            private void DirectorySetTimestamps(string path, DatabaseFile databaseFile)
            {
                Directory.SetCreationTime(path, databaseFile.CreationTime.AsDateTime());
                Directory.SetLastWriteTime(path, databaseFile.LastWriteTime.AsDateTime());
                Directory.SetLastAccessTime(path, databaseFile.LastAccessTime.AsDateTime());
            }

            private void TryIOOperation(Action action)
            {
                try
                {
                    action();
                }
                catch (IOException e)
                {
                    while (true)
                    {
                        var dialogResult = ShowIOErrorDialog(e);

                        if (dialogResult == DialogResult.Retry)
                        {
                            try
                            {
                                action();
                            }
                            catch (Exception)
                            {
                                continue;
                            }
                        }

                        break;
                    }
                }
            }

            private void SaveDirectory(DatabaseFile databaseFile, string path)
            {
                path = path + "\\" + databaseFile.FileName;
                //Console.WriteLine($"{path}");

                currentFile = databaseFile.FileName;
                progress.Report(numSaved++);

                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }

                foreach (DatabaseFile child in databaseFile.Children)
                {
                    Save(path, child);
                }

                TryIOOperation(() =>
                {
                    DirectorySetTimestamps(path, databaseFile);
                });
            }

            private void SaveFile(DatabaseFile databaseFile, string path)
            {
                path = path + "\\" + databaseFile.FileName;
                //Console.WriteLine($"{path}");

                currentFile = databaseFile.FileName;
                progress.Report(numSaved++);

                volume.SeekToCluster(databaseFile.FirstCluster);

                TryIOOperation(() =>
                {
                    WriteFile(path, databaseFile);

                    FileSetTimeStamps(path, databaseFile);
                });
            }

            public void Save(string path, DatabaseFile databaseFile)
            {
                if (databaseFile.IsDirectory())
                {
                    SaveDirectory(databaseFile, path);
                }
                else
                {
                    SaveFile(databaseFile, path);
                }
            }

            public void SaveAll(string path, List<DatabaseFile> dirents)
            {
                foreach (var databaseFile in dirents)
                {
                    Save(path, databaseFile);
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
            NodeTag nodeTag = (NodeTag)listView1.SelectedItems[0].Tag;

            switch (nodeTag.Type)
            {
                case NodeType.Dirent:
                    DatabaseFile databaseFile = (DatabaseFile)nodeTag.Tag;

                    FileInfo dialog = new FileInfo(this._volume, databaseFile.GetDirent());
                    dialog.ShowDialog();

                    break;
            }
        }

        private void viewCollisionsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // TODO: Create a new view or dialog for this.
            NodeTag nodeTag = (NodeTag)listView1.SelectedItems[0].Tag;

            switch (nodeTag.Type)
            {
                case NodeType.Dirent:
                    DatabaseFile databaseFile = (DatabaseFile)nodeTag.Tag;

                    foreach (var collision in databaseFile.GetCollisions())
                    {
                        Console.WriteLine($"Cluster: {collision} (Offset: {_volume.ClusterToPhysicalOffset(collision)})");
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
