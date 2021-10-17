using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

using FATX.Drive;
using FATX.FileSystem;

using FATXTools.Controls;
using FATXTools.Database;

namespace FATXTools
{
    public partial class DriveView : UserControl
    {
        /// <summary>
        /// Currently loaded drive.
        /// </summary>
        private XDrive _drive;

        /// <summary>
        /// List of partitions in this drive.
        /// </summary>
        private List<PartitionView> _partitionViews = new List<PartitionView>();

        /// <summary>
        /// The database model for this drive.
        /// </summary>
        private DriveDatabase _driveDatabase;

        /// <summary>
        /// This event fires when a new tab has been selected.
        /// </summary>
        public event EventHandler<PartitionSelectedEventArgs> TabSelectionChanged;

        /// <summary>
        /// Get the drive for this view.
        /// </summary>
        public XDrive Drive => _drive;

        /// <summary>
        /// Get all file systems for this drive.
        /// </summary>
        public List<Volume> Volumes => _partitionViews.Select(partitionView => partitionView.Volume).ToList();

        public DriveView()
        {
            InitializeComponent();
        }

        public void SetDrive(string name, XDrive drive)
        {
            _drive = drive;

            _driveDatabase = new DriveDatabase(name, drive);
            _driveDatabase.OnPartitionAdded += DriveDatabase_OnPartitionAdded;
            _driveDatabase.OnPartitionRemoved += DriveDatabase_OnPartitionRemoved;

            foreach (var partition in drive.Partitions)
                AddPartition(partition);

            // Fire SelectedPartitionChanged event.
            SelectedPartitionChanged();
        }

        public void AddPartition(Partition partition)
        {
            var volume = partition.Volume;

            // Try to mount it as a FATX file system.
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
            var partitionDatabase = _driveDatabase.AddPartition(volume);
            var partitionView = new PartitionView(volume, partitionDatabase)
            {
                Dock = DockStyle.Fill
            };
            page.Controls.Add(partitionView);
            partitionTabControl.TabPages.Add(page);
            _partitionViews.Add(partitionView);
        }

        /// <summary>
        /// Save the database to the specified path.
        /// </summary>
        /// <param name="path">The path to save the database to.</param>
        public void Save(string path)
        {
            _driveDatabase.Save(path);
        }

        /// <summary>
        /// Load a database from the specified path.
        /// </summary>
        /// <param name="path">The path to load the database from.</param>
        public void LoadFromJson(string path)
        {
            _driveDatabase.LoadFromJson(path);
        }

        private void SelectedPartitionChanged()
        {
            TabSelectionChanged?.Invoke(this, partitionTabControl.TabCount == 0 ? null : new PartitionSelectedEventArgs()
            {
                volume = _partitionViews[partitionTabControl.SelectedIndex].Volume
            });
        }

        #region Drive Database Events
        private void DriveDatabase_OnPartitionRemoved(object sender, RemovePartitionEventArgs e)
        {
            var index = e.Index;
            partitionTabControl.TabPages.RemoveAt(index);
            _partitionViews.RemoveAt(index);
        }

        private void DriveDatabase_OnPartitionAdded(object sender, AddPartitionEventArgs e)
        {
            AddPartition(e.Partition);
        }
        #endregion

        #region Partition Tab Control Events
        private void partitionTabControl_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                for (var i = 0; i < partitionTabControl.TabCount; i++)
                {
                    Rectangle r = partitionTabControl.GetTabRect(i);
                    if (r.Contains(e.Location))
                    {
                        partitionTabControl.SelectedIndex = i;
                        contextMenuStrip.Show(partitionTabControl, e.Location);
                        break;
                    }
                }
            }
        }

        private void partitionTabControl_SelectedIndexChanged(object sender, EventArgs e)
        {
            SelectedPartitionChanged();
        }

        private void ToolStripMenuItem1_Click(object sender, System.EventArgs e)
        {
            var dialogResult = MessageBox.Show("Are you sure you want to remove this partition?", "Are you sure?", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (dialogResult == DialogResult.Yes)
                _driveDatabase.RemovePartition(partitionTabControl.SelectedIndex);
        }
        #endregion
    }
}
