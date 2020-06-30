using System;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using FATX;
using FATX.FileSystem;
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

        private Color emptyColor = Color.White;
        private Color activeColor = Color.Green;
        private Color recoveredColor = Color.Yellow;
        private Color collisionColor = Color.Red;
        private Color rootColor = Color.Purple;

        private int previousSelectedIndex;
        private int currentSelectedIndex;

        private int currentClusterChainIndex;

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

                clusterColorMap[i] = emptyColor;

                if (occupants == null || occupants.Count == 0)
                {
                    // No occupants
                    continue;
                }

                if (occupants.Count > 1)
                {
                    if (occupants.Any(file => !file.IsDeleted))
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
                    if (!occupant.IsDeleted)
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

            clusterColorMap[this.volume.RootDirFirstCluster] = rootColor;

            UpdateDataMap();
        }

        private void InitializeActiveFileSystem()
        {
            // TODO: See if we can merge this with UpdateClusters
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

        private void UpdateDataMap()
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
                " Deleted: " + (deleted).ToString() + Environment.NewLine;

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
                        toolTipMessage += BuildToolTipMessage(index, occupant.GetDirent(), occupant.IsDeleted);
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
            currentSelectedIndex = dataMap.SelectedIndex;

            var clusterIndex = CellToClusterIndex(currentSelectedIndex);

            Debug.WriteLine($"Cluster Index: {clusterIndex}");

            var occupants = integrityAnalyzer.GetClusterOccupants(clusterIndex);

            if (occupants == null)
            {
                // Something is wrong
            }
            else if (occupants.Count > 0)
            {
                if (currentSelectedIndex != previousSelectedIndex)
                {
                    previousSelectedIndex = currentSelectedIndex;
                    currentClusterChainIndex = 0;
                }

                if (currentClusterChainIndex >= occupants.Count)
                {
                    currentClusterChainIndex = 0;
                }

                var clusterChain = occupants[currentClusterChainIndex].ClusterChain;

                // TODO: Change highlight color for colliding clusters
                foreach (var cluster in clusterChain)
                {
                    dataMap.Cells[ClusterToCellIndex(cluster)].Selected = true;
                }

                // Toggle between each occupant after each click
                currentClusterChainIndex++;
            }
        }
    }
}
