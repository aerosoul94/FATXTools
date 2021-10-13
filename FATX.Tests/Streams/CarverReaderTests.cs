using System;
using System.IO;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FATX.Streams.Tests
{
    [TestClass]
    public class CarverReaderTests
    {
        [TestMethod]
        public void TestReadCString()
        {
            using (MemoryStream stream = CreateMockStream())
            using (CarverReader reader = new CarverReader(stream))
            {
                stream.Seek(0, SeekOrigin.Begin);
                var test1 = reader.ReadCString();
                Assert.AreEqual(test1, "test1");
                // An extra test to make sure the stream incremented properly
                var test2 = reader.ReadCString();
                Assert.AreEqual(test2, "test2");
            }
        }

        private MemoryStream CreateMockStream()
        {
            MemoryStream stream = new MemoryStream();

            WriteCString(stream, "test1");
            WriteCString(stream, "test2");

            return stream;
        }

        private void WriteCString(Stream stream, string s)
        {
            foreach (var c in s)
            {
                stream.WriteByte(Convert.ToByte(c));
            }
            stream.WriteByte(0);
        }
    }
}