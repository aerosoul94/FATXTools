﻿using FATX.FileSystem;
using System;
using System.Collections.Generic;
using System.IO;

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
            Seek(0);

            // Definitely need to refactor all of this..
            // Check for memory unit image
            if (ReadUInt64() == 0x534F44534D9058EB)
            {
                Console.WriteLine("Mounting Xbox 360 Memory Unit..");

                ByteOrder = ByteOrder.Big;
                AddPartition("Storage", 0x20E2A000, 0xCE1D0000);
                AddPartition("SystemExtPartition", 0x13FFA000, 0xCE30000);
                AddPartition("SystemURLCachePartition", 0xDFFA000, 0x6000000);
                AddPartition("TitleURLCachePartition", 0xBFFA000, 0x2000000);
                AddPartition("StorageSystem", 0x7FFA000, 0x4000000);
                return;
            }

            // Might move to a new class
            // Check for Original XBOX partition.
            Seek(0xABE80000);
            if (ReadUInt32() == 0x58544146)
            {
                Console.WriteLine("Mounting Xbox Original HDD..");

                AddPartition("Partition1", 0xABE80000, 0x1312D6000);    // DATA
                AddPartition("Partition2", 0x8CA80000, 0x1f400000);     // SHELL
                AddPartition("Partition3", 0x5DC80000, 0x2ee00000);     // CACHE
                AddPartition("Partition4", 0x2EE80000, 0x2ee00000);     // CACHE
                AddPartition("Partition5", 0x80000, 0x2ee00000);        // CACHE
                return;
            }

            // Check for Original XBOX DVT3 (Prototype Development Kit) partition.
            Seek(0x80000);
            if (ReadUInt32() == 0x58544146)
            {
                Console.WriteLine("Mounting Xbox DVT3 HDD (v2)..");

                AddPartition("Partition1", 0x80000, 0x1312D6000);        // DATA
                AddPartition("Partition2", 0x131356000, 0x1f400000);     // SHELL
                AddPartition("Partition3", 0x150756000, 0x2ee00000);     // CACHE
                AddPartition("Partition4", 0x17F556000, 0x2ee00000);     // CACHE
                AddPartition("Partition5", 0x1AE356000, 0x2ee00000);     // CACHE
                return;
            }

            Seek(0x80004);
            if (ReadUInt32() == 0x58544146)
            {
                Console.WriteLine("Mounting Xbox DVT3 HDD (v1)..");

                AddPartition("Partition1", 0x80000, 0x1312D6000, true);        // DATA
                AddPartition("Partition2", 0x131356000, 0x1f400000, true);     // SHELL
                AddPartition("Partition3", 0x150756000, 0x2ee00000, true);     // CACHE
                AddPartition("Partition4", 0x17F556000, 0x2ee00000, true);     // CACHE
                AddPartition("Partition5", 0x1AE356000, 0x2ee00000, true);     // CACHE
                return;
            }

            // Check for XBOX 360 partitions.
            Seek(0);
            ByteOrder = ByteOrder.Big;
            if (ReadUInt32() == 0x20000)
            {
                Console.WriteLine("Mounting Xbox 360 Dev HDD..");

                // This is a dev formatted HDD.
                ReadUInt16();  // Kernel version
                ReadUInt16();

                // TODO: reading from raw devices requires sector aligned reads.
                Seek(8);
                // Partition1
                long dataOffset = (long)ReadUInt32() * Constants.SectorSize;
                long dataLength = (long)ReadUInt32() * Constants.SectorSize;
                // SystemPartition
                long shellOffset = (long)ReadUInt32() * Constants.SectorSize;
                long shellLength = (long)ReadUInt32() * Constants.SectorSize;
                // Unused?
                ReadUInt32();
                ReadUInt32();
                // DumpPartition
                ReadUInt32();
                ReadUInt32();
                // PixDump
                ReadUInt32();
                ReadUInt32();
                // Unused?
                ReadUInt32();
                ReadUInt32();
                // Unused?
                ReadUInt32();
                ReadUInt32();
                // AltFlash
                ReadUInt32();
                ReadUInt32();
                // Cache0
                long cache0Offset = (long)ReadUInt32() * Constants.SectorSize;
                long cache0Length = (long)ReadUInt32() * Constants.SectorSize;
                // Cache1
                long cache1Offset = (long)ReadUInt32() * Constants.SectorSize;
                long cache1Length = (long)ReadUInt32() * Constants.SectorSize;

                AddPartition("Partition1", dataOffset, dataLength);
                AddPartition("SystemPartition", shellOffset, shellLength);
                // TODO: Add support for these
                //AddPartition("Cache0", cache0Offset, cache0Length);
                //AddPartition("Cache1", cache1Offset, cache1Length);
            }
            else
            {
                Console.WriteLine("Mounting Xbox 360 Retail HDD..");

                //Seek(8);
                //var test = ReadUInt32();

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

                AddPartition("Partition1", 0x130eb0000, this.Length - 0x130eb0000);
                AddPartition("SystemPartition", 0x120eb0000, 0x10000000);

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
            }
        }

        public void AddPartition(string name, long offset, long length, bool legacy = false)
        {
            Volume partition = new Volume(this, name, offset, length, legacy);
            _partitions.Add(partition);
        }

        public Volume GetPartition(int index)
        {
            return _partitions[index];
        }

        public List<Volume> Partitions => _partitions;
    }
}
