using System;
using System.IO;
using System.Collections.Generic;

namespace FATX
{
    public enum VolumePlatform
    {
        Xbox,
        X360
    }

    public class Volume
    {
        private readonly DriveReader _reader;
        private readonly string _partitionName;
        private readonly long _partitionOffset;
        private readonly long _partitionLength;

        public const uint VolumeSignature = 0x58544146;

        private uint _signature;
        private uint _serialNumber;
        private uint _sectorsPerCluster;
        private uint _rootDirFirstCluster;

        private uint _bytesPerCluster;
        private uint _maxClusters;
        private uint _bytesPerFat;
        private bool _isFat16;
        private uint _fatByteOffset;
        private uint _fileAreaByteOffset;

        private List<DirectoryEntry> _root = new List<DirectoryEntry>();
        private uint[] _fileAllocationTable;
        private long _fileAreaLength;
        private VolumePlatform _platform;

        public Volume(DriveReader reader, string name, long offset, long length)
        {
            this._reader = reader;
            this._partitionName = name;
            this._partitionLength = length;
            this._partitionOffset = offset;

            this._platform = (reader.ByteOrder == ByteOrder.Big) ?
                VolumePlatform.X360 : VolumePlatform.Xbox;
        }

        public string Name
        {
            get { return _partitionName; }
        }

        public uint RootDirFirstCluster
        {
            get { return _rootDirFirstCluster; }
        }

        public long Length
        {
            get { return _partitionLength; }
        }

        public long FileAreaLength
        {
            get { return _fileAreaLength; }
        }

        public uint MaxClusters
        {
            get { return _maxClusters; }
        }

        public uint BytesPerCluster
        {
            get { return _bytesPerCluster; }
        }

        public DriveReader Reader
        {
            get { return _reader; }
        }

        public uint[] FileAllocationTable
        {
            get { return _fileAllocationTable; }
        }

        public long FileAreaByteOffset
        {
            get { return _fileAreaByteOffset; }
        }

        public long Offset
        {
            get { return _partitionOffset; }
        }

        public List<DirectoryEntry> GetRoot()
        {
            return _root;
        }

        public VolumePlatform Platform
        {
            get { return _platform; }
        }

        /// <summary>
        /// Loads the FATX file system.
        /// </summary>
        public void Mount()
        {
            // Read and verify volume metadata.
            ReadVolumeMetadata();
            CalculateOffsets();
            ReadFileAllocationTable();

            _root = ReadDirectoryStream(_rootDirFirstCluster);
            PopulateDirentStream(_root, _rootDirFirstCluster);
        }

        /// <summary>
        /// Read and verifies the FATX header.
        /// </summary>
        private void ReadVolumeMetadata()
        {
            _reader.Seek(_partitionOffset);
            _signature = _reader.ReadUInt32();
            _serialNumber = _reader.ReadUInt32();
            _sectorsPerCluster = _reader.ReadUInt32();
            _rootDirFirstCluster = _reader.ReadUInt32();

            if (_signature != VolumeSignature)
            {
                throw new FormatException(
                    String.Format("Invalid FATX Signature for {0}", _partitionName));
            }
        }

        /// <summary>
        /// Calculate offsets needed to perform work on this file system.
        /// </summary>
        private void CalculateOffsets()
        {
            _bytesPerCluster = _sectorsPerCluster * Constants.SectorSize;
            _maxClusters = (uint)(_partitionLength / (long)_bytesPerCluster) 
                + Constants.ReservedClusters;

            uint bytesPerFat;
            if (_maxClusters < 0xfff0)
            {
                bytesPerFat = _maxClusters * 2;
                _isFat16 = true;
            }
            else
            {
                bytesPerFat = _maxClusters * 4;
                _isFat16 = false;
            }

            _bytesPerFat = (bytesPerFat + (Constants.PageSize - 1)) &
                ~(Constants.PageSize - 1);

            this._fatByteOffset = Constants.ReservedBytes;
            this._fileAreaByteOffset = this._fatByteOffset + this._bytesPerFat;
            this._fileAreaLength = this.Length - this.FileAreaByteOffset;
        }

