using System.Collections.Generic;

namespace FATX.FileSystem
{
    public class Indexer
    {
        readonly Platform _platform;
        readonly ClusterReader _reader;
        readonly FileAllocationTable _fileAllocationTable;

        public List<DirectoryEntry> Root { get; private set; }

        public Indexer(ClusterReader reader, FileAllocationTable fileAllocationTable, Platform platform, uint rootDirFirstCluster)
        {
            this._reader = reader;
            this._platform = platform;
            this._fileAllocationTable = fileAllocationTable;

            Root = ReadDirectoryStream(rootDirFirstCluster);
            PopulateDirectoryStream(Root, rootDirFirstCluster);
        }

        private List<DirectoryEntry> ReadDirectoryStream(uint cluster)
        {
            List<DirectoryEntry> stream = new List<DirectoryEntry>();

            byte[] data = _reader.ReadCluster(cluster);
            long clusterOffset = _reader.ClusterToPhysicalOffset(cluster);

            for (int i = 0; i < 256; i++)
            {
                DirectoryEntry dirent = new DirectoryEntry(_platform, data, (i * 0x40));

                if (dirent.FileNameLength == Constants.DirentNeverUsed ||
                    dirent.FileNameLength == Constants.DirentNeverUsed2)
                    break;

                dirent.Offset = clusterOffset + (i * 0x40);
                stream.Add(dirent);
            }

            return stream;
        }

        private void PopulateDirectoryStream(IEnumerable<DirectoryEntry> stream, uint cluster)
        {
            foreach (var dirent in stream)
            {
                dirent.Cluster = cluster;

                if (dirent.IsDirectory() && !dirent.IsDeleted())
                {
                    List<uint> chainMap = _fileAllocationTable.GetClusterChain(dirent);

                    foreach (uint clusterIndex in chainMap)
                    {
                        List<DirectoryEntry> childStream = ReadDirectoryStream(clusterIndex);

                        dirent.AddChildren(childStream);

                        PopulateDirectoryStream(childStream, clusterIndex);
                    }
                }
            }
        }
    }
}