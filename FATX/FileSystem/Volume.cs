using System;
using System.IO;
using System.Collections.Generic;

using FATX.Streams;

namespace FATX.FileSystem
{
    public class Volume
    {
        Indexer _indexer;
        readonly Stream _stream;
        public string Name { get; }
        public long Offset { get; }
        public long Length { get; }

        public Platform Platform { get; }
        public Boolean Mounted { get; private set; }
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

        public Volume(Stream stream, Platform platform, string name, long offset, long length)
        {
            this._stream = stream;  // Should be a sub-stream from offset to offset + length

            Name = name;
            Offset = offset;    // TODO: Not needed: Move to Partition
            Length = length;    // TODO: Not needed: Move to Partition

            Platform = platform;
            Mounted = false;
        }

        public void Mount()
        {
            // TODO: Remove Offset dependency, use a SubStream then Seek to 0.
            _stream.Seek(Offset, SeekOrigin.Begin);
            Metadata = new VolumeMetadata(_stream, Platform);

            FatByteOffset = Constants.ReservedBytes;
            BytesPerCluster = Metadata.SectorsPerCluster * Constants.SectorSize;
            MaxClusters = (uint)((Length / (long)BytesPerCluster)  + Constants.ReservedClusters);

            FatType fatType = (MaxClusters < Constants.Cluster16Reserved) ?
                FatType.Fat16 : FatType.Fat32;

            BytesPerFat = (uint)(MaxClusters * (int)((fatType == FatType.Fat16) ? 2 : 4));
            BytesPerFat = (BytesPerFat + (Constants.PageSize - 1)) & ~(Constants.PageSize - 1);

            _stream.Seek(Offset + FatByteOffset, SeekOrigin.Begin);
            FileAllocationTable = new FileAllocationTable(_stream, Platform, fatType, MaxClusters);
            
            FileAreaByteOffset = FatByteOffset + BytesPerFat;
            FileAreaLength = Length - FileAreaByteOffset;

            FileAreaStream = new SubStream(_stream, Offset + FileAreaByteOffset, FileAreaLength);
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