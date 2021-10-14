using System;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

using FATX.Analyzers;
using FATX.FileSystem;
//using Be.Windows.Forms;

using ClusterColorMap = System.Collections.Generic.Dictionary<uint, System.Drawing.Color>;

namespace FATXTools.Controls
{
    public partial class ClusterViewer : UserControl
    {
        private DataMap _dataMap;
        private Volume _volume;
        private IntegrityAnalyzer _integrityAnalyzer;

        private ClusterColorMap _clusterColorMap;

        private static readonly Color EmptyColor = Color.White;
        private static readonly Color ActiveColor = Color.Green;
        private static readonly Color RecoveredColor = Color.Yellow;
        private static readonly Color CollisionColor = Color.Red;
        private static readonly Color RootColor = Color.Purple;

        private int _previousSelectedIndex;
        private int _currentSelectedIndex;

        private int _currentClusterChainIndex;

        public ClusterViewer(Volume volume, IntegrityAnalyzer integrityAnalyzer)
        {
            InitializeComponent();

            _volume = volume;
            _integrityAnalyzer = integrityAnalyzer;

            _dataMap = new DataMap((int)volume.MaxClusters)
            {
                Location = new Point(0, 0),
                Dock = DockStyle.Fill,
                Increment = (int)volume.BytesPerCluster
            };

            _dataMap.CellSelected += DataMap_CellSelected;
            _dataMap.CellHovered += DataMap_CellHovered;

            _clusterColorMap = new ClusterColorMap();

            Controls.Add(_dataMap);
            InitializeActiveFileSystem();
            UpdateDataMap();
        }

        public void UpdateClusters()
        {
            for (uint i = 1; i < _volume.MaxClusters; i++)
            {
                var occupants = _integrityAnalyzer.GetClusterOccupants(i);

                _clusterColorMap[i] = EmptyColor;

                if (occupants == null || occupants.Count == 0)
                {
                    // No occupants
                    continue;
                }

                if (occupants.Count > 1)
                {
                    if (occupants.Any(file => !file.IsDeleted))
                    {
                        _clusterColorMap[i] = ActiveColor;
                    }
                    else
                    {
                        _clusterColorMap[i] = CollisionColor;
                    }
                }
                else
                {
                    var occupant = occupants[0];
                    if (!occupant.IsDeleted)
                    {
                        // Sole occupant
                        _clusterColorMap[i] = ActiveColor;
                    }
                    else
                    {
                        // Only recovered occupant
                        _clusterColorMap[i] = RecoveredColor;
                    }
                }
            }

            _clusterColorMap[_volume.Metadata.RootDirFirstCluster] = RootColor;

            UpdateDataMap();
        }

        private void InitializeActiveFileSystem()
        {
            // TODO: See if we can merge this with UpdateClusters
            _clusterColorMap[_volume.Metadata.RootDirFirstCluster] = RootColor;

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

        private void SetCellColor(int cellIndex, Color color)
        {
            if (cellIndex < 0 || cellIndex > _dataMap.CellCount)
                return;

            _dataMap.Cells[cellIndex].Color = color;
        }

        private void UpdateDataMap()
        {
            foreach (var pair in _clusterColorMap)
            {
                SetCellColor(ClusterToCellIndex(pair.Key), pair.Value);
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

                var occupants = _integrityAnalyzer.GetClusterOccupants(clusterIndex);

                string toolTipMessage = "Cluster Index: " + clusterIndex.ToString() + Environment.NewLine;
                toolTipMessage += "Cluster Address: 0x" + _volume.ClusterReader.ClusterToPhysicalOffset(clusterIndex).ToString("X");

                if (clusterIndex == _volume.Metadata.RootDirFirstCluster)
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

                toolTip1.SetToolTip(_dataMap, toolTipMessage);
            }
            else
            {
                toolTip1.SetToolTip(_dataMap, "");
            }
        }

        private void DataMap_CellSelected(object sender, EventArgs e)
        {
            _currentSelectedIndex = _dataMap.SelectedIndex;

            var clusterIndex = CellToClusterIndex(_currentSelectedIndex);

            Debug.WriteLine($"Cluster Index: {clusterIndex}");

            var occupants = _integrityAnalyzer.GetClusterOccupants(clusterIndex);

            if (occupants == null)
            {
                // Something is wrong
            }
            else if (occupants.Count > 0)
            {
                if (_currentSelectedIndex != _previousSelectedIndex)
                {
                    _previousSelectedIndex = _currentSelectedIndex;
                    _currentClusterChainIndex = 0;
                }

                if (_currentClusterChainIndex >= occupants.Count)
                {
                    _currentClusterChainIndex = 0;
                }

                var clusterChain = occupants[_currentClusterChainIndex].ClusterChain;

                // TODO: Change highlight color for colliding clusters
                foreach (var cluster in clusterChain)
                {
                    _dataMap.Cells[ClusterToCellIndex(cluster)].Selected = true;
                }

                // Toggle between each occupant after each click
                _currentClusterChainIndex++;
            }
        }
    }
}
