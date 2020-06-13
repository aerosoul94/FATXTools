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
        public virtual long Seek(long offset)
        {
            BaseStream.Position = offset;
            return BaseStream.Position;
        }
        public virtual long Seek(long offset, SeekOrigin origin)
        {
            return BaseStream.Seek(offset, origin);
        }
        public virtual void Read(byte[] buffer, int count)
        {
            BaseStream.Read(buffer, 0, count);
        }
        public override short ReadInt16()
        {
            var temp = new byte[2];
            Read(temp, 2);
            if (byteOrder == ByteOrder.Big)
            {
                Array.Reverse(temp);
            }
            return BitConverter.ToInt16(temp, 0);
        }
        public override ushort ReadUInt16()
        {
            var temp = new byte[2];
            Read(temp, 2);
            if (byteOrder == ByteOrder.Big)
            {
                Array.Reverse(temp);
            }
            return BitConverter.ToUInt16(temp, 0);
        }
        public override int ReadInt32()
        {
            var temp = new byte[4];
            Read(temp, 4);
            if (byteOrder == ByteOrder.Big)
            {
                Array.Reverse(temp);
            }
            return BitConverter.ToInt32(temp, 0);
        }
        public override uint ReadUInt32()
        {
            var temp = new byte[4];
            Read(temp, 4);
            if (byteOrder == ByteOrder.Big)
            {
                Array.Reverse(temp);
            }
            return BitConverter.ToUInt32(temp, 0);
        }
    }
}
