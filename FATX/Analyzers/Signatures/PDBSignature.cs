using System.Text;

using FATX.Streams;

namespace FATX.Analyzers.Signatures
{
    public class PDBSignature : IFileSignature
    {
        public string Name => "PDB";

        private static readonly string PDBMagic = "Microsoft C/C++ MSF 7.00\r\n\x1A\x44\x53\0\0\0";

        public bool Test(CarverReader reader)
        {
            byte[] magic = reader.ReadBytes(0x20);

            return Encoding.ASCII.GetString(magic) == PDBMagic;
        }

        public void Parse(CarverReader reader, CarvedFile carvedFile)
        {
            reader.ByteOrder = ByteOrder.Little;
            reader.Seek(0x20);
            var blockSize = reader.ReadUInt32();
            reader.Seek(0x28);
            var numBlocks = reader.ReadUInt32();
            carvedFile.FileSize = blockSize * numBlocks;
        }
    }
}