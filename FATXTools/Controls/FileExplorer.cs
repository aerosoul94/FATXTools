using System;
using System.IO;
using System.Windows.Forms;
using System.Collections.Generic;
using FATX;
using System.ComponentModel;
using System.Drawing;

namespace FATXTools.Controls
{
    public partial class FileExplorer : UserControl
    {
        private Color deletedColor = Color.FromArgb(255, 200, 200);

        private PartitionView parent;
        private Volume volume;
        private AnalyzerProgress progressBar;

        public event EventHandler OnMetadataAnalyzerCompleted;
        public event EventHandler OnFileCarverCompleted;

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

        public FileExplorer(PartitionView parent, Volume volume)
        {
            InitializeComponent();

            this.parent = parent;
            this.volume = volume;

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

                    PopulateTreeNodeDirectory(node, dirent.GetChildren());
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
                    //item.ImageIndex = 1;
                    sizeStr = FormatBytes(dirent.FileSize);
                }
                else
                {
                    //item.ImageIndex = 0;
                }

                item.SubItems.Add(sizeStr);
                item.SubItems.Add(creationTime.ToString());
                item.SubItems.Add(lastWriteTime.ToString());
                item.SubItems.Add(lastAccessTime.ToString());
                item.SubItems.Add("0x" + dirent.Offset.ToString("x"));
                item.SubItems.Add(dirent.GetCluster().ToString());

                if (dirent.IsDeleted())
                {
                    item.BackColor = deletedColor;
                }

                index++;
            }
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
                        PopulateListView(dirent.GetChildren(), dirent);
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
                        PopulateListView(dirent.GetChildren(), dirent);
                    }

                    break;

                case NodeType.Root:
                    PopulateListView(this.volume.GetRoot(), null);
                    break;
            }
        }

        private void runMetadataAnalyzerToolStripMenuItem_Click(object sender, EventArgs e)
        {
            progressBar = new AnalyzerProgress(this.ParentForm, this.volume.FileAreaLength, this.volume.BytesPerCluster);
            progressBar.Show();
            backgroundWorker1.RunWorkerAsync();
        }

        private void runFileCarverToolStripMenuItem_Click(object sender, EventArgs e)
        {
            progressBar = new AnalyzerProgress(this.ParentForm, this.volume.FileAreaLength, (long)FileCarverInterval.Cluster);
            progressBar.Show();
            backgroundWorker2.RunWorkerAsync();
        }

        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker worker = sender as BackgroundWorker;
            MetadataAnalyzer analyzer = new MetadataAnalyzer(this.volume, volume.BytesPerCluster, volume.FileAreaLength);
            analyzer.Analyze(worker);
            e.Result = analyzer;
        }

        private void backgroundWorker2_DoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker worker = sender as BackgroundWorker;
            FileCarver carver = new FileCarver(this.volume, FileCarverInterval.Cluster, volume.FileAreaLength);
            carver.Analyze(worker);
            e.Result = carver;
        }

        private void backgroundWorker1_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            progressBar.UpdateProgress(e.ProgressPercentage);
        }

        private void backgroundWorker2_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            progressBar.UpdateProgress(e.ProgressPercentage);
        }

        private void backgroundWorker1_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            OnMetadataAnalyzerCompleted?.Invoke(this, new MetadataAnalyzerResults()
            {
                analyzer = (MetadataAnalyzer)e.Result
            });

            progressBar.Close();
            progressBar = null;
        }

        private void backgroundWorker2_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            OnFileCarverCompleted?.Invoke(this, new FileCarverResults()
            {
                carver = (FileCarver)e.Result
            });

            progressBar.Close();
            progressBar = null;
        }

        private void SaveNodeTag(string path, NodeTag nodeTag)
        {
            switch (nodeTag.Type)
            {
                case NodeType.Dirent:
                    DirectoryEntry dirent = nodeTag.Tag as DirectoryEntry;
                    dirent.Save(path);
                    break;
                case NodeType.Root:
                    SaveAll(path);
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

        private DialogResult ShowIOErrorDialog(Exception e)
        {
            return MessageBox.Show($"{e.Message}\n\n" + 
                "Retry?",
                "Error", MessageBoxButtons.YesNo, MessageBoxIcon.Error);
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

        private void TryFileWrite(string path, DirectoryEntry dirent, List<uint> chainMap)
        {
            try
            {
                WriteFile(path, dirent, chainMap);

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
                            WriteFile(path, dirent, chainMap);

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

        private void SaveFile(string path, DirectoryEntry dirent)
        {
            path = path + "\\" + dirent.FileName;
            Console.WriteLine(path);

            List<uint> chainMap = this.volume.GetClusterChain(dirent);

            TryFileWrite(path, dirent, chainMap);
        }

        private void SaveDirectory(string path, DirectoryEntry dirent)
        {
            path = path + "\\" + dirent.FileName;
            Console.WriteLine(path);

            Directory.CreateDirectory(path);

            foreach (DirectoryEntry child in dirent.GetChildren())
            {
                Save(path, child);
            }

            TryDirectorySetTimestamps(path, dirent);
        }

        private void Save(string path, DirectoryEntry dirent)
        {
            if (dirent.IsDeleted())
            {
                path = path + "\\" + dirent.FileName;
                Console.WriteLine($"{path} failed to dump as it was deleted.");
                return;
            }

            //Console.WriteLine($"{path + dirent.GetFullPath()}");

            if (dirent.IsDirectory())
            {
                SaveDirectory(path, dirent);
            }
            else
            {
                SaveFile(path, dirent);
            }
        }

        private void Save(string path, List<DirectoryEntry> dirents)
        {
            foreach (var dirent in dirents)
            {
                Save(path, dirent);
            }
        }

        private void SaveAll(string path)
        {
            Save(path, this.volume.GetRoot());

            //foreach (var dirent in this.volume.GetRoot())
            //{
            //    dirent.Save(path);
            //}
        }

        private void saveAllToolStripMenuItem2_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog dialog = new FolderBrowserDialog();
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                SaveAll(dialog.SelectedPath);

                Console.WriteLine($"Finished saving files.");
            }
        }

        private void listSaveSelectedToolStripMenuItem_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog dialog = new FolderBrowserDialog();
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                foreach (ListViewItem selected in listView1.SelectedItems)
                {
                    NodeTag nodeTag = (NodeTag)selected.Tag;
                    switch (nodeTag.Type)
                    {
                        case NodeType.Dirent:
                            DirectoryEntry dirent = nodeTag.Tag as DirectoryEntry;

                            Save(dialog.SelectedPath, dirent);

                            //dirent.Save(dialog.SelectedPath);

                            break;
                    }
                }

                Console.WriteLine($"Finished saving files.");
            }
        }

        private void saveAllToolStripMenuItem_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog dialog = new FolderBrowserDialog();
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                SaveAll(dialog.SelectedPath);

                Console.WriteLine($"Finished saving files.");
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
    }
}
