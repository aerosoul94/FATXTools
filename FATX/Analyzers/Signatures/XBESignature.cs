using System.IO;
using System.Text;

namespace FATX.Analyzers.Signatures
{
    class XBESignature : FileSignature
    {
        private const string XBEMagic = "XBEH";

        public XBESignature(Volume volume, long offset)
            : base(volume, offset)
        {

        }

        public override bool Test()
        {
            byte[] magic = this.ReadBytes(4);
            if (Encoding.UTF8.GetString(magic) == XBEMagic)
            {
                return true;
            }

            return false;
        }

        public override void Parse()
        {
            Seek(0x104);
            var baseAddress = ReadUInt32();
            Seek(0x10C);
            this.FileSize = ReadUInt32();
            Seek(0x150);
            var debugFileNameOffset = ReadUInt32();
            Seek(debugFileNameOffset - baseAddress);
            var debugFileName = ReadCString();
            this.FileName = Path.ChangeExtension(debugFileName, ".xbe");
        }
    }
}
