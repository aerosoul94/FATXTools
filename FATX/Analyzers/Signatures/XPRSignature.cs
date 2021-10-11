using System.IO;
using System.Text;

using FATX.Streams;

namespace FATX.Analyzers.Signatures
{
    public class XPRSignature : IFileSignature
    {
        public string Name => "XPR";

        private static readonly string XPRMagic = "XPR0";

        public bool Test(CarverReader reader)
        {
            byte[] magic = reader.ReadBytes(4);
            return Encoding.ASCII.GetString(magic) == XPRMagic;
        }

        public void Parse(CarverReader reader, CarvedFile carvedFile)
        {
            reader.Seek(0x4);
            carvedFile.FileSize = reader.ReadUInt32();
        }
    }
}
