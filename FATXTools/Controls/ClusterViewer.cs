using System;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Linq;
using FATX;
//using Be.Windows.Forms;

namespace FATXTools.Controls
{
    public partial class ClusterViewer : UserControl
    {
        private DataMap dataMap;
        private Volume volume;

        public ClusterViewer(Volume volume)
        {
            InitializeComponent();

            this.volume = volume;

            dataMap = new DataMap((int)volume.BytesPerCluster);
            //splitContainer1.Panel1.Controls.Add(dataMap);
            dataMap.Location = new System.Drawing.Point(0, 0);
            // We need this to auto resize control to the size of the panel
            dataMap.Dock = DockStyle.Fill;
            dataMap.CellSelected += Control_CellSelected;
            dataMap.CellHovered += Control_CellHovered;
            this.Controls.Add(dataMap);

            //DynamicFileByteProvider dynamicFileByteProvider = new DynamicFileByteProvider(volume.Reader.BaseStream);
            //hexBox1.ByteProvider = dynamicFileByteProvider;

            InitializeCells();
        }

        private void InitializeCells()
        {
            dataMap.NumCells = this.volume.MaxClusters;

            dataMap.SetCellStatus(this.volume.RootDirFirstCluster, ClusterStatus.PURPLE);
            InitializeRootDirectory(this.volume.GetRoot());
        }

        public void InitializeRootDirectory(List<DirectoryEntry> dirents)
        {
            foreach (DirectoryEntry dirent in dirents)
            {
                if (dirent.IsDeleted())
                {
                    dataMap.SetCellValue(dirent.FirstCluster, dirent);
                    dataMap.SetCellStatus(dirent.FirstCluster, ClusterStatus.YELLOW);
                    continue;
                }

                var firstCluster = dirent.FirstCluster;
                //cellStatus[firstCluster] = ClusterStatus.GREEN;

                var chain = volume.GetClusterChain(dirent);
                //Console.WriteLine("Dirent: {0}", dirent.FileName);
                //Console.Write("Clusters: [");
                foreach (uint cluster in chain)
                {
                    //Console.Write("{0},", cluster);
                    if (dataMap.GetCellStatus(cluster) == ClusterStatus.GREEN)
                    {
                        dataMap.SetCellValue(cluster, dirent);
                        dataMap.SetCellStatus(cluster, ClusterStatus.RED);
                    }
                    else
                    {
                        dataMap.SetCellValue(cluster, dirent);
                        dataMap.SetCellStatus(cluster, ClusterStatus.GREEN);
                    }
                }
                //Console.WriteLine("]");

                if (dirent.IsDirectory())
                {
                    InitializeRootDirectory(dirent.GetChildren());
                }
            }
        }

        public void UpdateClusters(List<DirectoryEntry> dirents)
        {
            foreach (DirectoryEntry dirent in dirents)
            {
                var firstCluster = dirent.FirstCluster;
                //cellStatus[firstCluster] = ClusterStatus.GREEN;

                //Console.WriteLine("Dirent: {0}", dirent.FileName);
                //Console.Write("Clusters: [");
                //Console.Write("{0},", cluster);
                var numClusters = (int)(((dirent.FileSize + (this.volume.BytesPerCluster - 1)) &
                    ~(this.volume.BytesPerCluster - 1)) / this.volume.BytesPerCluster);
                List<int> chain;
                //if (numClusters > 0x100)
                {
                    chain = new List<int>() { (int)dirent.FirstCluster };
                }
                //else
                //{
                //    chain = Enumerable.Range((int)dirent.FirstCluster, numClusters).ToList();
                //}
                foreach (var c in chain)
                {
                    uint cluster = (uint)c;
                    var currentStatus = dataMap.GetCellStatus(cluster);
                    if (currentStatus != ClusterStatus.WHITE)
                    {
                        var value = (DirectoryEntry)dataMap.GetCellValue(cluster)[0];
                        if (value.Offset != dirent.Offset)
                        {
                            dataMap.SetCellValue(cluster, dirent);
                            dataMap.SetCellStatus(cluster, ClusterStatus.RED);
                        }
                    }
                    else
                    {
                        dataMap.SetCellValue(cluster, dirent);
                        dataMap.SetCellStatus(cluster, ClusterStatus.ORANGE);
                    }
                }
                //Console.WriteLine("]");

                if (dirent.IsDirectory())
                {
                    UpdateClusters(dirent.GetChildren());
                }
            }
        }

        private void Control_CellHovered(object sender, EventArgs e)
        {
            CellDataEventArgs c = (CellDataEventArgs)e;
            if (c != null)
            {
                var dirents = c.Value;
                if (dirents != null)
                {
                    string toolTipMessage = "Index: " + c.ClusterIndex.ToString() + Environment.NewLine +
                        "Offset: " + this.volume.ClusterToPhysicalOffset(c.ClusterIndex).ToString("X16") + Environment.NewLine;

                    var i = 1;
                    foreach (var obj in dirents)
                    {
                        var dirent = (DirectoryEntry)obj;
                        string dataType;
                        if (dirent.IsDirectory())
                        {
                            dataType = "Dirent Stream";
                        }
                        else
                        {
                            dataType = "File Data";
                        }

                        toolTipMessage += Environment.NewLine + 
                            i.ToString() + "." +
                            " Type: " + dataType + Environment.NewLine +
                            " Owner: " + dirent.FileName + Environment.NewLine +
                            " File Size: " + dirent.FileSize.ToString("X8") + Environment.NewLine + 
                            " Date Written: " + dirent.LastWriteTime.AsDateTime() + Environment.NewLine;

                        i++;
                    }

                    //toolTip1.Active = true;
                    toolTip1.SetToolTip(this.dataMap, toolTipMessage);
                }
                else
                {
                    //toolTip1.Active = false;
                    toolTip1.SetToolTip(this.dataMap, 
                        "Index: " + c.ClusterIndex.ToString());
                }
            }
            else
            {
                //toolTip1.Active = false;
                toolTip1.SetToolTip(this.dataMap, "");
            }
        }

        private void Control_CellSelected(object sender, EventArgs e)
        {
            CellDataEventArgs c = (CellDataEventArgs)e;
            var dirents = c.Value;
            foreach (var obj in dirents)
            {
                var dirent = (DirectoryEntry)obj;
                var chain = this.volume.GetClusterChain(dirent);
                foreach (var cluster in chain)
                {
                    dataMap.SelectCell(cluster);
                }
                //Console.WriteLine(c.ClusterIndex);
                //Console.WriteLine(dirent.FileName);
            }
            //hexBox1.SelectionStart = this.volume.ClusterToPhysicalOffset(c.ClusterIndex);
            //hexBox1.SelectionLength = 1;
            //hexBox1.ScrollByteIntoView(hexBox1.SelectionStart);
            //hexBox1.Focus();
            //throw new NotImplementedException();
        }
    }
}
