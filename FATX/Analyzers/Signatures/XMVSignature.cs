using System;
using System.IO;
using System.Text;

using FATX.Streams;

namespace FATX.Analyzers.Signatures
{
    public class XMVSignature : IFileSignature
    {
        public string Name => "XMV";

        private static readonly string XMVMagic = "xobX";

        public bool Test(CarverReader reader)
        {
            reader.Seek(0xC);
            byte[] xmvMagic = reader.ReadBytes(4);
            return Encoding.ASCII.GetString(xmvMagic) == XMVMagic;
        }

        public void Parse(CarverReader reader, CarvedFile carvedFile)
        {
            // TODO: arbitrarily large, not sure what the max is (at least 0x2D000)
            const int absoluteMaxPacketSize = 0x80000;
            const int packetAlignmentMask = 0xFFF;

            int nextPacketSize = reader.ReadInt32();
            int thisPacketSize = reader.ReadInt32();
            int maxPacketSize = reader.ReadInt32();

            if ((nextPacketSize & thisPacketSize & maxPacketSize & packetAlignmentMask) > 0 ||
                maxPacketSize > absoluteMaxPacketSize || thisPacketSize > maxPacketSize)
            {
                throw new FormatException("Invalid XMV header.");
            }

            reader.Seek(thisPacketSize);

            while (nextPacketSize > 0)
            {
                thisPacketSize = nextPacketSize;
                nextPacketSize = reader.ReadInt32();

                if ((nextPacketSize & packetAlignmentMask) > 0 ||
                    nextPacketSize > maxPacketSize)
                {
                    throw new FormatException("Invalid XMV stream.");
                }

                reader.Seek(thisPacketSize - 4, SeekOrigin.Current);
            }

            carvedFile.FileSize = reader.BaseStream.Position;
        }
    }
}
