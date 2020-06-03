using System;
using System.Windows.Forms;
using System.Security.Principal;
using System.IO;
using System.Text;
using Microsoft.Win32.SafeHandles;
using FATX;
using FATXTools.DiskTypes;
using FATXTools.Controls;
using FATXTools.Utilities;

namespace FATXTools
{
    public partial class MainWindow : Form
    {
        private DriveView driveView;

        private const string ApplicationTitle = "FATX-Recover";

        public MainWindow()
        {
            InitializeComponent();

            this.Text = ApplicationTitle;

            Console.SetOut(new LogWriter(this.textBox1));
            Console.WriteLine("--------------------------------");
            Console.WriteLine("FATX-Tools v0.2");
            Console.WriteLine("--------------------------------");
        }

        public class LogWriter : TextWriter
        {
            private TextBox textBox;
            private delegate void SafeCallDelegate(string text);
            public LogWriter(TextBox textBox)
            {
                this.textBox = textBox;
            }

            public override void Write(char value)
            {
                textBox.Text += value;
            }

            public override void Write(string value)
            {
                textBox.AppendText(value);
            }

            public override void WriteLine()
            {
                textBox.AppendText(NewLine);
            }

            public override void WriteLine(string value)
            {
                if (textBox.InvokeRequired)
                {
                    var d = new SafeCallDelegate(WriteLine);
                    textBox.BeginInvoke(d, new object[] { value });
                }
                else
                {
                    textBox.AppendText(value + NewLine);
                }
            }

            public override Encoding Encoding
            {
                get { return Encoding.ASCII; }
            }
        }

        private void CreateNewDriveView(string path)
        {
            this.Text = $"{ApplicationTitle} - {Path.GetFileName(path)}";

            // Destroy the current drive view
            splitContainer1.Panel1.Controls.Remove(driveView);

            // Create a new view for this drive
            driveView = new DriveView();
            driveView.Dock = DockStyle.Fill;
            driveView.TabSelectionChanged += DriveView_TabSelectionChanged;

            // Add the view to the panel
            splitContainer1.Panel1.Controls.Add(driveView);
        }

        private void DriveView_TabSelectionChanged(object sender, EventArgs e)
        {
            PartitionSelectedEventArgs eventArgs = (PartitionSelectedEventArgs)e;
            var volume = eventArgs.volume;

            var usedSpace = volume.GetUsedSpace();
            var freeSpace = volume.GetFreeSpace();
            var totalSpace = volume.GetTotalSpace();

            statusStrip1.Items.Clear();
            statusStrip1.Items.Add($"Volume Offset: 0x{volume.Offset:X}");
            statusStrip1.Items.Add($"Volume Length: 0x{volume.Length:X}");
            statusStrip1.Items.Add($"Used Space: {Utility.FormatBytes(usedSpace)}");
            statusStrip1.Items.Add($"Free Space: {Utility.FormatBytes(freeSpace)}");
            statusStrip1.Items.Add($"Total Space: {Utility.FormatBytes(totalSpace)}");
        }

        private void OpenDiskImage(string path)
        {
            CreateNewDriveView(path);

            string fileName = Path.GetFileName(path);

            RawImage rawImage = new RawImage(path);
            driveView.AddDrive(fileName, rawImage);
        }

        private void OpenDisk(string device)
        {
            CreateNewDriveView(device);

            SafeFileHandle handle = DeviceSelector.CreateFile(device,
                       FileAccess.Read,
                       FileShare.None,
                       IntPtr.Zero,
                       FileMode.Open,
                       0, IntPtr.Zero);
            long length = DeviceSelector.GetDiskCapactity(handle);
            PhysicalDisk drive = new PhysicalDisk(handle, length);
            driveView.AddDrive(device, drive);
        }

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
                MessageBox.Show("You must re-run this program with Administrator priveleges\n" +
                                "in order to read from physical drives.",
                                "Cannot perform operation", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            DeviceSelector ds = new DeviceSelector(this);
            if (ds.ShowDialog() == DialogResult.OK)
            {
                OpenDisk(ds.SelectedDevice);
            }
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show(
                "Developed by aerosoul94\n" +
                "Source code: https://github.com/aerosoul94/FATXTools\n" +
                "Please report any bugs\n",
                "About", MessageBoxButtons.OK, MessageBoxIcon.Information);
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

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

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
                driveView.AddPartition(new Volume(driveView.GetDrive(),
                    partitionDialog.PartitionName, 
                    partitionDialog.PartitionOffset, 
                    partitionDialog.PartitionLength));
            }
        }

        private void settingsToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            SettingsForm settings = new SettingsForm();
            if (settings.ShowDialog() == DialogResult.OK)
            {
                Properties.Settings.Default.FileCarverInterval = settings.FileCarverInterval;
                Properties.Settings.Default.LogFile = settings.LogFile;

                Properties.Settings.Default.Save();
            }
        }
    }
}
