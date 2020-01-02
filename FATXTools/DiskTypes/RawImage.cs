using System;
using System.IO;
using FATX;

namespace FATXTools.DiskTypes
{
    public class RawImage : FATX.DriveReader
    {
        // TODO: replace with FileStream to be able to use "using"
        public RawImage(string fileName)
            : base(new FileStream(fileName,FileMode.Open, FileAccess.Read))
        {
            base.Initialize();
        }
    }
}
