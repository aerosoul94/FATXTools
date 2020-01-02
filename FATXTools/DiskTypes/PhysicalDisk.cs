using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Win32.SafeHandles;
using FATX;

namespace FATXTools.DiskTypes
{
    public class PhysicalDisk : DriveReader
    {
        private long _length;
        public PhysicalDisk(SafeFileHandle handle, long length)
            : base(new FileStream(handle, FileAccess.Read))
        {
            this._length = length;
            this.Initialize();
        }

        public override long Length
        {
            get { return _length; }
        }
    }
}
