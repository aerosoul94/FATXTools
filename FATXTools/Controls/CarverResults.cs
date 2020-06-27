using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using FATX;
using FATX.Analyzers.Signatures;
using FATXTools.Utilities;

namespace FATXTools
{
    public partial class CarverResults : UserControl
    {
        private FileCarver _analyzer;
        private Volume _volume;

        public CarverResults(FileCarver analyzer)
        {
            InitializeComponent();

            this._analyzer = analyzer;
            this._volume = analyzer.GetVolume();
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
    }
}
