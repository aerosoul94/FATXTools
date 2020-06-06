using System;
using System.IO;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Drawing;
using System.Collections;
using System.Threading;
using FATX;
using FATXTools.Utilities;

namespace FATXTools.Controls
{
    public partial class FileExplorer : UserControl
    {
        private Color deletedColor = Color.FromArgb(255, 200, 200);

        private PartitionView parent;
        private Volume volume;

        public event EventHandler OnMetadataAnalyzerCompleted;
        public event EventHandler OnFileCarverCompleted;

        private ListViewItemComparer listViewItemComparer;

        private TaskRunner taskRunner;

        private enum NodeType
        {
            Root,
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

        public FileExplorer(PartitionView parent, TaskRunner taskRunner, Volume volume)
        {
            InitializeComponent();

            this.parent = parent;
            this.taskRunner = taskRunner;
            this.volume = volume;

            this.listViewItemComparer = new ListViewItemComparer();
            this.listView1.ListViewItemSorter = this.listViewItemComparer;

            var rootNode = treeView1.Nodes.Add("Root");
            rootNode.Tag = new NodeTag(null, NodeType.Root);
            
            PopulateTreeNodeDirectory(rootNode, volume.GetRoot());
        }

        private void PopulateTreeNodeDirectory(TreeNode parentNode, List<DirectoryEntry> dirents)
        {
            foreach (var dirent in dirents)
            {
                if (dirent.IsDirectory())
                {
                    TreeNode node = parentNode.Nodes.Add(dirent.FileName);

                    node.Tag = new NodeTag(dirent, NodeType.Dirent);

                    if (dirent.IsDeleted())
                    {
                        node.ForeColor = Color.FromArgb(100, 100, 100);
                    }

                    PopulateTreeNodeDirectory(node, dirent.Children);
                }
            }
        }

        private void PopulateListView(List<DirectoryEntry> dirents, DirectoryEntry parent)
        {
            listView1.BeginUpdate();
            listView1.Items.Clear();

            // Add "up" item
            var upDir = listView1.Items.Add("");

            upDir.SubItems.Add("...");
            if (parent != null)
            {
                if (parent.GetParent() != null)
                {
                    upDir.Tag = new NodeTag(parent.GetParent(), NodeType.Dirent);
                }
                else
                {
                    upDir.Tag = new NodeTag(null, NodeType.Root);
                }
            }
            else
            {
                upDir.Tag = new NodeTag(null, NodeType.Root);
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
                    sizeStr = Utility.FormatBytes(dirent.FileSize);
                }

                item.SubItems.Add(sizeStr);
                item.SubItems.Add(creationTime.ToString());
                item.SubItems.Add(lastWriteTime.ToString());
                item.SubItems.Add(lastAccessTime.ToString());
                item.SubItems.Add("0x" + dirent.Offset.ToString("x"));
                item.SubItems.Add(dirent.Cluster.ToString());

                if (dirent.IsDeleted())
                {
                    item.BackColor = deletedColor;
                }

                index++;

                items.Add(item);
            }

            listView1.Items.AddRange(items.ToArray());
            listView1.EndUpdate();
        }
        
        private void treeView1_AfterSelect(object sender, TreeViewEventArgs e)
        {
            NodeTag nodeTag = (NodeTag)e.Node.Tag;
            
            switch (nodeTag.Type)
            {
                case NodeType.Dirent:
                    DirectoryEntry dirent = nodeTag.Tag as DirectoryEntry;

                    if (dirent.IsDeleted())
                    {
                        Console.WriteLine($"Cannot loads contents of a deleted directory: {dirent.FileName}");
                    }
                    else
                    {
                        PopulateListView(dirent.Children, dirent);
                    }

                    break;
                case NodeType.Root:

                    PopulateListView(this.volume.GetRoot(), null);

                    break;
            }
        }

        private void listView1_DoubleClick(object sender, EventArgs e)
        {
            if (listView1.SelectedItems.Count > 1)
            {
                return;
            }

            ListViewItem item = listView1.SelectedItems[0];
            NodeTag nodeTag = (NodeTag)item.Tag;

            switch (nodeTag.Type)
            {
                case NodeType.Dirent:
                    DirectoryEntry dirent = (DirectoryEntry)nodeTag.Tag;

                    if (!dirent.IsDirectory())
                    {
                        return;
                    }

                    if (dirent.IsDeleted())
                    {
                        Console.WriteLine($"Cannot display contents of a deleted directory: {dirent.FileName}");
                    }
                    else
                    {
                        PopulateListView(dirent.Children, dirent);
                    }

                    break;

                case NodeType.Root:
                    PopulateListView(this.volume.GetRoot(), null);
                    break;
            }
        }

