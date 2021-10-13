using System.IO;

using FATX.Streams;
using FATX.FileSystem;

namespace FATX.Drive
{
    public class Xbox360Drive : XDrive
    {
        public Xbox360Drive(Stream stream) : base(stream)
        {
            Build(stream);
        }

        public static bool Detect(Stream stream)
        {
            EndianReader reader = new EndianReader(stream, ByteOrder.Big);

            // Check for memory unit
            if (IsMemoryUnit(reader))
            {
                return true;
            }

            // Check for development hdd
            if (IsDevKit(reader))
            {
                return true;
            }

            // Check for retail hdd
            if (IsRetail(reader))
            {
                return true;
            }

            return false;
        }

        private void Build(Stream stream)
        {
            EndianReader reader = new EndianReader(stream, ByteOrder.Big);

            // All memory units have the same layout
            if (IsMemoryUnit(reader))
            {
                BuildMmu(stream);
                return;
            }

            // Dev kits have variable layouts
            if (IsDevKit(reader))
            {
                BuildDevKit(reader);
                return;
            }

            // Retail have always had the same layout
            if (IsRetail(reader))
            {
                BuildRetail(stream);
                return;
            }
        }

        private static bool IsMemoryUnit(EndianReader reader)
        {
            reader.Seek(0, SeekOrigin.Begin);
            return reader.ReadUInt64() == 0x534F44534D9058EB;
        }

        private static bool IsDevKit(EndianReader reader)
        {
            // TODO: Check all partitions
            reader.Seek(0, SeekOrigin.Begin);
            return reader.ReadUInt32() == 0x20000;
        }

        private static bool IsRetail(EndianReader reader)
        {
            reader.Seek(0x130eb0000, SeekOrigin.Begin);
            return reader.ReadUInt32() == FileSystem.Constants.VolumeSignature;
        }

        private void BuildMmu(Stream stream)
        {
            Name = "Xbox 360 Memory Unit";

            CreateFATXPartition(stream, "Storage", 0x20E2A000, 0xCE1D0000);
            CreateFATXPartition(stream, "SystemExtPartition", 0x13FFA000, 0xCE30000);
            CreateFATXPartition(stream, "SystemURLCachePartition", 0xDFFA000, 0x6000000);
            CreateFATXPartition(stream, "TitleURLCachePartition", 0xBFFA000, 0x2000000);
            CreateFATXPartition(stream, "StorageSystem", 0x7FFA000, 0x4000000);
        }

        private void BuildRetail(Stream stream)
        {
            Name = "Xbox 360 Retail HDD";

            const long dumpPartitionOffset = 0x100080000;

            CreateFATXPartition(stream, "Partition1", 0x130eb0000, stream.Length - 0x130eb0000);
            CreateFATXPartition(stream, "SystemPartition", 0x120eb0000, 0x10000000);

            // AddPartition("Cache0", 0x80000, 0x80000000);
            // AddPartition("Cache1", 0x80080000, 0x80000000);

            // AddPartition("DumpPartition", 0x100080000, 0x20E30000);
            // AddPartition("SystemURLCachePartition", dumpPartitionOffset + 0, 0x6000000);
            // AddPartition("TitleURLCachePartition", dumpPartitionOffset + 0x6000000, 0x2000000);
            // AddPartition("SystemExtPartition", dumpPartitionOffset + 0x0C000000, 0xCE30000);
            CreateFATXPartition(stream, "SystemAuxPartition", dumpPartitionOffset + 0x18e30000, 0x8000000);
        }

        private void BuildDevKit(EndianReader reader)
        {
            Name = "Xbox 360 DevKit HDD";

            reader.Seek(0, SeekOrigin.Begin);

            // Kernel version
            reader.ReadUInt16();    // Major
            reader.ReadUInt16();    // Minor
            reader.ReadUInt16();    // Build
            reader.ReadUInt16();    // Qfe

            // Partition1
            CreateFATXPartition(reader.BaseStream, "Partition1",
                ReadSectorCount(reader),
                ReadSectorCount(reader));

            // SystemPartition
            CreateFATXPartition(reader.BaseStream, "SystemPartition",
                ReadSectorCount(reader),
                ReadSectorCount(reader));

            // Unknown
            // AddPartition("Unknown1", ReadSectorCount(reader), ReadSectorCount(reader));

            // DumpPartition
            // AddPartition("DumpPartition", ReadSectorCount(reader), ReadSectorCount(reader));

            // PixDump
            // AddPartition("PixDump", ReadSectorCount(reader), ReadSectorCount(reader));

            // Unknown
            // AddPartition("Unknown2", ReadSectorCount(reader), ReadSectorCount(reader));

            // Unknown
            // AddPartition("Unknown3", ReadSectorCount(reader), ReadSectorCount(reader));

            // AltFlash
            // AddPartition("AltFlash", ReadSectorCount(reader), ReadSectorCount(reader));

            // Cache0
            // AddPartition("Cache0", ReadSectorCount(reader), ReadSectorCount(reader));
            
            // Cache1
            // AddPartition("Cache1", ReadSectorCount(reader), ReadSectorCount(reader));
        }

        private long ReadSectorCount(EndianReader reader)
        {
            return (long)reader.ReadUInt32() * Constants.SectorSize;
        }

        private void CreateFATXPartition(Stream stream, string name, long offset, long length)
        {
            AddPartition(name, offset, length)
                .Volume = new Volume(new SubStream(stream, offset, length), Platform.X360, name, offset, length);
        }
    }
}