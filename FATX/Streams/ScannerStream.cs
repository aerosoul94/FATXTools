using System;
using System.IO;

namespace FATX.Streams
{
    public class ScannerStream : Stream
    {
        Stream _stream;
        long _start;
        long _position;
        long _length;
        int _bufferSize;
        byte[] _buffer;

        public ScannerStream(Stream stream, int bufferSize)
        {
            _stream = stream;
            _start = 0;
            _position = 0;
            _length = _stream.Length;
            _bufferSize = bufferSize;
            _buffer = new byte[bufferSize];
            _stream.Seek(_start, SeekOrigin.Begin);
            _stream.Read(_buffer, 0, _bufferSize);
        }

        public void Shift(long shift)
        {
            _start += shift;
            _position = 0;
            _length = _stream.Length - _start;
            if (_length >= _bufferSize)
            {
                _stream.Seek(_start, SeekOrigin.Begin);
                _stream.Read(_buffer, 0, _bufferSize);
            }
        }

        public override bool CanRead => _stream.CanRead;

        public override bool CanSeek => _stream.CanSeek;

        public override bool CanWrite => _stream.CanWrite;

        public override long Length => _length;

        public override long Position 
        { 
            get => _position;
            set => _position = value;
        }

        public long Offset => _start;

        public override void Flush()
        {
            throw new NotImplementedException();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            int read;

            if (_position + offset < _bufferSize && _position + offset + count < _bufferSize)
            {
                Buffer.BlockCopy(_buffer, (int)(_position + offset), buffer, offset, count);
                read = count;
            }
            else
            {
                _stream.Seek(_start + _position, SeekOrigin.Begin);
                read = _stream.Read(buffer, offset, count);
            }

            _position += read;

            return read;
        }

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

            _position = position;

            //_stream.Seek(_start + _position, SeekOrigin.Begin);

            return _position;
        }

        public override void SetLength(long value)
        {
            throw new NotImplementedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }
    }
}
