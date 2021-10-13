using System;
using System.IO;
using System.Security.Principal;
using System.Windows.Forms;

using FATX.Devices;
using FATX.Drive;
using FATX.FileSystem;

using FATXTools.Controls;
using FATXTools.Dialogs;
using FATXTools.Utilities;

using Microsoft.Win32.SafeHandles;

namespace FATXTools.Forms
{
    public partial class MainWindow : Form
    {
        private DriveView driveView;

        private const string ApplicationTitle = "FATX-Recover";

        public MainWindow()
        {
            InitializeComponent();

            this.Text = ApplicationTitle;

            SetDatabaseOptionsEnabled(false);

            Console.SetOut(new LogWriter(this.textBox1));
            Console.WriteLine("--------------------------------");
            Console.WriteLine("FATX-Tools v0.3");
            Console.WriteLine("--------------------------------");
        }

        private void OpenDiskImage(string path)
        {
            CreateNewDriveView(path);

            string fileName = Path.GetFileName(path);

            RawImage disk = new RawImage(path);
            driveView.SetDrive(fileName, disk.Drive);

            SetDatabaseOptionsEnabled(true);
        }

        private void OpenDisk(string device)
        {
            CreateNewDriveView(device);

            SafeFileHandle handle = WinApi.CreateFile(device, FileAccess.Read, FileShare.None, IntPtr.Zero,
                FileMode.Open, 0, IntPtr.Zero);

            long length = WinApi.GetDiskCapactity(handle);
            long sectorLength = WinApi.GetSectorSize(handle);

            PhysicalDisk disk = new PhysicalDisk(handle, length, sectorLength);
            driveView.SetDrive(device, disk.Drive);

            SetDatabaseOptionsEnabled(true);
        }

        private void CreateNewDriveView(string path)
        {
            // Update the title to the newly loaded drive's file name.
            this.Text = $"{ApplicationTitle} - {Path.GetFileName(path)}";

            // Destroy the current drive view
            splitContainer1.Panel1.Controls.Remove(driveView);

            // Create a new view for this drive
            driveView = new DriveView();
            driveView.Dock = DockStyle.Fill;
            driveView.TabSelectionChanged += DriveView_TabSelectionChanged;

            TaskRunner.Instance.OnTaskStarted += MainWindow_OnTaskEnded;
            TaskRunner.Instance.OnTaskEnded += MainWindow_OnTaskStarted;

            // Add the view to the panel
            splitContainer1.Panel1.Controls.Add(driveView);
        }

        private void SetDatabaseOptionsEnabled(bool enabled)
        {
            loadToolStripMenuItem.Enabled = enabled;
            saveToolStripMenuItem.Enabled = enabled;

            addPartitionToolStripMenuItem.Enabled = enabled;
            //searchForPartitionsToolStripMenuItem.Enabled = enabled;
            //managePartitionsToolStripMenuItem.Enabled = enabled;
        }

        private void SetOpenOptionsEnabled(bool enabled)
        {
            openImageToolStripMenuItem.Enabled = enabled;
            openDeviceToolStripMenuItem.Enabled = enabled;
        }

        /// <summary>
        /// Update the status strip with information from the volume.
        /// </summary>
        /// <param name="volume">The volume containing the information.</param>
        private void UpdateStatusStrip(Volume volume)
        {
            statusStrip1.Items.Clear();

            if (volume.Mounted)
            {
                var usedSpace = volume.GetUsedSpace();
                var freeSpace = volume.GetFreeSpace();
                var totalSpace = volume.GetTotalSpace();

                statusStrip1.Items.Add($"Volume Offset: 0x{volume.Offset:X}");
                statusStrip1.Items.Add($"Volume Length: 0x{volume.Length:X}");
                statusStrip1.Items.Add($"Used Space: {Utility.FormatBytes(usedSpace)}");
                statusStrip1.Items.Add($"Free Space: {Utility.FormatBytes(freeSpace)}");
                statusStrip1.Items.Add($"Total Space: {Utility.FormatBytes(totalSpace)}");
            }
        }

        #region Main Window Events
        private void MainWindow_OnTaskStarted(object sender, EventArgs e)
        {
            SetOpenOptionsEnabled(true);
            SetDatabaseOptionsEnabled(true);
        }

        private void MainWindow_OnTaskEnded(object sender, EventArgs e)
        {
            SetOpenOptionsEnabled(false);
            SetDatabaseOptionsEnabled(false);
        }

