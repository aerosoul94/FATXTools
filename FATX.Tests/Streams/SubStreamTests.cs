using Microsoft.VisualStudio.TestTools.UnitTesting;

using System;
using System.IO;
using System.Linq;

namespace FATX.Streams.Tests
{
    [TestClass]
    public class SubStreamTests
    {
        [TestMethod]
        public void TestRead()
        {
            // Test
            using (MemoryStream memory = CreateMockStream())
            using (SubStream stream = new SubStream(memory, 0x100, 0x200))
            {
                // Try reading from the start
                byte[] expected = new byte[0x200];
                for (var i = 0; i < 0x200; i++)
                    expected[i] = i < 0x100 ? (byte)0x1 : (byte)0x2;
                stream.Seek(0, SeekOrigin.Begin);
                byte[] array = new byte[0x200];
                stream.Read(array, 0, 0x200);
                Assert.IsTrue(Enumerable.SequenceEqual(array, expected));
            }
        }
        
        [TestMethod]
        public void TestSeek()
        {
            using (MemoryStream memory = CreateMockStream())
            using (SubStream stream = new SubStream(memory, 0x10, 0x10))
            {
                //Assert.ThrowsException<ArgumentOutOfRangeException>(() => 
                //{
                //    stream.Seek(-0x80, SeekOrigin.Begin);
                //});

                //Assert.ThrowsException<ArgumentOutOfRangeException>(() => 
                //{
                //    stream.Seek(0x80, SeekOrigin.End);
                //});
            }
        }

        private MemoryStream CreateMockStream()
        {
            MemoryStream stream = new MemoryStream();

            for (var x = 0; x < 0xff; x++)
            {
                for (var y = 0; y < 0x100; y++)
                {
                    stream.Write(new byte[] { (byte)x });
                }
            }

            return stream;
        }
    }
}