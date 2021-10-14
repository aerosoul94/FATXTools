using System.Collections.Generic;
using System.IO;

namespace FATX.Drive
{
    public abstract class XDrive
    {
        public virtual string Name { get; protected set; }
        public Stream Stream { get; protected set; }
        public List<Partition> Partitions { get; } = new List<Partition>();

        public XDrive(Stream stream)
        {
            Stream = stream;
        }

        public Partition AddPartition(string name, long offset, long length)
        {
            var partition = new Partition(Stream, name, offset, length);

            Partitions.Add(partition);

            return partition;
        }
    }
}