        private async void runMetadataAnalyzerToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // TODO: Make into a user controlled setting
            var searchLength = this.volume.FileAreaLength;
            var searchInterval = this.volume.BytesPerCluster;

            taskRunner.Maximum = searchLength;
            taskRunner.Interval = searchInterval;

            MetadataAnalyzer analyzer = new MetadataAnalyzer(this.volume, searchInterval, searchLength);
            var numBlocks = searchLength / searchInterval;
            try
            {
                await taskRunner.RunTaskAsync("Metadata Analyzer",
                    // Task
                    (CancellationToken cancellationToken, Progress<int> progress) =>
                    {
                        analyzer.Analyze(cancellationToken, progress);
                    },
                    // Progress Update
                    (int progress) =>
                    {
                        //var progress = analyzer.GetProgress();
                        taskRunner.UpdateLabel($"Processing cluster {progress}/{numBlocks}");
                        taskRunner.UpdateProgress(progress);
                    },
                    // On Task Completion
                    () =>
                    {
                        OnMetadataAnalyzerCompleted?.Invoke(this, new MetadataAnalyzerResults()
                        {
                            analyzer = analyzer
                        });
                    });
            }
            catch (Exception exception)
            {
                MessageBox.Show(exception.Message);
            }
        }

        private async void runFileCarverToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // TODO: Make into a user controlled setting
            var searchLength = this.volume.FileAreaLength;
            var searchInterval = Properties.Settings.Default.FileCarverInterval;

            taskRunner.Maximum = searchLength;
            taskRunner.Interval = (long)searchInterval;

