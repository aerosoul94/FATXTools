using System;
using System.Collections.Generic;
using System.Windows.Forms;

using FATX.FileSystem;

using FATXTools.Database;

namespace FATXTools.Dialogs
{
    public partial class ClusterChainDialog : Form
    {
        Volume _volume;
        DatabaseFile _file;

        public List<uint> NewClusterChain { get; set; }

        public ClusterChainDialog(Volume volume, DatabaseFile file)
        {
            InitializeComponent();

            _volume = volume;
            _file = file;

            numericUpDown1.Minimum = 1;
            numericUpDown1.Maximum = volume.MaxClusters;

            InitializeClusterList(file.ClusterChain);
        }

        private void InitializeClusterList(List<uint> clusterChain)
        {
            listView1.BeginUpdate();

            foreach (var cluster in clusterChain)
            {
                AddCluster(cluster);
            }

            listView1.EndUpdate();
        }

        private void AddCluster(uint cluster)
        {
            var address = _volume.ClusterReader.ClusterToPhysicalOffset(cluster);

            ListViewItem item = new ListViewItem(new string[] { $"0x{address.ToString("X")}", cluster.ToString() })
            {
                Tag = cluster
            };

            listView1.Items.Add(item);
        }

        private void button3_Click(object sender, EventArgs e)
        {
            var cluster = (uint)numericUpDown1.Value;

            AddCluster(cluster);
        }

        private void button4_Click(object sender, EventArgs e)
        {
            listView1.BeginUpdate();

            foreach (ListViewItem item in listView1.SelectedItems)
            {
                listView1.Items.Remove(item);
            }

            listView1.EndUpdate();
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