        /// <summary>
        /// Read either the 16 or 32 bit file allocation table.
        /// </summary>
        private void ReadFileAllocationTable()
        {
            _fileAllocationTable = new uint[_maxClusters];

            var fatOffset = ByteOffsetToPhysicalOffset(this._fatByteOffset);
            _reader.Seek(fatOffset);
            if (this._isFat16)
            {
                for (int i = 0; i < _maxClusters; i++)
                {
                    _fileAllocationTable[i] = _reader.ReadUInt16();
                }
            }
            else
            {
                //byte[] buffer = new byte[this.BytesPerFat];
                //_reader.Read(buffer, (int)this.BytesPerFat);
                //Buffer.BlockCopy(buffer, 0, _fileAllocationTable, 0, (int)this.MaxClusters * 4);
                for (int i = 0; i < _maxClusters; i++)
                {
                    _fileAllocationTable[i] = Reader.ReadUInt32();
                }
            }
        }

        /// <summary>
        /// Read a single directory stream.
        /// </summary>
        /// <param name="cluster"></param>
        /// <returns></returns>
        private List<DirectoryEntry> ReadDirectoryStream(uint cluster)
        {
            List<DirectoryEntry> stream = new List<DirectoryEntry>();

            byte[] data = ReadCluster(cluster);
            long clusterOffset = ClusterToPhysicalOffset(cluster);

            for (int i = 0; i < 256; i++)
            {
                DirectoryEntry dirent = new DirectoryEntry(this, data, (i * 0x40));

                if (dirent.FileNameLength == Constants.DirentNeverUsed ||
                    dirent.FileNameLength == Constants.DirentNeverUsed2)
                {
                    // We are at the last dirent.
                    break;
                }

                dirent.Offset = clusterOffset + (i * 0x40);

                stream.Add(dirent);

            }

            return stream;
        }

        /// <summary>
        /// Iterates dirent's from stream and populates directories with its child
        /// dirents. 
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="clusterIndex"></param>
        private void PopulateDirentStream(List<DirectoryEntry> stream, uint clusterIndex)
        {
            foreach (DirectoryEntry dirent in stream)
            {
                dirent.SetCluster(clusterIndex);
                //Console.WriteLine(String.Format("{0}", dirent.FileName));

                if (dirent.IsDirectory() && !dirent.IsDeleted())
                {
                    List<uint> chainMap = GetClusterChain(dirent);

                    foreach (uint cluster in chainMap)
                    {
                        List<DirectoryEntry> direntStream = ReadDirectoryStream(cluster);

                        dirent.AddChildren(direntStream);

                        PopulateDirentStream(direntStream, cluster);
                    }
                }
            }
        }

        /// <summary>
        /// Seek to any offset in the file area.
        /// </summary>
        /// <param name="offset"></param>
        /// <param name="origin"></param>
        public void SeekFileArea(long offset, SeekOrigin origin = SeekOrigin.Begin)
        {
            // TODO: Check for invalid offset
            offset += FileAreaByteOffset + _partitionOffset;
            _reader.Seek(offset, origin);
        }

        /// <summary>
        /// Seek to a cluster.
        /// </summary>
        /// <param name="cluster"></param>
        public void SeekToCluster(uint cluster)
        {
            var offset = ClusterToPhysicalOffset(cluster);
            _reader.Seek(offset);
        }

        /// <summary>
        /// Reads a cluster and returns the data.
        /// </summary>
        /// <param name="cluster"></param>
        /// <returns></returns>
        public byte[] ReadCluster(uint cluster)
        {
            var clusterOffset = ClusterToPhysicalOffset(cluster);
            _reader.Seek(clusterOffset);
            byte[] clusterData = new byte[_bytesPerCluster];
            _reader.Read(clusterData, (int)_bytesPerCluster);
            return clusterData;
        }

