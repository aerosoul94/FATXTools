using System;
using System.Collections.Generic;
using System.Windows.Forms;
using FATX;
using FATX.Analyzers.Signatures;

namespace FATXTools
{
    public partial class CarverResults : UserControl
    {
        private FileCarver _analyzer;

        public CarverResults(FileCarver analyzer)
        {
            InitializeComponent();

            this._analyzer = analyzer;
            PopulateResultsList(analyzer.GetCarvedFiles());
        }

        public void PopulateResultsList(List<FileSignature> results)
        {
            var i = 0;
            foreach (var result in results)
            {
                var item = listView1.Items.Add(i.ToString());
                item.SubItems.Add(result.FileName);
                item.SubItems.Add(String.Format("0x{0:X}", result.Offset));
                item.SubItems.Add(String.Format("0x{0:X}", result.FileSize));
                item.Tag = result;
                i++;
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
                        _analyzer.Dump((FileSignature)item.Tag, fbd.SelectedPath);
                    }
                }
            }
        }
    }
}
