using Microsoft.VisualStudio.TestTools.UnitTesting;

using System.IO;

namespace FATX.FileSystem.Tests
{
    [TestClass]
    public class FileAllocationTableTests
    {
        [TestMethod]
        public void TestFat16()
        {
            using (MemoryStream stream = new MemoryStream())
            {
                // TODO: Test upper bounds of FAT16
                stream.Write(new byte[0x1000 * 2]);
                var fat = new FileAllocationTable(stream, Platform.Xbox, FatType.Fat16, 0x1000);
                Assert.IsTrue(fat.FatType == FatType.Fat16);
            }
        }

        [TestMethod]
        public void TestFat32()
        {
            using (MemoryStream stream = new MemoryStream())
            {
                // TODO: Test lower bounds of FAT32
                stream.Write(new byte[0x10000 * 4]);
                var fat = new FileAllocationTable(stream, Platform.Xbox, FatType.Fat32, 0x10000);
                Assert.IsTrue(fat.FatType == FatType.Fat32);
            }
        }

        [TestMethod]
        public void TestGetClusterChain()
        {
            using (MemoryStream stream = new MemoryStream(0x10000 * 4))
            using (BinaryWriter writer = new BinaryWriter(stream))
            {
                stream.Seek(4, SeekOrigin.Begin);
                for (var i = 2; i < 8; i++)
                    writer.Write((uint)i);
                writer.Write(Constants.ClusterLast);
                stream.Seek(0, SeekOrigin.Begin);
                var fat = new FileAllocationTable(stream, Platform.Xbox, FatType.Fat32, 0x10000);
                var chain = fat.GetClusterChain(1);
                Assert.AreEqual(7, chain.Count);
            }
        }
    }
}