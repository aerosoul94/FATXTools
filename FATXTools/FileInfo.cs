using System;
using System.Windows.Forms;
using FATX;

namespace FATXTools
{
    public partial class FileInfo : Form
    {
        public FileInfo(DirectoryEntry dirent)
        {
            InitializeComponent();

            listView1.Items.Add("Name").SubItems.Add(dirent.FileName);
            listView1.Items.Add("Size in bytes").SubItems.Add(dirent.FileSize.ToString());
            listView1.Items.Add("First Cluster").SubItems.Add(dirent.FirstCluster.ToString());
            listView1.Items.Add("First Cluster Offset").SubItems.Add("0x" + 
                dirent.GetVolume().ClusterToPhysicalOffset(dirent.FirstCluster).ToString("x"));
            listView1.Items.Add("Attributes").SubItems.Add(FormatAttributes(dirent.FileAttributes));

            DateTime creationTime = new DateTime(dirent.CreationTime.Year,
                dirent.CreationTime.Month, dirent.CreationTime.Day,
                dirent.CreationTime.Hour, dirent.CreationTime.Minute,
                dirent.CreationTime.Second);
            DateTime lastWriteTime = new DateTime(dirent.LastWriteTime.Year,
                dirent.LastWriteTime.Month, dirent.LastWriteTime.Day,
                dirent.LastWriteTime.Hour, dirent.LastWriteTime.Minute,
                dirent.LastWriteTime.Second);
            DateTime lastAccessTime = new DateTime(dirent.LastAccessTime.Year,
                dirent.LastAccessTime.Month, dirent.LastAccessTime.Day,
                dirent.LastAccessTime.Hour, dirent.LastAccessTime.Minute,
                dirent.LastAccessTime.Second);

            listView1.Items.Add("Creation Time").SubItems.Add(creationTime.ToString());
            listView1.Items.Add("Last Write Time").SubItems.Add(lastWriteTime.ToString());
            listView1.Items.Add("Last Access Time").SubItems.Add(lastAccessTime.ToString());
        }

        private string FormatAttributes(FileAttribute attributes)
        {
            string attrStr = "";

            if (attributes.HasFlag(FileAttribute.Archive))
            {
                attrStr += "A";
            }
            else if (attributes.HasFlag(FileAttribute.Directory))
            {
                attrStr += "D";
            }
            else if (attributes.HasFlag(FileAttribute.Hidden))
            {
                attrStr += "H";
            }
            else if (attributes.HasFlag(FileAttribute.ReadOnly))
            {
                attrStr += "R";
            }
            else if (attributes.HasFlag(FileAttribute.System))
            {
                attrStr += "S";
            }

            return attrStr;
        }
    }
}
