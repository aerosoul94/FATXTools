using System;
using System.IO;

namespace FATX.Streams
{
    public class SubStream : Stream
    {
        readonly Stream _parent;
        readonly long _start;
        readonly long _length;
        long _position;

        public SubStream(Stream parent, long start, long length)
        {
            this._parent = parent;
            this._start = start;
            this._length = length;

            if (start > parent.Length || start + length > parent.Length)
            {
                throw new ArgumentException("Invalid offset or length");
            }
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            //Console.WriteLine($"Read: start=0x{_start:X} position=0x{_position:X} offset=0x{offset:X} count={count}");
            _parent.Seek(_start + _position, SeekOrigin.Begin);
            int read = _parent.Read(buffer, offset, count);
            _position += read;
            return read;
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }

        public override bool CanRead => _parent.CanRead;

        public override bool CanWrite => _parent.CanWrite;

        public override bool CanSeek => _parent.CanSeek;

        public override long Position 
        { 
            get => _position; 
            set => _position = value; 
        }

        public override long Length => _length;

        public override long Seek(long offset, SeekOrigin origin)
        {
            var position = _position;
            switch (origin)
            {
                case SeekOrigin.Begin:
                    position = offset;
                    break;
                case SeekOrigin.Current:
                    position += offset;
                    break;
                case SeekOrigin.End:
                    position = _length + offset;
                    break;
            }

            //if (position < 0 || position >= _length)
            //{
            //    throw new ArgumentOutOfRangeException($"Attempted to seek beyond boundaries (offset={offset}, position={position}, length={_length}");
            //}

            _position = position;

            return _position;
        }

        public override void SetLength(long value)
        {
            throw new NotImplementedException();
        }

        public override void Flush()
        {
            throw new NotImplementedException();
        }
    }
}