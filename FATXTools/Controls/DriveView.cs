using FATX.Drive;
using FATX.FileSystem;
using FATXTools.Controls;
using FATXTools.Database;
using FATXTools.Utilities;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
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
        private XDrive drive;

        private string driveName;

        /// <summary>
        /// List of partitions in this drive.
        /// </summary>
        private List<PartitionView> partitionViews = new List<PartitionView>();

        public event EventHandler TaskStarted;

        public event EventHandler TaskCompleted;

        public event EventHandler<PartitionSelectedEventArgs> TabSelectionChanged;

        private DriveDatabase driveDatabase;

        public DriveView()
        {
            InitializeComponent();
        }

        public void AddDrive(string name, XDrive drive)
        {
            this.driveName = name;
            this.drive = drive;

            this.driveDatabase = new DriveDatabase(name, drive);
            this.driveDatabase.OnPartitionAdded += DriveDatabase_OnPartitionAdded;
            this.driveDatabase.OnPartitionRemoved += DriveDatabase_OnPartitionRemoved;

            this.partitionTabControl.MouseClick += PartitionTabControl_MouseClick;

            foreach (var partition in drive.Partitions)
            {
                AddPartition(partition);
            }

            // Fire SelectedIndexChanged event.
            SelectedIndexChanged();
        }

        private void DriveDatabase_OnPartitionRemoved(object sender, RemovePartitionEventArgs e)
        {
            var index = e.Index;
            partitionTabControl.TabPages.RemoveAt(index);
            partitionViews.RemoveAt(index);
        }

        private void PartitionTabControl_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                for (var i = 0; i < partitionTabControl.TabCount; i++)
                {
                    Rectangle r = partitionTabControl.GetTabRect(i);
                    if (r.Contains(e.Location))
                    {
                        partitionTabControl.SelectedIndex = i;
                        this.contextMenuStrip.Show(this.partitionTabControl, e.Location);
                        break;
                    }
                }
            }
        }

        private void DriveDatabase_OnPartitionAdded(object sender, AddPartitionEventArgs e)
        {
            AddPartition(e.Partition);
        }

        private void TaskRunner_TaskCompleted(object sender, EventArgs e)
        {
            TaskCompleted?.Invoke(sender, e);
        }

        private void TaskRunner_TaskStarted(object sender, EventArgs e)
        {
            TaskStarted?.Invoke(sender, e);
        }

        public void AddPartition(Partition partition)
        {
            var volume = partition.Volume;

            try
            {
                volume.Mount();

                Console.WriteLine($"Successfully mounted {volume.Name}");
            }
            catch (Exception e)
            {
                Console.WriteLine($"Failed to mount {volume.Name}: {e.Message}");
            }

            var page = new TabPage(volume.Name);
            var partitionDatabase = driveDatabase.AddPartition(volume);
            var partitionView = new PartitionView(volume, partitionDatabase);
            partitionView.Dock = DockStyle.Fill;
            page.Controls.Add(partitionView);
            partitionTabControl.TabPages.Add(page);
            partitionViews.Add(partitionView);
        }

        public XDrive GetDrive()
        {
            return drive;
        }

        public List<Volume> GetVolumes()
        {
            return partitionViews.Select(partitionView => partitionView.Volume).ToList();
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
            TabSelectionChanged?.Invoke(this, partitionTabControl.TabCount == 0 ? null : new PartitionSelectedEventArgs()
            {
                volume = partitionViews[partitionTabControl.SelectedIndex].Volume
            });
        }

        private void partitionTabControl_SelectedIndexChanged(object sender, EventArgs e)
        {
            SelectedIndexChanged();
        }

        private void ToolStripMenuItem1_Click(object sender, System.EventArgs e)
        {
            var dialogResult = MessageBox.Show("Are you sure you want to remove this partition?", "Are you sure?", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (dialogResult == DialogResult.Yes)
            {
                driveDatabase.RemovePartition(partitionTabControl.SelectedIndex);
            }
        }
    }
}
