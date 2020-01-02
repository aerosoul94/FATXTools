using System;
using System.IO;
using FATX;

namespace FATXTools.DiskTypes
{
    public class CompressedImage : FATX.DriveReader
    {
        public CompressedImage(string fileName)
            : base(new FileStream(fileName, FileMode.Open, FileAccess.Read))
        {
            // Verify IMGC header

            // Pre-load blocks

            //base.Initialize();
        }
    }
}
