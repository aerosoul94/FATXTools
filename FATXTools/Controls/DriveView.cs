using System;
using System.Collections.Generic;
using System.Windows.Forms;
using FATX;
using FATXTools.Controls;
using FATXTools.Utility;

namespace FATXTools
{
    public partial class DriveView : UserControl
    {
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

        private TaskRunner taskRunner;

        public event EventHandler TabSelectionChanged;

        public DriveView()
        {
            InitializeComponent();
        }

        public void AddDrive(string name, DriveReader drive)
        {
            this.drive = drive;

            // Single task runner for this drive
            // Currently only one task will be allowed to operate on a drive to avoid race conditions.
            this.taskRunner = new TaskRunner(this.ParentForm);

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
            var partitionView = new PartitionView(taskRunner, volume);
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
