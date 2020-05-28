using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using FATX;
using FATX.Analyzers;
using FATXTools.Utilities;

namespace FATXTools
{
    public partial class RecoveryResults : UserControl
    {
        private MetadataAnalyzer _analyzer;
        private Volume _volume;

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

        private Color[] statusColor = new Color[] 
        { 
            Color.FromArgb(150, 250, 150), // Green
            Color.FromArgb(200, 250, 150), // Yellow-Green
            Color.FromArgb(250, 250, 150),
            Color.FromArgb(250, 200, 150),
            Color.FromArgb(250, 150, 150),
        };

        public RecoveryResults(MetadataAnalyzer analyzer, IntegrityAnalyzer integrityAnalyzer)
        {
            InitializeComponent();

            this._analyzer = analyzer;
            this._integrityAnalyzer = integrityAnalyzer;
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
                    PopulateFolder(child.GetChildren(), childNode);
                }
            }
        }

        public void PopulateTreeView(List<DirectoryEntry> results)
        {
            foreach (var result in results)
            {
                var cluster = result.GetCluster();
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

                var clusterNodeText = "Cluster " + result.GetCluster();
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
                    PopulateFolder(result.GetChildren(), rootNode);
                }
            }
        }

        private void PopulateListView(List<DirectoryEntry> dirents, DirectoryEntry parent)
        {
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

            int index = 1;
            foreach (DirectoryEntry dirent in dirents)
            {
                ListViewItem item = listView1.Items.Add(index.ToString());
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
                item.SubItems.Add(dirent.GetCluster().ToString());

                //var statusItem = item.SubItems.Add("");
                var ranking = _integrityAnalyzer.GetRankedDirectoryEntry(dirent);
                item.BackColor = statusColor[ranking.Ranking];

                index++;
            }
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

                    PopulateListView(dirent.GetChildren(), dirent.GetParent());

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
                        PopulateListView(dirent.GetChildren(), dirent.GetParent());
                    }

                    break;
                case NodeType.Cluster:
                    List<DirectoryEntry> dirents = nodeTag.Tag as List<DirectoryEntry>;

                    PopulateListView(dirents, null);

                    break;
            }
        }

        private DialogResult ShowIOErrorDialog(Exception e)
        {
            return MessageBox.Show($"{e.Message}\n\n" +
                "Try Again?",
                "Error", MessageBoxButtons.YesNo, MessageBoxIcon.Error);
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
                    _volume.Reader.Read(buf, (int)read);
                    file.Write(buf, 0, (int)read);
                }
            }
        }

        private void TryFileWrite(string path, DirectoryEntry dirent)
        {
            try
            {
                WriteFile(path, dirent);

                FileSetTimeStamps(path, dirent);
            }
            catch (IOException e)
            {
                // TODO: make sure that its actually file access exception.
                while (true)
                {
                    var dialogResult = ShowIOErrorDialog(e);

                    if (dialogResult == DialogResult.Yes)
                    {
                        try
                        {
                            WriteFile(path, dirent);

                            FileSetTimeStamps(path, dirent);
                        }
                        catch (Exception)
                        {
                            continue;
                        }
                    }

                    // On success, or if No is selected, we will exit the loop.
                    break;
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

        private void TryDirectorySetTimestamps(string path, DirectoryEntry dirent)
        {
            try
            {
                DirectorySetTimestamps(path, dirent);
            }
            catch (IOException e)
            {
                while (true)
                {
                    var dialogResult = ShowIOErrorDialog(e);

                    if (dialogResult == DialogResult.Yes)
                    {
                        try
                        {
                            DirectorySetTimestamps(path, dirent);
                        }
                        catch (Exception)
                        {
                            continue;
                        }
                    }

                    // On success, or if No is selected, we will exit the loop.
                    break;
                }
            }
        }

        private void SaveDirectory(DirectoryEntry dirent, string path)
        {
            path = path + "\\" + dirent.FileName;
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            foreach (DirectoryEntry child in dirent.GetChildren())
            {
                Save(child, path);
            }

            TryDirectorySetTimestamps(path, dirent);
        }

        private void SaveFile(DirectoryEntry dirent, string path)
        {
            path = path + "\\" + dirent.FileName;
            _volume.SeekToCluster(dirent.FirstCluster);

            TryFileWrite(path, dirent);
        }

        private void Save(DirectoryEntry dirent, string path)
        {
            Console.WriteLine($"{path + dirent.GetFullPath()}");

            if (dirent.IsDirectory())
            {
                SaveDirectory(dirent, path);
            }
            else
            {
                SaveFile(dirent, path);
            }
        }

        private void Save(List<DirectoryEntry> dirents, string path)
        {
            foreach (var dirent in dirents)
            {
                Save(dirent, path);
            }
        }

        private void listRecoverSelectedToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var selectedItems = listView1.SelectedItems;
            using (var dialog = new FolderBrowserDialog())
            {
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    foreach (ListViewItem selectedItem in selectedItems)
                    {
                        NodeTag nodeTag = (NodeTag)selectedItem.Tag;

                        switch (nodeTag.Type)
                        {
                            case NodeType.Dirent:
                                DirectoryEntry dirent = nodeTag.Tag as DirectoryEntry;

                                Save(dirent, dialog.SelectedPath);

                                //_analyzer.Dump(dirent, dialog.SelectedPath);
                                break;
                        }
                    }

                    Console.WriteLine("Finished recovering files.");
                }
            }
        }

        private void listRecoverCurrentDirectoryToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (var dialog = new FolderBrowserDialog())
            {
                if (dialog.ShowDialog() == DialogResult.OK)
                {
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

                                Save(dirent, dialog.SelectedPath);

                                //_analyzer.Dump(dirent, dialog.SelectedPath);
                                break;
                        }
                    }

                    Console.WriteLine("Finished recovering files.");
                }
            }
        }

        private void listRecoverAllToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (var dialog = new FolderBrowserDialog())
            {
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    Save(_analyzer.GetRootDirectory(), dialog.SelectedPath);

                    //foreach (var dirent in _analyzer.GetRootDirectory())
                    //{
                    //    _analyzer.Dump(dirent, dialog.SelectedPath);
                    //}

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

                            Save(dirents, clusterDir);

                            //foreach (var dirent in dirents)
                            //{
                            //    _analyzer.Dump(dirent, clusterDir);
                            //}

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

                            Save(dirents, clusterDir);

                            //foreach (var dirent in dirents)
                            //{
                            //    _analyzer.Dump(dirent, clusterDir);
                            //}

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
                    foreach (TreeNode clusterNode in treeView1.Nodes)
                    {
                        string clusterDir = dialog.SelectedPath + "/" + clusterNode.Text;

                        Directory.CreateDirectory(clusterDir);

                        NodeTag nodeTag = (NodeTag)clusterNode.Tag;
                        switch (nodeTag.Type)
                        {
                            case NodeType.Cluster:
                                List<DirectoryEntry> dirents = nodeTag.Tag as List<DirectoryEntry>;

                                Save(dirents, clusterDir);

                                //foreach (var dirent in dirents)
                                //{
                                //    _analyzer.Dump(dirent, clusterDir);
                                //}

                                break;
                        }
                    }

                    Console.WriteLine("Finished recovering files.");
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
                        result = direntX.GetCluster().CompareTo(direntY.GetCluster());
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

                    RankedDirectoryEntry rankedDirent = _integrityAnalyzer.GetRankedDirectoryEntry(dirent);

                    foreach (var collision in rankedDirent.Collisions)
                    {
                        Console.WriteLine($"Cluster: {collision} (Offset: {_volume.ClusterToPhysicalOffset(collision)})");
                        var occupants = _integrityAnalyzer.GetClusterOccupants(collision);
                        foreach (var occupant in occupants)
                        {
                            var o = occupant.GetDirent();
                            Console.WriteLine($"{o.GetRootDirectoryEntry().GetCluster()}/{o.GetFullPath()}");
                        }
                    }

                    break;
            }
        }
    }
}
