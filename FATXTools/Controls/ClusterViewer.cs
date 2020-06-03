using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using FATX;
using FATX.Analyzers;
//using Be.Windows.Forms;

using ClusterColorMap = System.Collections.Generic.Dictionary<uint, System.Drawing.Color>;

namespace FATXTools.Controls
{
    public partial class ClusterViewer : UserControl
    {
        private DataMap dataMap;
        private Volume volume;
        private IntegrityAnalyzer integrityAnalyzer;

        private ClusterColorMap clusterColorMap;

        private Color activeColor = Color.Green;
        private Color recoveredColor = Color.Yellow;
        private Color collisionColor = Color.Red;
        private Color rootColor = Color.Purple;

        public ClusterViewer(Volume volume, IntegrityAnalyzer integrityAnalyzer)
        {
            InitializeComponent();

            this.volume = volume;
            this.integrityAnalyzer = integrityAnalyzer;

            dataMap = new DataMap((int)volume.MaxClusters);
            dataMap.Location = new Point(0, 0);
            dataMap.Dock = DockStyle.Fill;
            dataMap.CellSelected += DataMap_CellSelected;
            dataMap.CellHovered += DataMap_CellHovered;
            dataMap.Increment = (int)volume.BytesPerCluster;

            clusterColorMap = new ClusterColorMap();

            this.Controls.Add(dataMap);
            InitializeActiveFileSystem();
            UpdateDataMap();
        }

        public void UpdateClusters()
        {
            for (uint i = 1; i < volume.MaxClusters; i++)
            {
                var occupants = integrityAnalyzer.GetClusterOccupants(i);

                if (occupants == null || occupants.Count == 0)
                {
                    // No occupants
                    continue;
                }

                if (occupants.Count > 1)
                {
                    if (occupants.Any(dirent => dirent.IsActive))
                    {
                        clusterColorMap[i] = activeColor;
                    }
                    else
                    {
                        clusterColorMap[i] = collisionColor;
                    }
                }
                else
                {
                    var occupant = occupants[0];
                    if (occupant.IsActive)
                    {
                        // Sole occupant
                        clusterColorMap[i] = activeColor;
                    }
                    else
                    {
                        // Only recovered occupant
                        clusterColorMap[i] = recoveredColor;
                    }
                }
            }

            UpdateDataMap();
        }

        private void InitializeActiveFileSystem()
        {
            clusterColorMap[volume.RootDirFirstCluster] = rootColor;

            UpdateClusters();
        }

        private int ClusterToCellIndex(uint clusterIndex)
        {
            return (int)clusterIndex - 1;
        }

        private uint CellToClusterIndex(int cellIndex)
        {
            return (uint)cellIndex + 1;
        }

        public void UpdateDataMap()
        {
            foreach (var pair in clusterColorMap)
            {
                dataMap.Cells[ClusterToCellIndex(pair.Key)].Color = pair.Value;
            }
        }

        private string BuildToolTipMessage(int index, DirectoryEntry dirent, bool deleted)
        {
            // What kind of data is stored in this cluster?
            string dataType;
            if (dirent.IsDirectory())
            {
                dataType = "Dirent Stream";
            }
            else
            {
                dataType = "File Data";
            }

            string message = Environment.NewLine +
                index.ToString() + "." +
                " Type: " + dataType + Environment.NewLine +
                " Occupant: " + dirent.FileName + Environment.NewLine +
                " File Size: " + dirent.FileSize.ToString("X8") + Environment.NewLine +
                " Date Created: " + dirent.CreationTime.AsDateTime() + Environment.NewLine + 
                " Date Written: " + dirent.LastWriteTime.AsDateTime() + Environment.NewLine +
                " Date Accessed: " + dirent.LastAccessTime.AsDateTime() + Environment.NewLine + 
                " Deleted: " + (!deleted).ToString() + Environment.NewLine;

            return message;
        }

        private void DataMap_CellHovered(object sender, EventArgs e)
        {
            var cellHoveredEventArgs = e as CellHoveredEventArgs;

            if (cellHoveredEventArgs != null)
            {
                var clusterIndex = CellToClusterIndex(cellHoveredEventArgs.Index);

                Debug.WriteLine($"Cluster Index: {clusterIndex}");

                var occupants = integrityAnalyzer.GetClusterOccupants(clusterIndex);

                string toolTipMessage = "Cluster Index: " + clusterIndex.ToString() + Environment.NewLine;
                toolTipMessage += "Cluster Address: 0x" + volume.ClusterToPhysicalOffset(clusterIndex).ToString("X");

                if (clusterIndex == volume.RootDirFirstCluster)
                {
                    toolTipMessage += Environment.NewLine + Environment.NewLine;
                    toolTipMessage += " Type: Root Directory";
                }
                else if (occupants == null)
                {
                    // TODO: something is off
                    Debug.WriteLine("Something is wrong.");
                }
                else if (occupants.Count > 0)
                {
                    toolTipMessage += Environment.NewLine;

                    int index = 1;
                    foreach (var occupant in occupants)
                    {
                        toolTipMessage += BuildToolTipMessage(index, occupant.GetDirent(), occupant.IsActive);
                        index++;
                    }
                }

                toolTip1.SetToolTip(this.dataMap, toolTipMessage);
            }
            else
            {
                toolTip1.SetToolTip(this.dataMap, "");
            }
        }

        private void DataMap_CellSelected(object sender, EventArgs e)
        {
            var clusterIndex = CellToClusterIndex(dataMap.SelectedIndex);

            Debug.WriteLine($"Cluster Index: {clusterIndex}");

            var occupants = integrityAnalyzer.GetClusterOccupants(clusterIndex);

            if (occupants == null)
            {
                // Something is wrong
            }
            else if (occupants.Count > 0)
            {
                // Just use the first one for now.
                // IDEAS: 
                //   1. Maybe toggle between each occupant after each click
                //   2. Only highlight the largest file
                //   3. Only highlight the smallest file
                var clusterChain = occupants[0].ClusterChain;
                foreach (var cluster in clusterChain)
                {
                    dataMap.Cells[ClusterToCellIndex(cluster)].Selected = true;
                }

                //Console.WriteLine($"Occupants: {occupants.Count}");
            }
        }
    }
}
