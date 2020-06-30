using FATX;
using FATX.FileSystem;
using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace FATXTools.Dialogs
{
    public partial class PartitionManagerDialog : Form
    {
        private List<Volume> volumes;
        private DriveReader reader;

        public PartitionManagerDialog()
        {
            InitializeComponent();
        }

        public PartitionManagerDialog(DriveReader reader, List<Volume> volumes)
        {
            InitializeComponent();

            this.reader = reader;
            this.volumes = volumes;

            PopulateList(volumes);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            NewPartitionDialog dialog = new NewPartitionDialog();
            var dialogResult = dialog.ShowDialog();
            if (dialogResult == DialogResult.OK)
            {
                volumes.Add(new Volume(this.reader, dialog.PartitionName, dialog.PartitionOffset, dialog.PartitionLength));

                PopulateList(volumes);
            }
        }

        private void PopulateList(List<Volume> volumes)
        {
            listView1.Items.Clear();

            foreach (var volume in volumes)
            {
                ListViewItem item = new ListViewItem(volume.Name);
                item.SubItems.Add("0x" + volume.Offset.ToString("X"));
                item.SubItems.Add("0x" + volume.Length.ToString("X"));

                listView1.Items.Add(item);
            }
        }
    }
}
