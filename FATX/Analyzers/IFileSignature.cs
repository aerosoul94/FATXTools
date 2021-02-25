using FATX.Streams;

namespace FATX.Analyzers
{
    public interface IFileSignature
    {
        string Name { get; }
        bool Test(CarverReader reader);
        void Parse(CarverReader reader, CarvedFile carvedFile);
    }
}