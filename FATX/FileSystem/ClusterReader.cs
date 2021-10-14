using System.IO;

namespace FATX.FileSystem
{
    public class ClusterReader
    {
        Stream _stream;
        uint _bytesPerCluster;

        public ClusterReader(Stream stream, uint bytesPerCluster)
        {
            _stream = stream;
            _bytesPerCluster = bytesPerCluster;
        }

        public byte[] ReadCluster(uint cluster)
        {
            var clusterOffset = ClusterToPhysicalOffset(cluster);
            _stream.Seek(clusterOffset, SeekOrigin.Begin);
            byte[] clusterData = new byte[_bytesPerCluster];
            _stream.Read(clusterData, 0, (int)_bytesPerCluster);
            return clusterData;
        }

        public long ClusterToPhysicalOffset(uint cluster)
        {
            return (long)_bytesPerCluster * (long)(cluster - 1);
        }
    }
}