using System;
using System.Windows.Forms;
using System.IO;
using Microsoft.Win32.SafeHandles;
using FATXTools.Utilities;

namespace FATXTools.Dialogs
{
    public partial class DeviceSelectionDialog : Form
    {
        private string selectedDevice;

        public DeviceSelectionDialog(MainWindow main)
        {
            InitializeComponent();

            for (int i = 0; i < 24; i++)
            {
                string deviceName = String.Format(@"\\.\PhysicalDrive{0}", i);
                SafeFileHandle handle = WinApi.CreateFile(
                    deviceName,
                    FileAccess.Read,
                    FileShare.None,
                    IntPtr.Zero,
                    FileMode.Open,
                    0,
                    IntPtr.Zero
                    );

                if (handle.IsInvalid)
                {
                    continue;
                }

                var deviceItem = listView1.Items.Add(deviceName);
                deviceItem.SubItems.Add(FormatSize(WinApi.GetDiskCapactity(handle)));
                deviceItem.ImageIndex = 0;
                deviceItem.StateImageIndex = 0;

                handle.Close();
            }
        }

        static readonly string[] suffixes = { "Bytes", "KB", "MB", "GB", "TB", "PB" };
        public static string FormatSize(Int64 bytes)
        {
            int counter = 0;
            decimal number = (decimal)bytes;
            while (Math.Round(number / 1024) >= 1)
            {
                number = number / 1024;
                counter++;
            }
            return string.Format("{0:n1}{1}", number, suffixes[counter]);
        }

        public string SelectedDevice
        {
            get { return selectedDevice; }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            this.selectedDevice = listView1.SelectedItems[0].Text;
        }

        private void button1_Click(object sender, EventArgs e)
        {
        }
    }
}
