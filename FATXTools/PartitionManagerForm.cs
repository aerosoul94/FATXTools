using FATX;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FATXTools
{
    public partial class PartitionManagerForm : Form
    {
        private List<Volume> volumes;
        private DriveReader reader;

        public PartitionManagerForm()
        {
            InitializeComponent();
        }

        public PartitionManagerForm(DriveReader reader, List<Volume> volumes)
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