        private void MainWindow_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                // Only 1 file is allowed.
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (files.Length > 1)
                {
                    e.Effect = DragDropEffects.None;
                }
                else
                {
                    e.Effect = DragDropEffects.Link;
                }
            }
            else
            {
                // Not a file.
                e.Effect = DragDropEffects.None;
            }
        }

        private void MainWindow_DragDrop(object sender, DragEventArgs e)
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (files.Length > 1)
            {
                MessageBox.Show("You may only drop one file!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            else
            {
                string path = files[0];
                OpenDiskImage(path);
            }
        }

        private void MainWindow_FormClosing(object sender, FormClosingEventArgs e)
        {
            // TODO: For any partition, if any analysis was made, then we should ask.
            // TODO: Add setting for auto-saving (maybe at run-time or while closing)
            if (driveView != null)
            {
                var dialogResult = MessageBox.Show("Would you like to save progress before closing?", "Save Progress", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Warning);
                if (dialogResult == DialogResult.Yes)
                {
                    SaveFileDialog saveFileDialog = new SaveFileDialog()
                    {
                        Filter = "JSON File (*.json)|*.json"
                    };

                    if (saveFileDialog.ShowDialog() == DialogResult.OK)
                    {
                        driveView.Save(saveFileDialog.FileName);

                        Console.WriteLine($"Finished saving database: {saveFileDialog.FileName}");
                    }
                    else
                    {
                        // User may have accidentally cancelled? Maybe try again?
                    }
                }
                else if (dialogResult == DialogResult.Cancel)
                {
                    e.Cancel = true;
                    return;
                }
            }

            // TODO: handle closing dialogs
            e.Cancel = false;
        }
        #endregion

        #region Drive View Events
        private void DriveView_TabSelectionChanged(object sender, PartitionSelectedEventArgs e)
        {
            if (e == null)
            {
                statusStrip1.Items.Clear();
            }
            else
            {
                var volume = e.volume;

                UpdateStatusStrip(volume);
            }
        }
        #endregion

        #region File Menu Commands
        private void openImageToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                OpenDiskImage(ofd.FileName);
            }
        }

        private void openDeviceToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var isAdmin = new WindowsPrincipal(WindowsIdentity.GetCurrent())
                .IsInRole(WindowsBuiltInRole.Administrator);

            if (!isAdmin)
            {
                MessageBox.Show("You must re-run this program with Administrator privileges\n" +
                                "in order to read from physical drives.",
                                "Cannot perform operation", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            DeviceSelectionDialog ds = new DeviceSelectionDialog();
            if (ds.ShowDialog() == DialogResult.OK)
            {
                OpenDisk(ds.SelectedDevice);
            }
        }

        private void settingsToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            SettingsDialog settings = new SettingsDialog();
            if (settings.ShowDialog() == DialogResult.OK)
            {
                Properties.Settings.Default.FileCarverInterval = settings.FileCarverInterval;
                Properties.Settings.Default.LogFile = settings.LogFile;

                Properties.Settings.Default.Save();
            }
        }

        private void saveToJSONToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog()
            {
                Filter = "JSON File (*.json)|*.json"
            };

            if (saveFileDialog.ShowDialog() == DialogResult.OK)
            {
                driveView.Save(saveFileDialog.FileName);

                Console.WriteLine($"Finished saving database: {saveFileDialog.FileName}");
            }
        }

        private void loadFromJSONToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog()
            {
                Filter = "JSON File (*.json)|*.json"
            };

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                var dialogResult = MessageBox.Show($"Loading a database will overwrite current analysis progress.\n"
                    + $"Are you sure you want to load \'{Path.GetFileName(openFileDialog.FileName)}\'?",
                    "Load File", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                if (dialogResult == DialogResult.Yes)
                {
                    driveView.LoadFromJson(openFileDialog.FileName);

                    Console.WriteLine($"Finished loading database: {openFileDialog.FileName}");
                }
            }
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }
        #endregion

        #region Drive Menu Commands
        private void managePartitionsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (driveView != null)
            {
                //PartitionManagerForm partitionManagerForm = new PartitionManagerForm(driveView.GetDrive(), driveView.GetDrive().GetPartitions());
                //partitionManagerForm.ShowDialog();
            }
        }

        private void addPartitionToolStripMenuItem_Click(object sender, EventArgs e)
        {
            NewPartitionDialog partitionDialog = new NewPartitionDialog();
            var dialogResult = partitionDialog.ShowDialog();
            if (dialogResult == DialogResult.OK)
            {
                var drive = driveView.Drive;

                var partition = drive.AddPartition(
                    partitionDialog.PartitionName, 
                    partitionDialog.PartitionOffset, 
                    partitionDialog.PartitionLength
                );

                partition.Volume = new Volume(partition, drive is XboxDrive ? Platform.Xbox : Platform.X360);

                driveView.AddPartition(partition);
            }
        }
        #endregion

        #region About Menu Commands
        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show(
                "Developed by aerosoul94\n" +
                "Source code: https://github.com/aerosoul94/FATXTools\n" +
                "Please report any bugs\n",
                "About", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        #endregion
    }
}
