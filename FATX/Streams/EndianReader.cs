using System;
using System.IO;

namespace FATX.Streams
{
    public class EndianReader : BinaryReader
    {
        public EndianReader(Stream stream, ByteOrder byteOrder)
            : base(stream, new System.Text.UTF8Encoding(), false)
        {
            ByteOrder = byteOrder;
        }

        public EndianReader(Stream stream)
            : base(stream)
        {
            ByteOrder = ByteOrder.Little;
        }

        public ByteOrder ByteOrder { get; set; }

        public virtual long Seek(long offset, SeekOrigin origin)
        {
            return BaseStream.Seek(offset, origin);
        }

        public override short ReadInt16()
        {
            var temp = new byte[2];
            Read(temp, 0, 2);

            if (ByteOrder == ByteOrder.Big)
                Array.Reverse(temp);

            return BitConverter.ToInt16(temp, 0);
        }

        public override ushort ReadUInt16()
        {
            var temp = new byte[2];
            Read(temp, 0, 2);

            if (ByteOrder == ByteOrder.Big)
                Array.Reverse(temp);

            return BitConverter.ToUInt16(temp, 0);
        }

        public override int ReadInt32()
        {
            var temp = new byte[4];
            Read(temp, 0, 4);

            if (ByteOrder == ByteOrder.Big)
                Array.Reverse(temp);

            return BitConverter.ToInt32(temp, 0);
        }

        public override uint ReadUInt32()
        {
            var temp = new byte[4];
            Read(temp, 0, 4);

            if (ByteOrder == ByteOrder.Big)
                Array.Reverse(temp);

            return BitConverter.ToUInt32(temp, 0);
        }

        public override long ReadInt64()
        {
            var temp = new byte[8];
            Read(temp, 0, 8);

            if (ByteOrder == ByteOrder.Big)
                Array.Reverse(temp);

            return BitConverter.ToInt64(temp, 0);
        }

        public override ulong ReadUInt64()
        {
            var temp = new byte[8];
            Read(temp, 0, 8);

            if (ByteOrder == ByteOrder.Big)
                Array.Reverse(temp);

            return BitConverter.ToUInt64(temp, 0);
        }

        public override float ReadSingle()
        {
            var temp = new byte[4];
            Read(temp, 0, 4);

            if (ByteOrder == ByteOrder.Big)
                Array.Reverse(temp);

            return BitConverter.ToSingle(temp, 0);
        }

        public override double ReadDouble()
        {
            var temp = new byte[8];
            Read(temp, 0, 8);

            if (ByteOrder == ByteOrder.Big)
                Array.Reverse(temp);

            return BitConverter.ToDouble(temp, 0);
        }
    }
}