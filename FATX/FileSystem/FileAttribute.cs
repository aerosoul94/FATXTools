using System;
using System.Collections.Generic;
using System.Text;

namespace FATX.FileSystem
{
    [Flags]
    public enum FileAttribute
    {
        ReadOnly = 0x1,
        Hidden = 0x2,
        System = 0x4,
        Directory = 0x10,
        Archive = 0x20
    }
}
