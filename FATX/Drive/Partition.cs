
using FATX.FileSystem;

namespace FATX.Drive
{
    public class Partition
    {
        public string Name { get; private set; }
        public long Offset { get; private set; }
        public long Length { get; private set; }

        public Volume Volume { get; set; }

        public Partition(string name, long offset, long length)
        {
            this.Name = name;
            this.Offset = offset;
            this.Length = length;
        }
    }
}