using FATX.FileSystem;
using System;
using System.Collections.Generic;
using System.Text;

namespace FATXTools.Database
{
    public class AddPartitionEventArgs
    {
        public Volume Volume;

        public AddPartitionEventArgs(Volume volume)
        {
            this.Volume = volume;
        }
    }
}
