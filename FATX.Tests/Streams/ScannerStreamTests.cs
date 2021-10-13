using System.IO;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FATX.Streams.Tests
{
    [TestClass]
    public class ScannerStreamTests
    {
        [TestMethod]
        public void TestShift()
        {
            using (MemoryStream stream = CreateMockStream())
            using (ScannerStream scanner = new ScannerStream(stream, 0x100))
            {
                var length = stream.Length;
                for (var i = 0; i < 0xff; i++)
                {
                    scanner.Seek(0, SeekOrigin.Begin);
                    Assert.AreEqual(scanner.Position, 0);
                    Assert.AreEqual(scanner.ReadByte(), i);
                    scanner.Shift(0x100);
                    Assert.AreEqual(scanner.Length, length -= 0x100);
                }
            }
        }

        [TestMethod]
        public void TestSeek()
        {
            using (MemoryStream stream = CreateMockStream())
            using (ScannerStream scanner = new ScannerStream(stream, 0x100))
            {
                // Seek 
                scanner.Seek(0x100, SeekOrigin.Begin);
                Assert.AreEqual(scanner.Position, 0x100);

                scanner.Seek(0x100, SeekOrigin.Current);
                Assert.AreEqual(scanner.Position, 0x200);

                scanner.Seek(-0x100, SeekOrigin.End);
                Assert.AreEqual(scanner.Position, stream.Length - 0x100);
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