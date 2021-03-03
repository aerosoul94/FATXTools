using System.IO;

using FATX.Drive;

namespace FATX.Devices
{
    public class RawImage : DeviceHandler
    {
        public RawImage(string fileName)
        {
            _stream = new FileStream(fileName, FileMode.Open, FileAccess.Read);

            Drive = DriveFactory.Detect(_stream);
        }
    }
}
