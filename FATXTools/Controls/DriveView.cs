using FATX;
using FATX.FileSystem;
using FATXTools.Controls;
using FATXTools.Database;
using FATXTools.Utilities;
using System;
using System.Collections.Generic;
using System.Windows.Forms;

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

        private string driveName;

        /// <summary>
        /// List of partitions in this drive.
        /// </summary>
        private List<Volume> partitionList = new List<Volume>();

        private List<PartitionView> partitionViews = new List<PartitionView>();

        private TaskRunner taskRunner;

        public event EventHandler TaskStarted;

        public event EventHandler TaskCompleted;

        public event EventHandler TabSelectionChanged;

        private DriveDatabase driveDatabase;

        public DriveView()
        {
            InitializeComponent();
        }

        public void AddDrive(string name, DriveReader drive)
        {
            this.driveName = name;
            this.drive = drive;

            this.driveDatabase = new DriveDatabase(name);

            // Single task runner for this drive
            // Currently only one task will be allowed to operate on a drive to avoid race conditions.
            this.taskRunner = new TaskRunner(this.ParentForm);
            this.taskRunner.TaskStarted += TaskRunner_TaskStarted;
            this.taskRunner.TaskCompleted += TaskRunner_TaskCompleted;

            foreach (var volume in drive.Partitions)
            {
                AddPartition(volume);
            }

            // Fire SelectedIndexChanged event.
            SelectedIndexChanged();
        }

        private void TaskRunner_TaskCompleted(object sender, EventArgs e)
        {
            TaskCompleted?.Invoke(sender, e);
        }

        private void TaskRunner_TaskStarted(object sender, EventArgs e)
        {
            TaskStarted?.Invoke(sender, e);
        }

        public void AddPartition(Volume volume)
        {
            try
            {
                volume.Mount();

                Console.WriteLine($"Successfully mounted {volume.Name}");
            }
            catch (Exception e)
            {
                Console.WriteLine($"Failed to mount {volume.Name}: {e.Message}");
            }

            partitionList.Add(volume);

            var page = new TabPage(volume.Name);
            var partitionDatabase = driveDatabase.AddPartition(volume);
            var partitionView = new PartitionView(taskRunner, volume, partitionDatabase);
            partitionView.Dock = DockStyle.Fill;
            page.Controls.Add(partitionView);
            partitionTabControl.TabPages.Add(page);
            partitionViews.Add(partitionView);
        }

        public DriveReader GetDrive()
        {
            return drive;
        }

        public void Save(string path)
        {
            driveDatabase.Save(path);
        }

        public void LoadFromJson(string path)
        {
            driveDatabase.LoadFromJson(path);
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
