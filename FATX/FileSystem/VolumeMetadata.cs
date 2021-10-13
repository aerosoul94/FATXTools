using System;
using System.IO;

namespace FATX.FileSystem
{
    public class VolumeMetadata
    {
        readonly Stream _stream;
        readonly Platform _platform;

        public uint Signature { get; private set; }
        public uint SerialNumber { get; private set; }
        public uint SectorsPerCluster { get; private set; }
        public uint RootDirFirstCluster { get; private set; }

        public VolumeMetadata(Stream stream, Platform platform)
        {
            this._stream = stream;
            this._platform = platform;

            Read();
        }

        public void Read()
        {
            _stream.Seek(0, SeekOrigin.Begin);

            var header = new byte[0x10];
            _stream.Read(header, 0, 0x10);

            if (_platform == Platform.X360)
            {
                Array.Reverse(header, 0, 4);
                Array.Reverse(header, 4, 4);
                Array.Reverse(header, 8, 4);
                Array.Reverse(header, 12, 4);
            }

            Signature = BitConverter.ToUInt32(header, 0);
            SerialNumber = BitConverter.ToUInt32(header, 4);
            SectorsPerCluster = BitConverter.ToUInt32(header, 8);
            RootDirFirstCluster = BitConverter.ToUInt32(header, 12);

            if (Signature != Constants.VolumeSignature)
            {
                throw new FormatException("Invalid FATX Signature");
            }
        }
    }
}