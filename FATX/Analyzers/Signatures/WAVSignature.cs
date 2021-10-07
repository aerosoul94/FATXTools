using System.Text;

using FATX.Streams;

namespace FATX.Analyzers.Signatures
{
    public class WAVSignature : IFileSignature
    {
        public string Name => "WAV";

        private static readonly string RIFFMagic = "RIFF";
        private static readonly string WAVEMagic = "WAVEfmt";

        public bool Test(CarverReader reader)
        {
            byte[] riffMagic = reader.ReadBytes(4);
            reader.Seek(0x8);
            byte[] waveMagic = reader.ReadBytes(7);
            return Encoding.ASCII.GetString(riffMagic) == RIFFMagic &&
                Encoding.ASCII.GetString(waveMagic) == WAVEMagic;
        }

        public void Parse(CarverReader reader, CarvedFile carvedFile)
        {
            reader.Seek(0x4);
            carvedFile.FileSize = reader.ReadUInt32() + 8;
        }
    }
}
