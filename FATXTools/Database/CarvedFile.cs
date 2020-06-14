using FATX.Analyzers.Signatures;

namespace FATXTools.Database
{
    class CarvedFile
    {
        FileSignature signature;

        public CarvedFile(FileSignature signature)
        {
            this.signature = signature;
        }
    }
}
