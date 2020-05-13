using System;
using System.Collections.Generic;
using System.Text;

namespace FATX.Analyzers.Signatures
{
    class PDBSignature : FileSignature
    {
        private const string PDBMagic = "Microsoft C/C++ MSF 7.00\r\n\x1A\x44\x53\0\0\0";

        public PDBSignature(Volume volume, long offset)
            : base(volume, offset)
        {

        }

        public override bool Test()
        {
            byte[] magic = ReadBytes(0x20);
            if (System.Text.Encoding.UTF8.GetString(magic) == PDBMagic)
            {
                return true;
            }

            return false;
        }

        public override void Parse()
        {
            SetByteOrder(ByteOrder.Little);
            Seek(0x20);
            var blockSize = ReadUInt32();
            Seek(0x28);
            var numBlocks = ReadUInt32();
            this.FileSize = blockSize * numBlocks;
        }
    }
}
