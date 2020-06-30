using FATX.FileSystem;
using FATXTools.Database;
using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace FATXTools.Dialogs
{
    public partial class ClusterChainDialog : Form
    {
        Volume volume;
        DatabaseFile file;

        public List<uint> NewClusterChain { get; set; }

        public ClusterChainDialog(Volume volume, DatabaseFile file)
        {
            InitializeComponent();

            this.volume = volume;
            this.file = file;

            numericUpDown1.Minimum = 1;
            numericUpDown1.Maximum = volume.MaxClusters;

            InitializeClusterList(file.ClusterChain);
        }

        private void InitializeClusterList(List<uint> clusterChain)
        {
            foreach (var cluster in clusterChain)
            {
                AddCluster(cluster);
            }
        }

        private void AddCluster(uint cluster)
        {
            var address = volume.ClusterToPhysicalOffset(cluster);

            ListViewItem item = new ListViewItem(new string[] { $"0x{address.ToString("X")}", cluster.ToString() });

            item.Tag = cluster;

            listView1.Items.Add(item);
        }

        private void RemoveCluster(uint cluster)
        {
            foreach (ListViewItem item in listView1.Items)
            {
                if ((uint)item.Tag == cluster)
                {
                    listView1.Items.Remove(item);
                    break;
                }
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            var cluster = (uint)numericUpDown1.Value;

            AddCluster(cluster);
        }

        private void button4_Click(object sender, EventArgs e)
        {
            foreach (ListViewItem item in listView1.SelectedItems)
            {
                listView1.Items.Remove(item);
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            NewClusterChain = new List<uint>();

            foreach (ListViewItem item in listView1.Items)
            {
                NewClusterChain.Add((uint)item.Tag);
            }
        }
    }
}
