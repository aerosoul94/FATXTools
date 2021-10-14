using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

namespace FATX.FileSystem
{
    public class FileAllocationTable : IEnumerable<uint>
    {
        readonly Stream _stream;
        readonly uint _maxClusters;
        readonly Platform _platform;

        public FatType FatType { get; private set; }

        private uint[] _fileAllocationTableBuffer;

        public uint this[int index] => _fileAllocationTableBuffer[index];

        public FileAllocationTable(Stream stream, Platform platform, FatType fatType, uint maxClusters)
        {
            _stream = stream;
            _platform = platform;
            _maxClusters = maxClusters;

            FatType = fatType;

            Read();
        }

        private void Read()
        {
            _fileAllocationTableBuffer = new uint[_maxClusters];

            var entrySize = FatType == FatType.Fat16 ? 2 : 4;

            byte[] tempFat = new byte[_maxClusters * entrySize];
            _stream.Read(tempFat, 0, (int)(_maxClusters * entrySize));

            if (_platform == Platform.X360)
            {
                for (var i = 0; i < _maxClusters; i++)
                    Array.Reverse(tempFat, i * entrySize, entrySize);
            }

            if (FatType == FatType.Fat16)
            {
                for (var i = 0; i < _maxClusters; i++)
                    _fileAllocationTableBuffer[i] = BitConverter.ToUInt16(tempFat, i * 2);
            }
            else
            {
                for (var i = 0; i < _maxClusters; i++)
                    _fileAllocationTableBuffer[i] = BitConverter.ToUInt32(tempFat, i * 4);
            }
        }

        public List<uint> GetClusterChain(uint firstCluster)
        {
            if (firstCluster == 0 || firstCluster > _maxClusters)
                throw new IndexOutOfRangeException(
                    $"First cluster is invalid (FirstCluster={firstCluster} MaxClusters={_maxClusters})"
                );

            List<uint> clusterChain = new List<uint>() { firstCluster };

            uint fatEntry = firstCluster;
            uint reservedIndexes = (FatType == FatType.Fat16) ?
                Constants.Cluster16Reserved : Constants.ClusterReserved;

            while (true)
            {
                fatEntry = _fileAllocationTableBuffer[fatEntry];

                if (fatEntry >= reservedIndexes)
                    break;

                if (fatEntry == 0 || fatEntry > _fileAllocationTableBuffer.Length)
                    return new List<uint>() { firstCluster };

                clusterChain.Add(fatEntry);
            }

            return clusterChain;
        }

        public List<uint> GetClusterChain(DirectoryEntry dirent)
        {
            var firstCluster = dirent.FirstCluster;
            if (firstCluster == 0 || firstCluster > _maxClusters)
                throw new IndexOutOfRangeException(
                    $"First cluster is invalid (FirstCluster={firstCluster} MaxClusters={_maxClusters})"
                );

            if (dirent.IsDeleted())
                return new List<uint>() { dirent.FirstCluster };

            return GetClusterChain(firstCluster);
        }

        public IEnumerator<uint> GetEnumerator()
        {
            return ((IEnumerable<uint>)_fileAllocationTableBuffer).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _fileAllocationTableBuffer.GetEnumerator();
        }
    }
}