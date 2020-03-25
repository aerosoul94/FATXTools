using System;
using System.IO;
using System.Collections.Generic;

namespace FATX
{
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
        private uint BytesPerFat;
        private bool IsFat16;
        private uint FatByteOffset;
        private uint FileAreaByteOffset;

        private List<DirectoryEntry> _root = new List<DirectoryEntry>();
        private uint[] _fileAllocationTable;
        private long _fileAreaLength;
        public Type _timeStampFormat;

        public Volume(DriveReader drive, string name, long offset, long length)
        {
            this._reader = drive;
            this._partitionName = name;
            this._partitionLength = length;
            this._partitionOffset = offset;

            this._timeStampFormat = (drive.ByteOrder == ByteOrder.Big) ?
                typeof(X360TimeStamp) : typeof(XTimeStamp);
        }

        public string Name
        {
            get { return _partitionName; }
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

        public List<DirectoryEntry> GetRoot()
        {
            return _root;
        }

        public void SeekFileArea(long offset, SeekOrigin origin = SeekOrigin.Begin)
        {
            offset += FileAreaByteOffset + _partitionOffset;
            _reader.Seek(offset, origin);
        }

        public void SeekToCluster(uint cluster)
        {
            var offset = ClusterToPhysicalOffset(cluster);
            _reader.Seek(offset);
        }

        public void Mount()
        {
            // Read and verify volume metadata.
            ReadVolumeMetadata();
            CalculateOffsets();
            ReadFileAllocationTable();

            long RootDirentStreamOffset = ClusterToPhysicalOffset(_rootDirFirstCluster);
            _root = ReadDirectoryStream(RootDirentStreamOffset);
            PopulateDirentStream(_root, _rootDirFirstCluster);
        }

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

        private void CalculateOffsets()
        {
            _bytesPerCluster = _sectorsPerCluster * Constants.SectorSize;
            _maxClusters = (uint)(_partitionLength / (long)_bytesPerCluster) 
                + Constants.ReservedClusters;

            uint bytesPerFat;
            if (_maxClusters < 0xfff0)
            {
                bytesPerFat = _maxClusters * 2;
                IsFat16 = true;
            }
            else
            {
                bytesPerFat = _maxClusters * 4;
                IsFat16 = false;
            }

            BytesPerFat = (bytesPerFat + (Constants.PageSize - 1)) &
                ~(Constants.PageSize - 1);

            this.FatByteOffset = Constants.ReservedBytes;
            this.FileAreaByteOffset = this.FatByteOffset + this.BytesPerFat;
            this._fileAreaLength = this.Length - this.FileAreaByteOffset;
        }

        private void ReadFileAllocationTable()
        {
            _fileAllocationTable = new uint[_maxClusters];

            var fatOffset = ByteOffsetToPhysicalOffset(this.FatByteOffset);
            _reader.Seek(fatOffset);
            if (this.IsFat16)
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

        private List<DirectoryEntry> ReadDirectoryStream(long offset)
        {
            List<DirectoryEntry> stream = new List<DirectoryEntry>();

            _reader.Seek(offset);

            for (int i = 0; i < 256; i++)
            {
                DirectoryEntry dirent = new DirectoryEntry(this);
                if (dirent.FileNameLength == Constants.DirentNeverUsed ||
                    dirent.FileNameLength == Constants.DirentNeverUsed2)
                {
                    break;
                }

                stream.Add(dirent);

            }

            return stream;
        }

        private void PopulateDirentStream(List<DirectoryEntry> stream, uint clusterIndex)
        {
            foreach (DirectoryEntry dirent in stream)
            {
                dirent.SetCluster(clusterIndex);
                //Console.WriteLine(String.Format("{0}", dirent.FileName));

                if (dirent.IsDirectory() && !dirent.IsDeleted())
                {
                    List<uint> chainMap = GetClusterChain(dirent.FirstCluster);

                    foreach (uint cluster in chainMap)
                    {
                        List<DirectoryEntry> direntStream = ReadDirectoryStream(
                            ClusterToPhysicalOffset(cluster));

                        dirent.AddDirentStreamToThisDirectory(direntStream);

                        PopulateDirentStream(direntStream, cluster);
                    }
                }
            }
        }

        public List<uint> GetClusterChain(uint firstCluster)
        {
            List<uint> clusterChain = new List<uint>();
            clusterChain.Add(firstCluster);
            uint fatEntry = firstCluster;
            uint reservedIndexes = (IsFat16) ? Constants.Cluster16Reserved : Constants.ClusterReserved;
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
                    Console.WriteLine("Invalid cluster index!");
                    clusterChain = new List<uint>(1);
                    clusterChain.Add(firstCluster);
                    return clusterChain;
                }

                // Get next cluster.
                clusterChain.Add(fatEntry);
            }

            return clusterChain;
        }

        public byte[] ReadCluster(uint cluster)
        {
            var clusterOffset = ClusterToPhysicalOffset(cluster);
            _reader.Seek(clusterOffset);
            byte[] clusterData = new byte[_bytesPerCluster];
            _reader.Read(clusterData, (int)_bytesPerCluster);
            return clusterData;
        }

        public void DumpFile(string path, DirectoryEntry file)
        {
            path = path + "/" + file.FileName;
            Console.WriteLine(path);

            List<uint> chainMap = GetClusterChain(file.FirstCluster);

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

        public void DumpDirectory(string path, DirectoryEntry dirent)
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
                // dump single file
                DumpFile(path, dirent);
            }
        }

        private long ByteOffsetToPhysicalOffset(long offset)
        {
            return this._partitionOffset + offset;
        }

        public long ClusterToPhysicalOffset(uint cluster)
        {
            var physicalOffset = ByteOffsetToPhysicalOffset(FileAreaByteOffset);
            long clusterOffset = (long)_bytesPerCluster * (long)(cluster - 1);
            return (physicalOffset + clusterOffset);
        }
    }
}
