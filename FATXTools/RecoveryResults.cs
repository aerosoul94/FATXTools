using System;
using System.Collections.Generic;
using System.Windows.Forms;
using FATX;

namespace FATXTools
{
    public partial class RecoveryResults : UserControl
    {
        private MetadataAnalyzer _analyzer;
        private Dictionary<uint, List<DirectoryEntry>> clusterNodes = 
            new Dictionary<uint, List<DirectoryEntry>>();

        public RecoveryResults(MetadataAnalyzer analyzer)
        {
            InitializeComponent();

            this._analyzer = analyzer;
            PopulateResults(analyzer.GetRoot());
        }

        enum NodeType
        {
            Cluster,
            Dirent
        }

        struct NodeTag
        {
            public object Tag;
            public NodeType Type;
        }

        private void PopulateFolder(List<DirectoryEntry> children, TreeNode parent)
        {
            foreach (var child in children)
            {
                if (child.IsDirectory())
                {
                    var childNode = parent.Nodes.Add(child.FileName);
                    NodeTag nodeTag = new NodeTag();
                    nodeTag.Tag = child;
                    nodeTag.Type = NodeType.Dirent;
                    childNode.Tag = nodeTag;
                    PopulateFolder(child.GetChildren(), childNode);
                }
            }
        }

        public void PopulateResults(List<DirectoryEntry> results)
        {
            foreach (var result in results)
            {
                var cluster = result.GetCluster();
                if (!clusterNodes.ContainsKey(cluster))
                {
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
                    NodeTag nodeTag = new NodeTag();
                    nodeTag.Tag = clusterNodes[cluster];
                    nodeTag.Type = NodeType.Cluster;
                    clusterNode.Tag = nodeTag;
                }
                else
                {
                    clusterNode = treeView1.Nodes[clusterNodeText];
                }

                if (result.IsDirectory())
                {
                    var rootNode = clusterNode.Nodes.Add(result.FileName);
                    NodeTag nodeTag = new NodeTag();
                    nodeTag.Tag = result;
                    nodeTag.Type = NodeType.Dirent;
                    rootNode.Tag = nodeTag;
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

        private void PopulateListView(List<DirectoryEntry> dirents)
        {
            listView1.Items.Clear();

            int index = 1;
            foreach (DirectoryEntry dirent in dirents)
            {
                ListViewItem item = listView1.Items.Add(index.ToString());
                item.Tag = dirent;

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
            NodeTag nodeTag = (NodeTag)e.Node.Tag;
            switch (nodeTag.Type)
            {
                case NodeType.Cluster:
                    List<DirectoryEntry> dirents = (List<DirectoryEntry>)nodeTag.Tag;
                    PopulateListView(dirents);
                    break;
                case NodeType.Dirent:
                    DirectoryEntry dirent = (DirectoryEntry)nodeTag.Tag;
                    PopulateListView(dirent.GetChildren());
                    break;
            }
        }

        private void listView1_DoubleClick(object sender, EventArgs e)
        {
            if (listView1.SelectedItems.Count > 1)
                return;

            DirectoryEntry dirent = (DirectoryEntry)listView1.SelectedItems[0].Tag;
            if (dirent.IsDirectory())
            {
                PopulateListView(dirent.GetChildren());
            }
        }

        private void dumpToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var selectedItems = listView1.SelectedItems;
            using (var fbd = new FolderBrowserDialog())
            {
                if (fbd.ShowDialog() == DialogResult.OK)
                {
                    foreach (ListViewItem selectedItem in selectedItems)
                    {
                        DirectoryEntry dirent = (DirectoryEntry)selectedItem.Tag;
                        _analyzer.Dump(dirent, fbd.SelectedPath);
                    }
                }
            }
        }
    }
}
