using System;
using System.IO;

namespace FATX
{
    public enum ByteOrder
    {
        Big,
        Little
    }
    public class EndianReader : BinaryReader
    {
        private ByteOrder byteOrder;
        public EndianReader(Stream stream, ByteOrder byteOrder)
            : base(stream)
        {
            this.byteOrder = byteOrder;
        }
        public EndianReader(Stream stream)
            : base(stream)
        {
            this.byteOrder = ByteOrder.Little;
        }
        public ByteOrder ByteOrder
        {
            get { return this.byteOrder; }
            set { this.byteOrder = value; }
        }
        public virtual long Length
        {
            get { return BaseStream.Length; }
        }
        public virtual long Position
        {
            get { return BaseStream.Position; }
        }
        public long Seek(long offset)
        {
            BaseStream.Position = offset;
            return BaseStream.Position;
        }
        public long Seek(long offset, SeekOrigin origin)
        {
            return BaseStream.Seek(offset, origin);
        }
        public void Read(byte[] buffer, int count)
        {
            BaseStream.Read(buffer, 0, count);
        }
        public override short ReadInt16()
        {
            var value = base.ReadInt16();
            if (byteOrder == ByteOrder.Big)
            {
                var bytes = BitConverter.GetBytes(value);
                Array.Reverse(bytes);
                value = BitConverter.ToInt16(bytes, 0);
            }
            return value;
        }
        public override ushort ReadUInt16()
        {
            var value = base.ReadUInt16();
            if (byteOrder == ByteOrder.Big)
            {
                var bytes = BitConverter.GetBytes(value);
                Array.Reverse(bytes);
                value = BitConverter.ToUInt16(bytes, 0);
            }
            return value;
        }
        public override int ReadInt32()
        {
            var value = base.ReadInt32();
            if (byteOrder == ByteOrder.Big)
            {
                var bytes = BitConverter.GetBytes(value);
                Array.Reverse(bytes);
                value = BitConverter.ToInt32(bytes, 0);
            }
            return value;
        }
        public override uint ReadUInt32()
        {
            var value = base.ReadUInt32();
            if (byteOrder == ByteOrder.Big)
            {
                var bytes = BitConverter.GetBytes(value);
                Array.Reverse(bytes);
                value = BitConverter.ToUInt32(bytes, 0);
            }
            return value;
        }
    }
}
