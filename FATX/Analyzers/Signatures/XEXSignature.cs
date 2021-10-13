using System.IO;
using System.Text;

using FATX.Streams;

namespace FATX.Analyzers.Signatures
{
    public class XEXSignature : IFileSignature
    {
        public string Name => "XEX";

        private static readonly string XEX1Signature = "XEX1";
        private static readonly string XEX2Signature = "XEX2";

        public bool Test(CarverReader reader)
        {
            string magic = Encoding.ASCII.GetString(reader.ReadBytes(4));
            return magic == XEX2Signature || magic == XEX1Signature;
        }

        public void Parse(CarverReader reader, CarvedFile carvedFile)
        {
            reader.Seek(0x10);
            var securityOffset = reader.ReadUInt32();
            var headerCount = reader.ReadUInt32();
            uint fileNameOffset = 0;
            for (int i = 0; i < headerCount; i++)
            {
                var xid = reader.ReadUInt32();
                if (xid == 0x000183ff)
                    fileNameOffset = reader.ReadUInt32();
                else
                    reader.ReadUInt32();
            }
            reader.Seek(securityOffset + 4);
            carvedFile.FileSize = reader.ReadUInt32();
            if (fileNameOffset != 0)
            {
                reader.Seek(fileNameOffset + 4);
                carvedFile.FileName = Path.ChangeExtension(reader.ReadCString(), ".xex");
            }
        }
    }
}
