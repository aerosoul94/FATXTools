using System;
using System.Linq;

using FATX.Streams;

namespace FATX.Analyzers.Signatures
{
    public class PESignature : IFileSignature
    {
        public string Name => "PE";

        private static readonly byte[] PEMagic = new byte[] { 0x4D, 0x5A, 0x90, 0x00 };

        public bool Test(CarverReader reader)
        {
            byte[] magic = reader.ReadBytes(4);
            return magic.SequenceEqual(PEMagic);
        }

        public void Parse(CarverReader reader, CarvedFile carvedFile)
        {
            reader.ByteOrder = ByteOrder.Little;
            reader.Seek(0x3C);
            var lfanew = reader.ReadUInt32();
            reader.Seek(lfanew);
            var sign = reader.ReadUInt32();
            if (sign != 0x00004550)
                return;
            reader.Seek(lfanew + 0x6);
            var nsec = reader.ReadUInt16();
            var lastSecOff = (lfanew + 0xF8) + ((nsec - 1) * 0x28);
            reader.Seek(lastSecOff + 0x10);
            var secLen = reader.ReadUInt32();
            reader.Seek(lastSecOff + 0x14);
            var secOff = reader.ReadUInt32();
            carvedFile.FileSize = secOff + secLen;
        }
    }
}
