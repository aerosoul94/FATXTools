using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using FATX;

namespace FATXTools
{
    public partial class RecoveryResults : UserControl
    {
        private MetadataAnalyzer _analyzer;

        /// <summary>
        /// Mapping of cluster index to it's directory entries.
        /// </summary>
        private Dictionary<uint, List<DirectoryEntry>> clusterNodes = 
            new Dictionary<uint, List<DirectoryEntry>>();

        /// <summary>
        /// Leads to the node of the current cluster.
        /// </summary>
        private TreeNode currentClusterNode;

        public RecoveryResults(MetadataAnalyzer analyzer)
        {
            InitializeComponent();

            this._analyzer = analyzer;
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

                DateTime creationTime = new DateTime(dirent.CreationTime.Year,
                    dirent.CreationTime.Month, dirent.CreationTime.Day,
                    dirent.CreationTime.Hour, dirent.CreationTime.Minute,
                    dirent.CreationTime.Second);
                DateTime lastWriteTime = new DateTime(dirent.LastWriteTime.Year,
                    dirent.LastWriteTime.Month, dirent.LastWriteTime.Day,
                    dirent.LastWriteTime.Hour, dirent.LastWriteTime.Minute,
                    dirent.LastWriteTime.Second);
                DateTime lastAccessTime = new DateTime(dirent.LastAccessTime.Year,
                    dirent.LastAccessTime.Month, dirent.LastAccessTime.Day,
                    dirent.LastAccessTime.Hour, dirent.LastAccessTime.Minute,
                    dirent.LastAccessTime.Second);

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

                                _analyzer.Dump(dirent, dialog.SelectedPath);
                                break;
                        }
                    }
                }
            }

            Console.WriteLine("Finished recovering files.");
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

                                _analyzer.Dump(dirent, dialog.SelectedPath);
                                break;
                        }
                    }
                }
            }

            Console.WriteLine("Finished recovering files.");
        }

        private void listRecoverAllToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (var dialog = new FolderBrowserDialog())
            {
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    foreach (var dirent in _analyzer.GetRootDirectory())
                    {
                        _analyzer.Dump(dirent, dialog.SelectedPath);
                    }
                }
            }

            Console.WriteLine("Finished recovering files.");
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

                            foreach (var dirent in dirents)
                            {
                                _analyzer.Dump(dirent, clusterDir);
                            }

                            break;
                    }

                    Console.WriteLine($"{clusterNode.Text}");
                }
            }

            Console.WriteLine("Finished recovering files.");
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

                            foreach (var dirent in dirents)
                            {
                                _analyzer.Dump(dirent, clusterDir);
                            }

                            break;
                    }
                }
            }

            Console.WriteLine("Finished recovering files.");
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

                                foreach (var dirent in dirents)
                                {
                                    _analyzer.Dump(dirent, clusterDir);
                                }

                                break;
                        }
                    }
                }
            }

            Console.WriteLine("Finished recovering files.");
        }
    }
}
