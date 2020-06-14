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
        private MetadataAnalyzer _analyzer;
        private Volume _volume;
        private TaskRunner _taskRunner;

        /// <summary>
        /// Mapping of cluster index to it's directory entries.
        /// </summary>
        private Dictionary<uint, List<DirectoryEntry>> clusterNodes =
            new Dictionary<uint, List<DirectoryEntry>>();

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

        public RecoveryResults(MetadataAnalyzer analyzer, FileDatabase database, IntegrityAnalyzer integrityAnalyzer, TaskRunner taskRunner)
        {
            InitializeComponent();

            this._analyzer = analyzer;
            this._fileDatabase = database;
            this._integrityAnalyzer = integrityAnalyzer;
            this._taskRunner = taskRunner;
            this._volume = analyzer.GetVolume();

            listViewItemComparer = new ListViewItemComparer();
            listView1.ListViewItemSorter = listViewItemComparer;

            PopulateTreeView(analyzer.GetRootDirectory());
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

        private void PopulateFolder(List<DirectoryEntry> children, TreeNode parent)
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

        public void PopulateTreeView(List<DirectoryEntry> results)
        {
            foreach (var result in results)
            {
                var cluster = result.Cluster;
                if (!clusterNodes.ContainsKey(cluster))
                {
                    // Initialize new 
                    List<DirectoryEntry> list = new List<DirectoryEntry>()
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

        private void PopulateListView(List<DirectoryEntry> dirents, DirectoryEntry parent)
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
                upDir.Tag = new NodeTag(nodeTag.Tag as List<DirectoryEntry>, NodeType.Cluster);
            }

            List<ListViewItem> items = new List<ListViewItem>();
            int index = 1;
            foreach (DirectoryEntry dirent in dirents)
            {
                ListViewItem item = new ListViewItem(index.ToString());
                item.Tag = new NodeTag(dirent, NodeType.Dirent);

                item.SubItems.Add(dirent.FileName);

                DateTime creationTime = dirent.CreationTime.AsDateTime();
                DateTime lastWriteTime = dirent.LastWriteTime.AsDateTime();
                DateTime lastAccessTime = dirent.LastAccessTime.AsDateTime();

                string sizeStr = "";
                if (!dirent.IsDirectory())
                {
                    item.ImageIndex = 1;
                    sizeStr = Utility.FormatBytes(dirent.FileSize);
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
                item.SubItems.Add(dirent.Cluster.ToString());

                //var statusItem = item.SubItems.Add("");
                var ranking = _fileDatabase.GetFile(dirent);
                item.BackColor = statusColor[ranking.Ranking];

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
                    List<DirectoryEntry> dirents = (List<DirectoryEntry>)nodeTag.Tag;

                    PopulateListView(dirents, null);

                    break;
                case NodeType.Dirent:
                    DirectoryEntry dirent = (DirectoryEntry)nodeTag.Tag;

                    PopulateListView(dirent.Children, dirent.GetParent());

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
                    DirectoryEntry dirent = nodeTag.Tag as DirectoryEntry;

                    if (dirent.IsDirectory())
                    {
                        PopulateListView(dirent.Children, dirent.GetParent());
                    }

                    break;
                case NodeType.Cluster:
                    List<DirectoryEntry> dirents = nodeTag.Tag as List<DirectoryEntry>;

                    PopulateListView(dirents, null);

                    break;
            }
        }
        private long CountFiles(List<DirectoryEntry> dirents)
        {
            // DirectoryEntry.CountFiles does not count deleted files
            long numFiles = 0;

            foreach (var dirent in dirents)
            {
                if (dirent.IsDirectory())
                {
                    numFiles += CountFiles(dirent.Children) + 1;
                }
                else
                {
                    numFiles++;
                }
            }

            return numFiles;
        }

        private async void RunRecoverAllTaskAsync(string path, Dictionary<string, List<DirectoryEntry>> clusters)
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

        private async void RunRecoverDirectoryEntryTaskAsync(string path, DirectoryEntry dirent)
        {
            RecoveryTask recoverTask = null;

            var numFiles = dirent.CountFiles();
            _taskRunner.Maximum = numFiles;
            _taskRunner.Interval = 1;

            await _taskRunner.RunTaskAsync("Save File",
                (CancellationToken cancellationToken, Progress<int> progress) =>
                {
                    recoverTask = new RecoveryTask(this._volume, cancellationToken, progress);
                    recoverTask.Save(path, dirent);
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

        private async void RunRecoverClusterTaskAsync(string path, List<DirectoryEntry> dirents)
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
                    List<DirectoryEntry> selectedFiles = new List<DirectoryEntry>();

                    foreach (ListViewItem selectedItem in selectedItems)
                    {
                        NodeTag nodeTag = (NodeTag)selectedItem.Tag;

                        switch (nodeTag.Type)
                        {
                            case NodeType.Dirent:
                                DirectoryEntry dirent = nodeTag.Tag as DirectoryEntry;

                                selectedFiles.Add(dirent);

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
                    List<DirectoryEntry> selectedFiles = new List<DirectoryEntry>();

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
                                DirectoryEntry dirent = nodeTag.Tag as DirectoryEntry;

                                selectedFiles.Add(dirent);

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
                    RunRecoverClusterTaskAsync(dialog.SelectedPath, _analyzer.GetRootDirectory());

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
                            List<DirectoryEntry> dirents = nodeTag.Tag as List<DirectoryEntry>;

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
                            List<DirectoryEntry> dirents = nodeTag.Tag as List<DirectoryEntry>;

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
                    Dictionary<string, List<DirectoryEntry>> clusterList = new Dictionary<string, List<DirectoryEntry>>();

                    foreach (TreeNode clusterNode in treeView1.Nodes)
                    {
                        NodeTag nodeTag = (NodeTag)clusterNode.Tag;
                        switch (nodeTag.Type)
                        {
                            case NodeType.Cluster:
                                List<DirectoryEntry> dirents = nodeTag.Tag as List<DirectoryEntry>;

                                clusterList[clusterNode.Text] = dirents;

                                //Save(dirents, clusterDir);

                                //foreach (var dirent in dirents)
                                //{
                                //    _analyzer.Dump(dirent, clusterDir);
                                //}

                                break;
                        }
                    }

                    RunRecoverAllTaskAsync(dialog.SelectedPath, clusterList);
                    //Console.WriteLine("Finished recovering files.");
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

            private void WriteFile(string path, DirectoryEntry dirent)
            {
                const int bufsize = 0x100000;
                var remains = dirent.FileSize;

                using (FileStream file = new FileStream(path, FileMode.Create))
                {
                    while (remains > 0)
                    {
                        var read = Math.Min(remains, bufsize);
                        remains -= read;
                        byte[] buf = new byte[read];
                        volume.GetReader().Read(buf, (int)read);
                        file.Write(buf, 0, (int)read);
                    }
                }
            }

            private void FileSetTimeStamps(string path, DirectoryEntry dirent)
            {
                File.SetCreationTime(path, dirent.CreationTime.AsDateTime());
                File.SetLastWriteTime(path, dirent.LastWriteTime.AsDateTime());
                File.SetLastAccessTime(path, dirent.LastAccessTime.AsDateTime());
            }

            private void DirectorySetTimestamps(string path, DirectoryEntry dirent)
            {
                Directory.SetCreationTime(path, dirent.CreationTime.AsDateTime());
                Directory.SetLastWriteTime(path, dirent.LastWriteTime.AsDateTime());
                Directory.SetLastAccessTime(path, dirent.LastAccessTime.AsDateTime());
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

            private void SaveDirectory(DirectoryEntry dirent, string path)
            {
                path = path + "\\" + dirent.FileName;
                //Console.WriteLine($"{path}");

                currentFile = dirent.FileName;
                progress.Report(numSaved++);

                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }

                foreach (DirectoryEntry child in dirent.Children)
                {
                    Save(path, child);
                }

                TryIOOperation(() =>
                {
                    DirectorySetTimestamps(path, dirent);
                });
            }

            private void SaveFile(DirectoryEntry dirent, string path)
            {
                path = path + "\\" + dirent.FileName;
                //Console.WriteLine($"{path}");

                currentFile = dirent.FileName;
                progress.Report(numSaved++);

                volume.SeekToCluster(dirent.FirstCluster);

                TryIOOperation(() =>
                {
                    WriteFile(path, dirent);

                    FileSetTimeStamps(path, dirent);
                });
            }

            public void Save(string path, DirectoryEntry dirent)
            {
                if (dirent.IsDirectory())
                {
                    SaveDirectory(dirent, path);
                }
                else
                {
                    SaveFile(dirent, path);
                }
            }

            public void SaveAll(string path, List<DirectoryEntry> dirents)
            {
                foreach (var dirent in dirents)
                {
                    Save(path, dirent);
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

                DirectoryEntry direntX = (DirectoryEntry)((NodeTag)itemX.Tag).Tag;
                DirectoryEntry direntY = (DirectoryEntry)((NodeTag)itemY.Tag).Tag;

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
                    DirectoryEntry dirent = (DirectoryEntry)nodeTag.Tag;

                    FileInfo dialog = new FileInfo(dirent);
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
                    DirectoryEntry dirent = (DirectoryEntry)nodeTag.Tag;

                    RecoveredFile rankedDirent = _integrityAnalyzer.GetRankedDirectoryEntry(dirent);

                    foreach (var collision in rankedDirent.Collisions)
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
    }
}
