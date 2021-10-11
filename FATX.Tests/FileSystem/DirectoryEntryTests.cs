using Microsoft.VisualStudio.TestTools.UnitTesting;

using System;
using System.IO;
using System.Text;

namespace FATX.FileSystem.Tests
{
    [TestClass]
    public class DirectoryEntryTests
    {
        [TestMethod]
        public void TestDirectoryEntryIsFile()
        {
            var data = CreateMockDirectoryEntry(true).ToArray();
            var dirent = new DirectoryEntry(Platform.Xbox, data, 0);
            Assert.IsTrue(dirent.IsFile());
            Assert.IsFalse(dirent.IsDirectory());
        }

        [TestMethod]
        public void TestDirectoryEntryIsDirectory()
        {
            var data = CreateMockDirectoryEntry(false).ToArray();
            var dirent = new DirectoryEntry(Platform.Xbox, data, 0);
            Assert.IsFalse(dirent.IsFile());
            Assert.IsTrue(dirent.IsDirectory());
        }

        [TestMethod]
        public void TestDirectoryEntryFields()
        {
            var data = CreateMockDirectoryEntry(true).ToArray();
            var dirent = new DirectoryEntry(Platform.Xbox, data, 0);
            Assert.AreEqual(0x4, dirent.FileNameLength);
            Assert.AreEqual("test", dirent.FileName);
            Assert.AreEqual((UInt32)0x123, dirent.FirstCluster);
            Assert.AreEqual((UInt32)0x456, dirent.FileSize);
        }

        [TestMethod]
        public void TestDirectoryEntryHierarchy()
        {

        }

        public MemoryStream CreateMockDirectoryEntry(bool isFile)
        {
            MemoryStream stream = new MemoryStream();
            stream.WriteByte(0x4);                          // FileNameLength
            stream.WriteByte(isFile ? (byte)0x00 : (byte)FileAttribute.Directory);         // FileAttributes
            stream.Write(Encoding.ASCII.GetBytes("test"));  // FileName
            for (var i = 0; i < 42 - 4; i++)
                stream.WriteByte(0xff);
            stream.Write(BitConverter.GetBytes((Int32)0x123));  // FirstCluster
            stream.Write(BitConverter.GetBytes((Int32)0x456));  // FileSize
            stream.Write(BitConverter.GetBytes((Int32)0));      // CreationTime
            stream.Write(BitConverter.GetBytes((Int32)0));      // LastWriteTime
            stream.Write(BitConverter.GetBytes((Int32)0));      // LastAccessTime
            return stream;
        }
    }
}
