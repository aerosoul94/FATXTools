using System;
using System.Collections.Generic;
using System.IO;

namespace FATX.Analyzers.Signatures
{
    public abstract class FileSignature
    {
        private Volume _volume;
        private DriveReader _reader;
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
            this._reader = volume.GetReader();
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
            _reader.ByteOrder = byteOrder;
        }

        protected byte[] ReadBytes(int count)
        {
            return _reader.ReadBytes(count);
        }

        protected byte ReadByte()
        {
            return _reader.ReadByte();
        }

        protected ushort ReadUInt16()
        {
            return _reader.ReadUInt16();
        }

        protected uint ReadUInt32()
        {
            return _reader.ReadUInt32();
        }

        protected String ReadCString(int terminant = 0)
        {
            String tempString = String.Empty;
            Int32 tempChar = -1;
            bool eof;

            while (!(eof = (_reader.Position == _reader.Length)) && (tempChar = ReadByte()) != terminant)
            {
                tempString += Convert.ToChar(tempChar);
                if (eof)
                {
                    tempString += '\0';
                    break;
                }
            }

            return tempString;
        }
    }
}
