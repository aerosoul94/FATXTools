using System.IO;

using FATX.Streams;
using FATX.FileSystem;

namespace FATX.Drive
{
    public class XboxDrive : XDrive
    {
        public XboxDrive(Stream stream) : base(stream)
        {
            Build(stream);
        }

        public static bool Detect(Stream stream)
        {
            EndianReader reader = new EndianReader(stream);
            stream.Seek(0xABE80000, SeekOrigin.Begin);
            return reader.ReadUInt32() == FileSystem.Constants.VolumeSignature;
        }

        private void Build(Stream stream)
        {
            Name = "Xbox Original HDD";

            CreateFATXPartition(stream, "Partition1", 0xABE80000, 0x1312D6000);    // DATA
            CreateFATXPartition(stream, "Partition2", 0x8CA80000, 0x1f400000);     // SHELL
            CreateFATXPartition(stream, "Partition3", 0x5DC80000, 0x2ee00000);     // CACHE
            CreateFATXPartition(stream, "Partition4", 0x2EE80000, 0x2ee00000);     // CACHE
            CreateFATXPartition(stream, "Partition5", 0x80000, 0x2ee00000);        // CACHE
        }

        private void CreateFATXPartition(Stream stream, string name, long offset, long length)
        {
            AddPartition(name, offset, length)
                .Volume = new Volume(stream, Platform.Xbox, name, offset, length);
        }
    }
}