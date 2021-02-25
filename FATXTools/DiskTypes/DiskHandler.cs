using System.IO;

using FATX.Drive;

namespace FATXTools.DiskTypes
{
    public abstract class DiskHandler
    {
        protected Stream _stream;

        public XDrive Drive { get; protected set; }
    }
}