            FileCarver carver = new FileCarver(this.volume, searchInterval, searchLength);
            var numBlocks = searchLength / (long)searchInterval;
            try
            {
                await taskRunner.RunTaskAsync("File Carver",
                    // Task
                    (CancellationToken cancellationToken, Progress<int> progress) =>
                    {
                        carver.Analyze(cancellationToken, progress);
                    },
                    // Progress Update
                    (int progress) =>
                    {
                        //var progress = carver.GetProgress();
                        taskRunner.UpdateLabel($"Processing block {progress}/{numBlocks}");
                        taskRunner.UpdateProgress(progress);
                    },
                    // On Task Completion
                    () =>
                    {
                        OnFileCarverCompleted?.Invoke(this, new FileCarverResults()
                        {
                            carver = carver
                        });
                    });
            }
            catch (Exception exception)
            {
                MessageBox.Show(exception.Message);
            }
        }

        private void SaveNodeTag(string path, NodeTag nodeTag)
        {
            switch (nodeTag.Type)
            {
                case NodeType.Dirent:
                    DirectoryEntry dirent = nodeTag.Tag as DirectoryEntry;

                    RunSaveDirectoryEntryTaskAsync(path, dirent);

                    break;
                case NodeType.Root:
                    RunSaveAllTaskAsync(path, volume.GetRoot());
                    break;
            }
        }

        private void treeSaveSelectedToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog dialog = new FolderBrowserDialog();
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                SaveNodeTag(dialog.SelectedPath, (NodeTag)treeView1.SelectedNode.Tag);
            }
        }

        private void listSaveSelectedToolStripMenuItem_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog dialog = new FolderBrowserDialog();
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                List<DirectoryEntry> selectedFiles = new List<DirectoryEntry>();

                foreach (ListViewItem selected in listView1.SelectedItems)
                {
                    NodeTag nodeTag = (NodeTag)selected.Tag;
                    switch (nodeTag.Type)
                    {
                        case NodeType.Dirent:
                            DirectoryEntry dirent = nodeTag.Tag as DirectoryEntry;

                            selectedFiles.Add(dirent);

                            break;
                    }
                }

                RunSaveAllTaskAsync(dialog.SelectedPath, selectedFiles);
            }
        }

        private async void RunSaveDirectoryEntryTaskAsync(string path, DirectoryEntry dirent)
        {
            SaveContentTask saveContentTask = null;

            var numFiles = dirent.CountFiles();
            taskRunner.Maximum = numFiles;
            taskRunner.Interval = 1;

            await taskRunner.RunTaskAsync("Save File",
                (CancellationToken cancellationToken, Progress<int> progress) =>
                {
                    saveContentTask = new SaveContentTask(this.volume, cancellationToken, progress);
                    saveContentTask.Save(path, dirent);
                },
                (int progress) =>
                {
                    string currentFile = saveContentTask.GetCurrentFile();
                    taskRunner.UpdateLabel($"{progress}/{numFiles}: {currentFile}");
                    taskRunner.UpdateProgress(progress);
                },
                () =>
                {
                    Console.WriteLine("Finished saving files.");
                });
        }

        private async void RunSaveAllTaskAsync(string path, List<DirectoryEntry> dirents)
        {
            SaveContentTask saveContentTask = null;
            var numFiles = volume.CountFiles();
            taskRunner.Maximum = numFiles;
            taskRunner.Interval = 1;

            await taskRunner.RunTaskAsync("Save All",
                (CancellationToken cancellationToken, Progress<int> progress) =>
                {
                    try
                    {
                        saveContentTask = new SaveContentTask(this.volume, cancellationToken, progress);
                        saveContentTask.SaveAll(path, dirents);
                    }
                    catch (OperationCanceledException)
                    {
                        Console.WriteLine("Save all cancelled");
                    }
                },
                (int progress) =>
                {
                    string currentFile = saveContentTask.GetCurrentFile();
                    taskRunner.UpdateLabel($"{progress}/{numFiles}: {currentFile}");
                    taskRunner.UpdateProgress(progress);
                },
                () =>
                {
                    Console.WriteLine("Finished saving files.");
                });
        }

        private void saveAllToolStripMenuItem_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog dialog = new FolderBrowserDialog();
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                RunSaveAllTaskAsync(dialog.SelectedPath, volume.GetRoot());
            }
        }

        private void saveAllToolStripMenuItem2_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog dialog = new FolderBrowserDialog();
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                RunSaveAllTaskAsync(dialog.SelectedPath, volume.GetRoot());
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

        public class SaveContentTask
        {
            private CancellationToken cancellationToken;

            private IProgress<int> progress;

            private Volume volume;

            private string currentFile;

            private int numSaved;

            public SaveContentTask(Volume volume, CancellationToken cancellationToken, IProgress<int> progress)
            {
                currentFile = String.Empty;

                this.cancellationToken = cancellationToken;
                this.progress = progress;
                this.volume = volume;

                this.numSaved = 0;
            }

            public string GetCurrentFile()
            {
                // I am considering returning a DirectoryEntry instead to show
                // more information about the current file.
                return currentFile;
            }

            private DialogResult ShowIOErrorDialog(Exception e)
            {
                return MessageBox.Show($"{e.Message}",
                    "Error", MessageBoxButtons.RetryCancel, MessageBoxIcon.Error);
            }

            private void WriteFile(string path, DirectoryEntry dirent, List<uint> chainMap)
            {
                using (FileStream outFile = File.OpenWrite(path))
                {
                    uint bytesLeft = dirent.FileSize;

                    foreach (uint cluster in chainMap)
                    {
                        byte[] clusterData = this.volume.ReadCluster(cluster);

                        var writeSize = Math.Min(bytesLeft, this.volume.BytesPerCluster);
                        outFile.Write(clusterData, 0, (int)writeSize);

                        bytesLeft -= writeSize;
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

            private void SaveFile(string path, DirectoryEntry dirent)
            {
                path = path + "\\" + dirent.FileName;
                Console.WriteLine(path);

                // Report where we are at
                currentFile = dirent.FileName;
                progress.Report(numSaved++);

                List<uint> chainMap = this.volume.GetClusterChain(dirent);

                TryIOOperation(() =>
                {
                    WriteFile(path, dirent, chainMap);

                    FileSetTimeStamps(path, dirent);
                });

                if (cancellationToken.IsCancellationRequested)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                }
            }

            private void SaveDirectory(string path, DirectoryEntry dirent)
            {
                path = path + "\\" + dirent.FileName;
                Console.WriteLine(path);

                // Report where we are at
                currentFile = dirent.FileName;
                progress.Report(numSaved++);

                Directory.CreateDirectory(path);

                foreach (DirectoryEntry child in dirent.Children)
                {
                    SaveDirectoryEntry(path, child);
                }

                TryIOOperation(() =>
                {
                    DirectorySetTimestamps(path, dirent);
                });

                if (cancellationToken.IsCancellationRequested)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                }
            }

            private void SaveDeleted(string path, DirectoryEntry dirent)
            {
                path = path + "\\" + dirent.FileName;

                currentFile = dirent.GetFullPath();

                Console.WriteLine($"{path}: Cannot save deleted files.");
            }

            private void SaveDirectoryEntry(string path, DirectoryEntry dirent)
            {
                if (dirent.IsDeleted())
                {
                    SaveDeleted(path, dirent);
                    return;
                }

                if (dirent.IsDirectory())
                {
                    SaveDirectory(path, dirent);
                }
                else
                {
                    SaveFile(path, dirent);
                }
            }

            public void Save(string path, DirectoryEntry dirent)
            {
                SaveDirectoryEntry(path, dirent);
            }

            public void SaveAll(string path, List<DirectoryEntry> dirents)
            {
                foreach (var dirent in dirents)
                {
                    SaveDirectoryEntry(path, dirent);
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
    }
}
