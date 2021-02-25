using System.IO;

using FATX.Drive;

namespace FATXTools.DiskTypes
{
    public class RawImage : DiskHandler
    {
        public RawImage(string fileName)
        {
            _stream = new FileStream(fileName, FileMode.Open, FileAccess.Read);

            Drive = DriveFactory.Detect(_stream);
        }
    }
}
