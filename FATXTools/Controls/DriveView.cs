using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using FATX;

namespace FATXTools
{
    public partial class DriveView : UserControl
    {
        public DriveView()
        {
            InitializeComponent();
        }

        /// <summary>
        /// List of loaded drives.
        /// </summary>
        //private List<DriveReader> driveList = new List<DriveReader>();

        /// <summary>
        /// Currently loaded drive.
        /// </summary>
        private DriveReader drive;

        /// <summary>
        /// List of partitions in this drive.
        /// </summary>
        private List<Volume> partitionList = new List<Volume>();

        public void AddDrive(string name, DriveReader drive)
        {
            foreach (var volume in drive.GetPartitions())
            {
                try
                {
                    volume.Mount();

                    Console.WriteLine($"Successfuly mounted {volume.Name}");
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Failed to mount {volume.Name}: {e.Message}");
                }

                partitionList.Add(volume);

                var page = new TabPage(volume.Name);
                var partitionView = new PartitionView(volume);
                partitionView.Dock = DockStyle.Fill;
                page.Controls.Add(partitionView);
                driveControl.TabPages.Add(page);
            }
            
        }
    }
}
