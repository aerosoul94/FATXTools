using System;
using System.Collections.Generic;
using System.Windows.Forms;

using FATXTools.Utilities;

namespace FATXTools.Dialogs
{
    public partial class DeviceSelectionDialog : Form
    {
        private string selectedDevice;

        public DeviceSelectionDialog()
        {
            InitializeComponent();

            List<WinApi.DeviceInfo> list = WinApi.GetDeviceList();

            for (var i = 0; i < list.Count; i++)
            {
                var device = list[i];
                var deviceItem = listView1.Items.Add(device.DeviceName);

                deviceItem.SubItems.Add(Utility.FormatBytes(device.Capacity));
                deviceItem.ImageIndex = 0;
                deviceItem.StateImageIndex = 0;
            }
        }

        public string SelectedDevice => selectedDevice;

        private void button2_Click(object sender, EventArgs e)
        {
            selectedDevice = listView1.SelectedItems[0].Text;
        }
    }
}
