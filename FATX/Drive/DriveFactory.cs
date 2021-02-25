using System.IO;

namespace FATX.Drive
{
    public class DriveFactory
    {
        public static XDrive Detect(Stream stream)
        {
            stream.Seek(0, SeekOrigin.Begin);
            if (XboxDrive.Detect(stream))
            {
                return new XboxDrive(stream);
            }
            
            stream.Seek(0, SeekOrigin.Begin);
            if (Xbox360Drive.Detect(stream))
            {
                return new Xbox360Drive(stream);
            }

            return null;
        }
    }
}