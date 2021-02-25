using System.Text;

using FATX.Streams;

namespace FATX.Analyzers.Signatures
{
    public class LiveSignature : IFileSignature
    {
        public string Name => "Live Content";

        private static readonly string LiveMagic = "LIVE";

        public bool Test(CarverReader reader)
        {
            byte[] magic = reader.ReadBytes(4);
            return Encoding.ASCII.GetString(magic) == LiveMagic;
        }

        public void Parse(CarverReader reader, CarvedFile carvedFile)
        {
            // does nothing for now
        }
    }
}
