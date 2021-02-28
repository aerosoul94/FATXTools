using FATX.Analyzers;
using FATX.Analyzers.Signatures;
using FATX.FileSystem;
using FATXTools.Dialogs;
using FATXTools.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Windows.Forms;

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

        private void SaveFile(CarvedFile signature, string path)
        {
            const int bufsize = 0x100000;
            var remains = signature.FileSize;
            _volume.FileAreaStream.Seek(signature.Offset, SeekOrigin.Begin);

            path = path + "/" + signature.FileName;
            var uniquePath = Utility.UniqueFileName(path);
            using (FileStream file = new FileStream(uniquePath, FileMode.Create))
            {
                while (remains > 0)
                {
                    var read = Math.Min(remains, bufsize);
                    remains -= read;
                    byte[] buf = new byte[read];
                    _volume.FileAreaStream.Read(buf, 0, (int)read);
                    file.Write(buf, 0, (int)read);
                }
            }
        }

        private void recoverFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (listView1.SelectedItems.Count == 0)
                return;

            using (var fbd = new FolderBrowserDialog())
            {
                if (fbd.ShowDialog() == DialogResult.OK)
                {
                    foreach (ListViewItem item in listView1.SelectedItems)
                    {
                        SaveFile((CarvedFile)item.Tag, fbd.SelectedPath);
                    }
                }
            }
        }

        private Action<CancellationToken, IProgress<(int, string)>> RunRecoverAllTask(string path, List<CarvedFile> files)
        {
            return (cancellationToken, progress) =>
            {
                var i = 1;
                var count = files.Count;

                foreach (var file in files)
                {
                    SaveFile(file, path);

                    var percent = (int)(((float)i / (float)count) * 100);
                    progress.Report((percent, $"{i}/{count}: {file.FileName}"));
                }
            };
        }

        private async void recoverAllToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (var fbd = new FolderBrowserDialog())
            {
                if (fbd.ShowDialog() == DialogResult.OK)
                {
                    List<CarvedFile> files = new List<CarvedFile>();
                    foreach (ListViewItem item in listView1.Items)
                    {
                        files.Add((CarvedFile)item.Tag);
                    }

                    var options = new TaskDialogOptions() { Title = "Save File" };

                    await TaskRunner.Instance.RunTaskAsync(ParentForm, options, RunRecoverAllTask(fbd.SelectedPath, files));
                }
            }
        }
    }
}
