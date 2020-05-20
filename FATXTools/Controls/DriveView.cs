using System;
using System.Collections.Generic;
using System.Windows.Forms;
using FATX;
using FATXTools.Controls;

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

        public event EventHandler TabSelectionChanged;

        public void AddDrive(string name, DriveReader drive)
        {
            this.drive = drive;

            foreach (var volume in drive.GetPartitions())
            {
                AddPartition(volume);
            }

            // Fire SelectedIndexChanged event.
            SelectedIndexChanged();
        }

        public void AddPartition(Volume volume)
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
            partitionTabControl.TabPages.Add(page);
        }

        public DriveReader GetDrive()
        {
            return drive;
        }
        
        private void SelectedIndexChanged()
        {
            TabSelectionChanged?.Invoke(this, new PartitionSelectedEventArgs()
            {
                volume = partitionList[partitionTabControl.SelectedIndex]
            });
        }

        private void partitionTabControl_SelectedIndexChanged(object sender, EventArgs e)
        {
            SelectedIndexChanged();
        }
    }
}
