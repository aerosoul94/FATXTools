using System.IO;
using System.Text;

using FATX.Streams;

namespace FATX.Analyzers.Signatures
{
    public class XBESignature : IFileSignature
    {
        public string Name => "XBE";

        private static readonly string XBEMagic = "XBEH";

        public bool Test(CarverReader reader)
        {
            byte[] magic = reader.ReadBytes(4);
            return Encoding.ASCII.GetString(magic) == XBEMagic;
        }

        public void Parse(CarverReader reader, CarvedFile carvedFile)
        {
            reader.Seek(0x104);
            var baseAddress = reader.ReadUInt32();
            reader.Seek(0x10C);
            carvedFile.FileSize = reader.ReadUInt32();
            reader.Seek(0x150);
            var debugFileNameOffset = reader.ReadUInt32();
            reader.Seek(debugFileNameOffset - baseAddress);
            var debugFileName = reader.ReadCString();
            carvedFile.FileName = Path.ChangeExtension(debugFileName, ".xbe");
        }
    }
}