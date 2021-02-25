using Microsoft.Win32.SafeHandles;

using System.IO;

using FATX.Drive;
using FATX.Streams;

namespace FATXTools.DiskTypes
{
    public class PhysicalDisk : DiskHandler
    {
        public PhysicalDisk(SafeFileHandle handle, long length, long sectorLength)
        {
            _stream = new AlignedStream(
                new FileStream(handle, FileAccess.Read), length, (int)sectorLength
            );

            Drive = DriveFactory.Detect(_stream);
        }
    }
}
