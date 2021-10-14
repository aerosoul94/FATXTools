using System.IO;

using FATX.FileSystem;
using FATX.Streams;

namespace FATX.Drive
{
    public class Partition
    {
        public string Name { get; private set; }
        public long Offset { get; private set; }
        public long Length { get; private set; }

        public Stream Stream { get; private set; }
        public Volume Volume { get; set; }

        /// <summary>
        /// A partition that exists inside a Drive.
        /// </summary>
        /// <param name="diskStream">The disk stream that this partition exists.</param>
        /// <param name="name">The name given to this partition.</param>
        /// <param name="offset">The offset into the drive.</param>
        /// <param name="length">The length of this partition.</param>
        public Partition(Stream diskStream, string name, long offset, long length)
        {
            Name = name;
            Offset = offset;
            Length = length;
            Stream = new SubStream(diskStream, offset, length);
        }
    }
}