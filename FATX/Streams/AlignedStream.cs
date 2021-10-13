using System;
using System.IO;

namespace FATX.Streams
{
    public class AlignedStream : Stream
    {
        readonly Stream _stream;
        readonly int _alignment;
        readonly long _length;
        long _position;

        public AlignedStream(Stream stream, long length, int alignment)
        {
            _stream = stream;
            _length = length;
            _position = 0;
            _alignment = alignment;
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            // Round down the position
            var position = _position + offset;
            if (_position % _alignment != 0)
            {
                position -= _position % _alignment;
            }

            _stream.Position = position;

            // Round up the count to alignment
            var end = ((_position + count) + (_alignment - 1)) / _alignment * _alignment; 
            var alignedCount = end - position;

            var tempBuf = new byte[alignedCount];
            var read = _stream.Read(tempBuf, 0, (int)alignedCount);

            Buffer.BlockCopy(tempBuf, (int)(_position % _alignment), buffer, 0, count);

            _position += count;
            return read;
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }

        public override bool CanRead => _stream.CanRead;

        public override bool CanWrite => _stream.CanWrite;

        public override bool CanSeek => _stream.CanSeek;

        public override long Position 
        { 
            get => _position;
            set => _position = value; 
        }

        public override long Length => _length;

        public override long Seek(long offset, SeekOrigin origin)
        {
            switch (origin)
            {
                case SeekOrigin.Begin:
                    _position = offset;
                    break;
                case SeekOrigin.Current:
                    _position += offset;
                    break;
                case SeekOrigin.End:
                    _position = _length + offset;
                    break;
            }

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