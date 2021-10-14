using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

using FATX.Analyzers;
using FATX.FileSystem;

using FATXTools.Dialogs;
using FATXTools.Tasks;
using FATXTools.Utilities;

namespace FATXTools.Controls
{
    public partial class FileExplorer : UserControl
    {
        private Volume _volume;

        private ListViewItemComparer _listViewItemComparer;

        public event EventHandler OnMetadataAnalyzerCompleted;
        public event EventHandler OnFileCarverCompleted;

        private static readonly Color DeletedColor = Color.FromArgb(255, 200, 200);

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
                Tag = tag;
                Type = type;
            }
        }

        public FileExplorer(Volume volume)
        {
            InitializeComponent();

            _volume = volume;

            _listViewItemComparer = new ListViewItemComparer();
            listView1.ListViewItemSorter = _listViewItemComparer;

            var rootNode = treeView1.Nodes.Add("Root");
            rootNode.Tag = new NodeTag(null, NodeType.Root);

            PopulateTreeNodeDirectory(rootNode, volume.Root);
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
                if (parent.Parent != null)
                {
                    upDir.Tag = new NodeTag(parent.Parent, NodeType.Dirent);
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
                ListViewItem item = new ListViewItem(index.ToString())
                {
                    Tag = new NodeTag(dirent, NodeType.Dirent)
                };

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
                    item.BackColor = DeletedColor;
                }

                index++;

                items.Add(item);
            }

            listView1.Items.AddRange(items.ToArray());
            listView1.EndUpdate();
        }

        private void OpenDirectory(DirectoryEntry dirent)
        {
            if (!dirent.IsDirectory())
                return;

            if (dirent.IsDeleted())
                Console.WriteLine($"Cannot loads contents of a deleted directory: {dirent.FileName}");
            else
                PopulateListView(dirent.Children, dirent);
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
                    RunSaveAllTaskAsync(path, _volume.Root);
                    break;
            }
        }

        private async void RunSaveDirectoryEntryTaskAsync(string path, DirectoryEntry dirent)
        {
            var options = new TaskDialogOptions() { Title = "Save File" };

            await TaskRunner.Instance.RunTaskAsync(ParentForm, options, SaveContentTask.RunSaveTask(_volume, path, dirent));
        }

        private async void RunSaveAllTaskAsync(string path, List<DirectoryEntry> dirents)
        {
            var options = new TaskDialogOptions() { Title = "Save All" };

            await TaskRunner.Instance.RunTaskAsync(ParentForm, options, SaveContentTask.RunSaveAllTask(_volume, path, dirents));
        }

        #region Common ContextMenu Actions
        private async void runMetadataAnalyzerToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // TODO: Make into a user controlled setting
            var searchLength = _volume.FileAreaLength;
            var searchInterval = _volume.BytesPerCluster;

            var options = new TaskDialogOptions() { Title = "Metadata Analyzer" };
            List<DirectoryEntry> results = null;

            MetadataAnalyzer analyzer = new MetadataAnalyzer(_volume, searchInterval);
            await TaskRunner.Instance.RunTaskAsync(ParentForm, options,
                (cancellationToken, progress) => results = analyzer.Analyze(cancellationToken, progress)
            );

            OnMetadataAnalyzerCompleted?.Invoke(this, new MetadataAnalyzerResults()
            {
                Results = results
            });
        }

        private async void runFileCarverToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // TODO: Make into a user controlled setting
            var searchLength = _volume.FileAreaLength;
            var searchInterval = Properties.Settings.Default.FileCarverInterval;

            var options = new TaskDialogOptions() { Title = "File Carver" };
            List<CarvedFile> results = null;

            FileCarver carver = new FileCarver(_volume, searchInterval);
            await TaskRunner.Instance.RunTaskAsync(ParentForm, options,
                (cancellationToken, progress) => results = carver.Analyze(cancellationToken, progress)
            );

            OnFileCarverCompleted?.Invoke(this, new FileCarverResults()
            {
                Results = results
            });
        }
        #endregion

        #region TreeView ContextMenu Actions
        private void treeSaveSelectedToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog dialog = new FolderBrowserDialog();
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                SaveNodeTag(dialog.SelectedPath, (NodeTag)treeView1.SelectedNode.Tag);
            }
        }
        #endregion

        #region ListView ContextMenu Actions
        // ListView ContextMenu Actions
        private void saveAllToolStripMenuItem_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog dialog = new FolderBrowserDialog();
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                RunSaveAllTaskAsync(dialog.SelectedPath, _volume.Root);
            }
        }

        // ListView ContextMenu Actions
        private void saveAllToolStripMenuItem2_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog dialog = new FolderBrowserDialog();
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                RunSaveAllTaskAsync(dialog.SelectedPath, _volume.Root);
            }
        }

        // ListView ContextMenu Actions
        private void viewInformationToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (listView1.SelectedItems.Count == 0)
                return;

            NodeTag nodeTag = (NodeTag)listView1.SelectedItems[0].Tag;

            switch (nodeTag.Type)
            {
                case NodeType.Dirent:
                    DirectoryEntry dirent = (DirectoryEntry)nodeTag.Tag;

                    FileInfoDialog dialog = new FileInfoDialog(_volume, dirent);
                    dialog.ShowDialog();

                    break;
            }
        }

        // ListView ContextMenu Actions
        private void listSaveSelectedToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (listView1.SelectedItems.Count == 0)
                return;

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
        #endregion

        #region TreeView Events
        private void treeView1_AfterSelect(object sender, TreeViewEventArgs e)
        {
            NodeTag nodeTag = (NodeTag)e.Node.Tag;

            switch (nodeTag.Type)
            {
                case NodeType.Dirent:
                    DirectoryEntry dirent = nodeTag.Tag as DirectoryEntry;

                    OpenDirectory(dirent);

                    break;
                case NodeType.Root:

                    PopulateListView(_volume.Root, null);

                    break;
            }
        }
        #endregion

        #region ListView Events
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

                    OpenDirectory(dirent);

                    break;

                case NodeType.Root:
                    PopulateListView(_volume.Root, null);
                    break;
            }
        }

        private void listView1_ColumnClick(object sender, ColumnClickEventArgs e)
        {
            _listViewItemComparer.Column = (ColumnIndex)e.Column;

            if (_listViewItemComparer.Order == SortOrder.Ascending)
            {
                _listViewItemComparer.Order = SortOrder.Descending;
            }
            else
            {
                _listViewItemComparer.Order = SortOrder.Ascending;
            }

            listView1.Sort();
        }
        #endregion

        #region ListView Comparer
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
            private ColumnIndex _column;
            private SortOrder _order;

            public ColumnIndex Column
            {
                get => _column;
                set => _column = value;
            }

            public SortOrder Order
            {
                get => _order;
                set => _order = value;
            }

            public ListViewItemComparer()
            {
                _order = SortOrder.Ascending;
                _column = 0;
            }

            public ListViewItemComparer(ColumnIndex column)
            {
                _column = column;
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

                switch (_column)
                {
                    case ColumnIndex.Index:
                        result = uint.Parse(itemX.Text).CompareTo(uint.Parse(itemY.Text));
                        break;
                    case ColumnIndex.Name:
                        result = string.Compare(direntX.FileName, direntY.FileName);
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

                if (_order == SortOrder.Ascending)
                {
                    return result;
                }
                else
                {
                    return -result;
                }
            }
        }
        #endregion
    }
}
