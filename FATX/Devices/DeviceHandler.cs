using System.IO;

using FATX.Drive;

namespace FATX.Devices
{
    public abstract class DeviceHandler
    {
        protected Stream _stream;

        public XDrive Drive { get; protected set; }
    }
}