        /// <summary>
        /// Get a cluster chain for a dirent from the file allocation table.
        /// </summary>
        /// <param name="dirent">The DirectoryEntry to get the cluster chain for.</param>
        /// <returns></returns>
        public List<uint> GetClusterChain(DirectoryEntry dirent)
        {
            var firstCluster = dirent.FirstCluster;
            List<uint> clusterChain = new List<uint>();

            if (firstCluster == 0)
            {
                // 0 is reserved!
                Console.WriteLine($"File {dirent.FileName} first cluster is invalid.");
                return clusterChain;
            }
            
            clusterChain.Add(firstCluster);
            
            if (dirent.IsDeleted())
            {
                return clusterChain;
            }

            uint fatEntry = firstCluster;
            uint reservedIndexes = (_isFat16) ? Constants.Cluster16Reserved : Constants.ClusterReserved;
            while (true)
            {
                fatEntry = _fileAllocationTable[fatEntry];
                if (fatEntry >= reservedIndexes)
                {
                    break;
                }

                if (fatEntry == 0 || fatEntry > _fileAllocationTable.Length)
                {
                    // TODO: Warn user.
                    Console.WriteLine($"File {dirent.FileName} has a corrupt cluster chain!");
                    clusterChain = new List<uint>(1);
                    clusterChain.Add(firstCluster);
                    return clusterChain;
                }

                // Get next cluster.
                clusterChain.Add(fatEntry);
            }

            return clusterChain;
        }

        /// <summary>
        /// Dump a file to path.
        /// </summary>
        /// <param name="path"></param>
        /// <param name="file"></param>
        private void DumpFile(string path, DirectoryEntry file)
        {
            path = path + "/" + file.FileName;
            Console.WriteLine(path);

            List<uint> chainMap = GetClusterChain(file);

            using (FileStream outFile = File.OpenWrite(path))
            {
                uint bytesLeft = file.FileSize;

                foreach (uint cluster in chainMap)
                {
                    byte[] clusterData = ReadCluster(cluster);

                    var writeSize = Math.Min(bytesLeft, _bytesPerCluster);
                    outFile.Write(clusterData, 0, (int)writeSize);

                    bytesLeft -= writeSize;
                }
            }

            File.SetCreationTime(path, file.CreationTime.AsDateTime());
            File.SetLastWriteTime(path, file.LastWriteTime.AsDateTime());
            File.SetLastAccessTime(path, file.LastAccessTime.AsDateTime());
        }

        /// <summary>
        /// Dump a directory to path.
        /// </summary>
        /// <param name="path"></param>
        /// <param name="dirent"></param>
        private void DumpDirectory(string path, DirectoryEntry dirent)
        {
            path = path + "/" + dirent.FileName;
            Console.WriteLine(path);

            Directory.CreateDirectory(path);

            foreach (DirectoryEntry child in dirent.GetChildren())
            {
                DumpDirent(path, child);
            }

            Directory.SetCreationTime(path, dirent.CreationTime.AsDateTime());
            Directory.SetLastWriteTime(path, dirent.LastWriteTime.AsDateTime());
            Directory.SetLastAccessTime(path, dirent.LastAccessTime.AsDateTime());
        }

        /// <summary>
        /// Dump a dirent to path.
        /// </summary>
        /// <param name="path"></param>
        /// <param name="dirent"></param>
        public void DumpDirent(string path, DirectoryEntry dirent)
        {
            if (dirent.IsDeleted())
            {
                path = path + "/" + dirent.FileName;
                Console.WriteLine(String.Format("{0} failed to dump as it was deleted.", path));
                return;
            }

            if (dirent.IsDirectory())
            {
                DumpDirectory(path, dirent);
            }
            else
            {
                DumpFile(path, dirent);
            }
        }

        /// <summary>
        /// Convert a byte offset (relative to the volume) to an offset into the
        /// image file it belongs to.
        /// </summary>
        /// <param name="offset"></param>
        /// <returns></returns>
        private long ByteOffsetToPhysicalOffset(long offset)
        {
            return this._partitionOffset + offset;
        }

        /// <summary>
        /// Convert a cluster index to an offset into the image file it belongs to.
        /// </summary>
        /// <param name="cluster"></param>
        /// <returns></returns>
        public long ClusterToPhysicalOffset(uint cluster)
        {
            var physicalOffset = ByteOffsetToPhysicalOffset(FileAreaByteOffset);
            long clusterOffset = (long)_bytesPerCluster * (long)(cluster - 1);
            return (physicalOffset + clusterOffset);
        }

        public long GetFreeSpace()
        {
            var FileAreaLength = Length - FileAreaByteOffset;
            return (FileAreaLength) - GetUsedSpace();
        }

        public long GetUsedSpace()
        {
            // Count number of used clusters
            uint clustersUsed = 0;

            foreach (var cluster in FileAllocationTable)
            {
                if (cluster != Constants.ClusterAvailable)
                {
                    clustersUsed++;
                }
            }

            return (clustersUsed * BytesPerCluster);
        }
    }
}
