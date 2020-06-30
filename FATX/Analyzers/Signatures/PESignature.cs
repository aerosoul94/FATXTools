using FATX.FileSystem;
using System.Linq;

namespace FATX.Analyzers.Signatures
{
    class PESignature : FileSignature
    {
        private byte[] PEMagic = new byte[] { 0x4D, 0x5A, 0x90, 0x00 };

        public PESignature(Volume volume, long offset)
            : base(volume, offset)
        {

        }

        public override bool Test()
        {
            byte[] magic = ReadBytes(4);
            if (magic.SequenceEqual(PEMagic))
            {
                return true;
            }

            return false;
        }

        public override void Parse()
        {
            SetByteOrder(ByteOrder.Little);
            Seek(0x3C);
            var lfanew = ReadUInt32();
            Seek(lfanew);
            var sign = ReadUInt32();
            if (sign != 0x00004550)
            {
                return;
            }
            Seek(lfanew + 0x6);
            var nsec = ReadUInt16();
            var lastSecOff = (lfanew + 0xF8) + ((nsec - 1) * 0x28);
            Seek(lastSecOff + 0x10);
            var secLen = ReadUInt32();
            Seek(lastSecOff + 0x14);
            var secOff = ReadUInt32();
            this.FileSize = secOff + secLen;
        }
    }
}
