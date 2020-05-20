using System.IO;
using System.Text;

namespace FATX.Analyzers.Signatures
{
    class XEXSignature : FileSignature
    {
        private const string XEX1Signature = "XEX1";
        private const string XEX2Signature = "XEX2";

        public XEXSignature(Volume volume, long offset)
            : base(volume, offset)
        {

        }

        public override bool Test()
        {
            byte[] magic = this.ReadBytes(4);
            if (Encoding.UTF8.GetString(magic) == XEX2Signature)
            {
                return true;
            }

            return false;
        }

        public override void Parse()
        {
            Seek(0x10);
            var securityOffset = ReadUInt32();
            var headerCount = ReadUInt32();
            uint fileNameOffset = 0;
            for (int i = 0; i < headerCount; i++)
            {
                var xid = ReadUInt32();
                if (xid == 0x000183ff)
                {
                    fileNameOffset = ReadUInt32();
                }
                else
                {
                    ReadUInt32();
                }
            }
            Seek(securityOffset + 4);
            this.FileSize = ReadUInt32();
            if (fileNameOffset != 0)
            {
                Seek(fileNameOffset + 4);
                this.FileName = Path.ChangeExtension(ReadCString(), ".xex");
            }
        }
    }
}
