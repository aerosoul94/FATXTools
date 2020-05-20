using System;
using System.Collections.Generic;
using System.IO;

namespace FATX.Analyzers.Signatures
{
    public abstract class FileSignature
    {
        private Volume _volume;
        private long _offset;
        private long _fileSize;
        private string _fileName;

        private static Dictionary<string, int> _counters = new Dictionary<string, int>();

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
            get
            {
                if (_fileName == null)
                {
                    if (!_counters.ContainsKey(this.GetType().Name))
                    {
                        _counters[this.GetType().Name] = 1;
                    }
                    _fileName = this.GetType().Name + (_counters[this.GetType().Name]++).ToString();
                }

                return _fileName;
            }
            set => _fileName = value;
        }

        public long FileSize
        {
            get => _fileSize;
            set => _fileSize = value;
        }

        public long Offset
        {
            get => _offset;
            set => _offset = value;
        }

        protected void Seek(long offset, SeekOrigin origin = SeekOrigin.Begin)
        {
            offset += this._offset;
            _volume.SeekFileArea(offset, origin);
        }

        protected void SetByteOrder(ByteOrder byteOrder)
        {
            _volume.Reader.ByteOrder = byteOrder;
        }

        protected byte[] ReadBytes(int count)
        {
            return _volume.Reader.ReadBytes(count);
        }

        protected char ReadChar()
        {
            return _volume.Reader.ReadChar();
        }

        protected char[] ReadChars(int count)
        {
            return _volume.Reader.ReadChars(4);
        }

        protected ushort ReadUInt16()
        {
            return _volume.Reader.ReadUInt16();
        }

        protected uint ReadUInt32()
        {
            return _volume.Reader.ReadUInt32();
        }

        protected string ReadCString()
        {
            String s = String.Empty;
            while (true)
            {
                char c = ReadChar();
                if (c == '\0')
                {
                    return s;
                }
                s += c;
            }
        }
    }
}
