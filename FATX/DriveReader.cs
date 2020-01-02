using System;
using System.IO;
using System.Collections.Generic;

namespace FATX
{
    public class DriveReader : EndianReader
    {
        private List<Volume> _partitions = new List<Volume>();
        public DriveReader(Stream stream)
            : base(stream)
        {
        }

        public void Initialize()
        {
            base.Seek(0);

            // Might move to a new class
            // Check for Original XBOX partition.
            base.Seek(0xABE80000);
            if (base.ReadUInt32() == 0x58544146)
            {
                AddPartition("Partition5", 0x80000, 0x2ee00000);        // CACHE
                AddPartition("Partition4", 0x2EE80000, 0x2ee00000);     // CACHE
                AddPartition("Partition3", 0x5DC80000, 0x2ee00000);     // CACHE
                AddPartition("Partition2", 0x8CA80000, 0x1f400000);     // SHELL
                AddPartition("Partition1", 0xABE80000, 0x1312D6000);    // DATA
            }
            else
            {
                // Check for XBOX 360 partitions.
                base.Seek(0);
                base.ByteOrder = ByteOrder.Big;
                if (base.ReadUInt32() == 0x20000)
                {
                    // This is a dev formatted HDD.
                    base.ReadUInt16();  // Kernel version
                    base.ReadUInt16();

                    base.Seek(8);
                    // Partition1
                    long dataOffset = (long)base.ReadUInt32() * Constants.SectorSize;
                    long dataLength = (long)base.ReadUInt32() * Constants.SectorSize;
                    // SystemPartition
                    long shellOffset = (long)base.ReadUInt32() * Constants.SectorSize;
                    long shellLength = (long)base.ReadUInt32() * Constants.SectorSize;
                    // Unused?
                    base.ReadUInt32();
                    base.ReadUInt32();
                    // DumpPartition
                    base.ReadUInt32();
                    base.ReadUInt32();
                    // PixDump
                    base.ReadUInt32();
                    base.ReadUInt32();
                    // Unused?
                    base.ReadUInt32();
                    base.ReadUInt32();
                    // Unused?
                    base.ReadUInt32();
                    base.ReadUInt32();
                    // AltFlash
                    base.ReadUInt32();
                    base.ReadUInt32();
                    // Cache0
                    long cache0Offset = (long)base.ReadUInt32() * Constants.SectorSize;
                    long cache0Length = (long)base.ReadUInt32() * Constants.SectorSize;
                    // Cache1
                    long cache1Offset = (long)base.ReadUInt32() * Constants.SectorSize;
                    long cache1Length = (long)base.ReadUInt32() * Constants.SectorSize;

                    AddPartition("SystemPartition", shellOffset, shellLength);
                    AddPartition("Partition1", dataOffset, dataLength);
                    // TODO: Add support for these
                    //AddPartition("Cache0", cache0Offset, cache0Length);
                    //AddPartition("Cache1", cache1Offset, cache1Length);
                }
                else
                {
                    // This is a retail formatted HDD.
                    /// Partition0 0, END
                    /// Cache0 0x80000, 0x80000000
                    /// Cache1 0x80080000, 0x80000000
                    /// DumpPartition 0x100080000, 0x20E30000
                    ///   SystemURLCachePartition 0, 0x6000000
                    ///   TitleURLCachePartition 0x6000000, 0x2000000
                    ///   SystemExtPartition 0x0C000000, 0x0CE30000
                    ///   SystemAuxPartition 0x18e30000, 0x08000000
                    /// SystemPartition 0x120EB0000, 0x10000000
                    /// Partition1 0x130EB0000, END

                    // 0x118EB0000 - 0x100080000
                    // TODO: Add support for these
                    //AddPartition("Cache0", 0x80000, 0x80000000);
                    //AddPartition("Cache1", 0x80080000, 0x80000000);

                    const long dumpPartitionOffset = 0x100080000;
                    // TODO: Add support for these
                    //AddPartition("DumpPartition", 0x100080000, 0x20E30000);
                    //AddPartition("SystemURLCachePartition", dumpPartitionOffset + 0, 0x6000000);
                    //AddPartition("TitleURLCachePartition", dumpPartitionOffset + 0x6000000, 0x2000000);
                    //AddPartition("SystemExtPartition", dumpPartitionOffset + 0x0C000000, 0xCE30000);
                    AddPartition("SystemAuxPartition", dumpPartitionOffset + 0x18e30000, 0x8000000);

                    AddPartition("SystemPartition", 0x120eb0000, 0x10000000);
                    AddPartition("Partition1", 0x130eb0000, this.Length - 0x130eb0000);
                }
            }
        }

        public void AddPartition(string name, long offset, long length)
        {
            Volume partition = new Volume(this, name, offset, length);
            _partitions.Add(partition);
        }

        public Volume GetPartition(int index)
        {
            return _partitions[index];
        }

        public List<Volume> GetPartitions()
        {
            return _partitions;
        }
    }
}
