using System.IO;
using System.Linq;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FATX.Streams.Tests
{
    [TestClass]
    public class AlignedStreamTests
    {
        [TestMethod]
        public void TestRead()
        {
            using (MemoryStream memory = CreateMockStream())
            using (AlignedStream stream = new AlignedStream(memory, memory.Length, 0x100))
            {
                byte[] expected1 = new byte[0x80];
                for (var i = 0; i < 0x80; i++)
                    expected1[i] = i < 0x40 ? (byte)0x1 : (byte)0x2;
                byte[] array1 = new byte[0x80];
                stream.Seek(0x1C0, SeekOrigin.Begin);
                stream.Read(array1, 0, 0x80);
                Assert.IsTrue(Enumerable.SequenceEqual(array1, expected1));

                byte[] expected2 = new byte[0x40];
                for (var i = 0; i < 0x40; i++)
                    expected2[i] = 0x1;
                byte[] array2 = new byte[0x40];
                stream.Seek(0x1C0, SeekOrigin.Begin);
                stream.Read(array2, 0, 0x40);
                Assert.IsTrue(Enumerable.SequenceEqual(array2, expected2));
            }
        }

        [TestMethod]
        public void TestSeek()
        {
            using (MemoryStream memory = CreateMockStream())
            using (AlignedStream stream = new AlignedStream(memory, memory.Length, 0x2))
            {
                stream.Seek(0x180, SeekOrigin.Begin);
                Assert.AreEqual(stream.Position, 0x180);

                stream.Seek(0x80, SeekOrigin.Current);
                Assert.AreEqual(stream.Position, 0x200);

                stream.Seek(-0x80, SeekOrigin.End);
                Assert.AreEqual(stream.Position, memory.Length - 0x80);
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