using System.IO;

using FATX.FileSystem;
using FATX.Streams;

namespace FATX.Drive
{
    public class XboxDrive : XDrive
    {
        public XboxDrive(Stream stream) : base(stream)
        {
            Build();
        }

        public static bool Detect(Stream stream)
        {
            EndianReader reader = new EndianReader(stream);
            stream.Seek(0xABE80000, SeekOrigin.Begin);
            return reader.ReadUInt32() == FileSystem.Constants.VolumeSignature;
        }

        private void Build()
        {
            Name = "Xbox Original HDD";

            CreateFATXPartition("Partition1", 0xABE80000, 0x1312D6000);    // DATA
            CreateFATXPartition("Partition2", 0x8CA80000, 0x1f400000);     // SHELL
            CreateFATXPartition("Partition3", 0x5DC80000, 0x2ee00000);     // CACHE
            CreateFATXPartition("Partition4", 0x2EE80000, 0x2ee00000);     // CACHE
            CreateFATXPartition("Partition5", 0x80000, 0x2ee00000);        // CACHE
        }

        private void CreateFATXPartition(string name, long offset, long length)
        {
            var partition = AddPartition(name, offset, length);

            partition.Volume = new Volume(partition, Platform.Xbox);
        }
    }
}