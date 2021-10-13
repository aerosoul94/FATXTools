using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Forms;

using FATX.Analyzers;
using FATX.FileSystem;

using FATXTools.Dialogs;
using FATXTools.Tasks;
using FATXTools.Utilities;

namespace FATXTools
{
    public partial class CarverResults : UserControl
    {
        private List<CarvedFile> _carvedFiles;
        private Volume _volume;

        public CarverResults(Volume volume, List<CarvedFile> files)
        {
            InitializeComponent();

            this._carvedFiles = files;
            this._volume = volume;

            PopulateResultsList(_carvedFiles);
        }

        public void PopulateResultsList(List<CarvedFile> results)
        {
            var i = 1;

            var baseOffset = _volume.Offset + _volume.FileAreaByteOffset;

            foreach (var result in results)
            {
                var item = listView1.Items.Add(i.ToString());
                item.SubItems.Add(result.FileName);
                item.SubItems.Add(String.Format("0x{0:X}", result.Offset + baseOffset));
                item.SubItems.Add(String.Format("0x{0:X}", result.FileSize));
                item.Tag = result;
                i++;
            }
        }

        private async Task RunRecoverAllTask(string path, List<CarvedFile> files)
        {
            var options = new TaskDialogOptions() { Title = "Save Files" };

            await TaskRunner.Instance.RunTaskAsync(ParentForm, options,
                CarverTask.RunSaveAllTask(_volume, path, files)
            );
        }

        private async void recoverFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (listView1.SelectedItems.Count == 0)
                return;

            using (var fbd = new FolderBrowserDialog())
            {
                if (fbd.ShowDialog() == DialogResult.OK)
                {
                    List<CarvedFile> files = new List<CarvedFile>();
                    foreach (ListViewItem item in listView1.SelectedItems)
                        files.Add((CarvedFile)item.Tag);

                    await RunRecoverAllTask(fbd.SelectedPath, files);
                }
            }
        }

        private async void recoverAllToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (var fbd = new FolderBrowserDialog())
            {
                if (fbd.ShowDialog() == DialogResult.OK)
                {
                    List<CarvedFile> files = new List<CarvedFile>();
                    foreach (ListViewItem item in listView1.Items)
                        files.Add((CarvedFile)item.Tag);

                    await RunRecoverAllTask(fbd.SelectedPath, files);
                }
            }
        }
    }
}
