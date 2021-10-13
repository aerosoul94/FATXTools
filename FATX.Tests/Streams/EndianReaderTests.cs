using System.IO;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FATX.Streams.Tests
{
    [TestClass]
    public class EndianReaderTests
    {
        [TestMethod]
        public void TestReadInt16()
        {
            using (MemoryStream stream = new MemoryStream(new byte[] { 0x12, 0x34 }))
            using (EndianReader reader = new EndianReader(stream))
            Assert.AreEqual(reader.ReadInt16(), 0x3412);

            using (MemoryStream stream = new MemoryStream(new byte[] { 0x12, 0x34 }))
            using (EndianReader reader = new EndianReader(stream, ByteOrder.Big))
            Assert.AreEqual(reader.ReadInt16(), 0x1234);
            
            using (MemoryStream stream = new MemoryStream(new byte[] { 0x12, 0x34 }))
            using (EndianReader reader = new EndianReader(stream))
            Assert.AreEqual(reader.ReadUInt16(), 0x3412);

            using (MemoryStream stream = new MemoryStream(new byte[] { 0x12, 0x34 }))
            using (EndianReader reader = new EndianReader(stream, ByteOrder.Big))
            Assert.AreEqual(reader.ReadUInt16(), 0x1234);
        }

        [TestMethod]
        public void TestReadInt32()
        {
            using (MemoryStream stream = new MemoryStream(new byte[] { 0x12, 0x34, 0x56, 0x78 }))
            using (EndianReader reader = new EndianReader(stream))
            Assert.AreEqual(reader.ReadInt32(), 0x78563412);
            
            using (MemoryStream stream = new MemoryStream(new byte[] { 0x12, 0x34, 0x56, 0x78 }))
            using (EndianReader reader = new EndianReader(stream, ByteOrder.Big))
            Assert.AreEqual(reader.ReadInt32(), 0x12345678);

            using (MemoryStream stream = new MemoryStream(new byte[] { 0x12, 0x34, 0x56, 0x78 }))
            using (EndianReader reader = new EndianReader(stream))
            Assert.AreEqual(reader.ReadUInt32(), (uint)0x78563412);
            
            using (MemoryStream stream = new MemoryStream(new byte[] { 0x12, 0x34, 0x56, 0x78 }))
            using (EndianReader reader = new EndianReader(stream, ByteOrder.Big))
            Assert.AreEqual(reader.ReadUInt32(), (uint)0x12345678);
        }
    }
}