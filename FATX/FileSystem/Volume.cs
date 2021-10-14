using System.Collections.Generic;
using System.IO;

using FATX.Drive;
using FATX.Streams;

namespace FATX.FileSystem
{
    public class Volume
    {
        Indexer _indexer;
        Partition _partition;
        readonly Stream _stream;

        public string Name => _partition.Name;
        public long Offset => _partition.Offset;
        public long Length => _partition.Length;

        public Platform Platform { get; }
        public bool Mounted { get; private set; }
        public List<DirectoryEntry> Root => _indexer.Root;

        public VolumeMetadata Metadata { get; private set; }
        public FileAllocationTable FileAllocationTable { get; private set; }
        public Stream FileAreaStream { get; private set; }
        public ClusterReader ClusterReader { get; private set; }

        public uint BytesPerCluster { get; private set; }
        public uint MaxClusters { get; private set; }
        public uint BytesPerFat { get; private set; }
        public long FatByteOffset { get; private set; }
        public long FileAreaByteOffset { get; private set; }
        public long FileAreaLength { get; private set; }

        public Volume(Partition partition, Platform platform)
        {
            _partition = partition;
            _stream = partition.Stream;  // Should be a sub-stream from offset to offset + length

            Platform = platform;
            Mounted = false;
        }

        public void Mount()
        {
            Metadata = new VolumeMetadata(_stream, Platform);

            FatByteOffset = Constants.ReservedBytes;
            BytesPerCluster = Metadata.SectorsPerCluster * Constants.SectorSize;
            MaxClusters = (uint)((_stream.Length / (long)BytesPerCluster) + Constants.ReservedClusters);

            FatType fatType = (MaxClusters < Constants.Cluster16Reserved) ?
                FatType.Fat16 : FatType.Fat32;

            BytesPerFat = (uint)(MaxClusters * (int)((fatType == FatType.Fat16) ? 2 : 4));
            BytesPerFat = (BytesPerFat + (Constants.PageSize - 1)) & ~(Constants.PageSize - 1);

            _stream.Seek(FatByteOffset, SeekOrigin.Begin);
            FileAllocationTable = new FileAllocationTable(_stream, Platform, fatType, MaxClusters);

            FileAreaByteOffset = FatByteOffset + BytesPerFat;
            FileAreaLength = _stream.Length - FileAreaByteOffset;

            FileAreaStream = new SubStream(_stream, FileAreaByteOffset, FileAreaLength);
            ClusterReader = new ClusterReader(FileAreaStream, BytesPerCluster);
            _indexer = new Indexer(ClusterReader, FileAllocationTable, Platform, Metadata.RootDirFirstCluster);

            Mounted = true;
        }

        private long CountFiles(List<DirectoryEntry> dirents)
        {
            long numFiles = 0;

            foreach (var dirent in dirents)
                numFiles += dirent.CountFiles();

            return numFiles;
        }

        public long CountFiles()
        {
            return CountFiles(Root);
        }

        public long GetUsedSpace()
        {
            long clustersUsed = 0;

            foreach (var cluster in FileAllocationTable)
            {
                if (cluster != Constants.ClusterAvailable)
                    clustersUsed++;
            }

            return (clustersUsed * BytesPerCluster);
        }

        public long GetFreeSpace()
        {
            return FileAreaLength - GetUsedSpace();
        }

        public long GetTotalSpace()
        {
            return FileAreaLength;
        }
    }
}