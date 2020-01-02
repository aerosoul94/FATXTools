using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Management;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace FATXTools
{
    public partial class DeviceSelector : Form
    {
        private string selectedDevice;

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern SafeFileHandle CreateFile(
            string FileName,
            FileAccess DesiredAccess,
            FileShare ShareMode,
            IntPtr SecurityAttributes,
            FileMode CreationDisposition,
            int FlagsAndAttributes,
            IntPtr Template);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool DeviceIoControl(
            SafeFileHandle hDevice,
            uint dwIoControlCode,
            [MarshalAs(UnmanagedType.AsAny)]
            [Out] object lpInBuffer,
            int nInBufferSize,
            [MarshalAs(UnmanagedType.AsAny)]
            [Out] object lpOutBuffer,
            int nOutBufferSize,
            ref int pBytesReturned,
            IntPtr lpOverlapped
            );

        public DeviceSelector(MainWindow main)
        {
            InitializeComponent();

            for (int i = 0; i < 24; i++)
            {
                string deviceName = String.Format(@"\\.\PhysicalDrive{0}", i);
                SafeFileHandle handle = CreateFile(
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
                deviceItem.SubItems.Add(FormatSize(GetDiskCapactity(handle)));
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

        public static long GetDiskCapactity(SafeFileHandle diskHandle)
        {
            byte[] sizeBytes = new byte[8];
            int bytesRet = sizeBytes.Length;
            if (!DeviceIoControl(diskHandle, 0x00000007405C, null, 0, sizeBytes, bytesRet, ref bytesRet, IntPtr.Zero))
            {
                throw new Exception("Failed to get disk size!");
            }
            return BitConverter.ToInt64(sizeBytes, 0);
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
