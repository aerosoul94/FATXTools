using FATX.Analyzers;
using FATX.Analyzers.Signatures;
using FATX.FileSystem;
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
        private FileCarver _analyzer;
        private Volume _volume;
        private TaskRunner taskRunner;

        public CarverResults(FileCarver analyzer, TaskRunner taskRunner)
        {
            InitializeComponent();

            this._analyzer = analyzer;
            this._volume = analyzer.GetVolume();
            this.taskRunner = taskRunner;
            PopulateResultsList(analyzer.GetCarvedFiles());
        }

        public void PopulateResultsList(List<FileSignature> results)
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

        private void SaveFile(FileSignature signature, string path)
        {
            const int bufsize = 0x100000;
            var remains = signature.FileSize;
            _volume.SeekFileArea(signature.Offset);

            path = path + "/" + signature.FileName;
            var uniquePath = Utility.UniqueFileName(path);
            using (FileStream file = new FileStream(uniquePath, FileMode.Create))
            {
                while (remains > 0)
                {
                    var read = Math.Min(remains, bufsize);
                    remains -= read;
                    byte[] buf = new byte[read];
                    _volume.GetReader().Read(buf, (int)read);
                    file.Write(buf, 0, (int)read);
                }
            }
        }

        private void recoverFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (var fbd = new FolderBrowserDialog())
            {
                if (fbd.ShowDialog() == DialogResult.OK)
                {
                    foreach (ListViewItem item in listView1.SelectedItems)
                    {
                        SaveFile((FileSignature)item.Tag, fbd.SelectedPath);
                    }
                }
            }
        }

        private async void recoverAllToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (var fbd = new FolderBrowserDialog())
            {
                if (fbd.ShowDialog() == DialogResult.OK)
                {
                    var numFiles = listView1.Items.Count;
                    string currentFile = string.Empty;
                    this.taskRunner.Maximum = listView1.Items.Count;
                    this.taskRunner.Interval = 1;

                    List<FileSignature> signatures = new List<FileSignature>();
                    foreach (ListViewItem item in listView1.Items)
                    {
                        signatures.Add((FileSignature)item.Tag);
                    }

                    await taskRunner.RunTaskAsync("Save File",
                        (CancellationToken cancellationToken, IProgress<int> progress) =>
                        {
                            int p = 1;
                            foreach (var signature in signatures)
                            {
                                currentFile = signature.FileName;

                                SaveFile(signature, fbd.SelectedPath);

                                progress.Report(p++);
                            }
                        },
                        (int progress) =>
                        {
                            taskRunner.UpdateLabel($"{progress}/{numFiles}: {currentFile}");
                            taskRunner.UpdateProgress(progress);
                        },
                        () =>
                        {
                            Console.WriteLine("Finished saving files.");
                        });
                }
            }
        }
    }
}
