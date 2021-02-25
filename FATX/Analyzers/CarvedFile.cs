namespace FATX.Analyzers
{
    public class CarvedFile
    {
        public string Type { get; private set; }
        public long Offset { get; private set; }
        public string FileName { get; set; }
        public long   FileSize { get; set; }

        public CarvedFile(long offset, string type)
        {
            Type = type;
            Offset = offset;
        }
    }
}