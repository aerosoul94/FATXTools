using System;
using System.Collections.Generic;
using System.Text;

namespace FATX
{
    public abstract class FileSignature
    {
        private Volume _volume;
        private long _offset;
        protected string _fileName;
        protected long _fileSize;



        public FileSignature(Volume volume, long offset)
        {
            this._fileName = null;
            this._fileSize = 0;
            this._offset = offset;
            this._volume = volume;
        }

        public abstract bool Test();

        public abstract void Parse();

        public string FileName
        {
            get { return _fileName; }
        }

        public long FileSize
        {
            get { return _fileSize; }
        }

        public long Offset
        {
            get { return _offset; }
        }

        protected void SetByteOrder(ByteOrder byteOrder)
        {
            _volume.Reader.ByteOrder = byteOrder;
        }

        protected byte[] ReadBytes(int count)
        {
            return _volume.Reader.ReadBytes(count);
        }

        protected char[] ReadChars(int count)
        {
            return _volume.Reader.ReadChars(4);
        }

        protected uint ReadUInt32()
        {
            return _volume.Reader.ReadUInt32();
        }
    }
}
