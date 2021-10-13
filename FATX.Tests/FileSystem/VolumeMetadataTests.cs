using System;
using System.IO;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FATX.FileSystem.Tests
{
    [TestClass]
    public class VolumeMetadataTests
    {
        [TestMethod]
        public void TestXbox360VolumeMetadata()
        {
            var stream = CreateMockVolumeMetadata(Platform.X360);
            stream.Seek(0, SeekOrigin.Begin);
            var metadata = new VolumeMetadata(stream, Platform.X360);
        }

        [TestMethod]
        public void TestXboxVolumeMetadata()
        {
            var stream = CreateMockVolumeMetadata(Platform.Xbox);
            stream.Seek(0, SeekOrigin.Begin);
            var metadata = new VolumeMetadata(stream, Platform.Xbox);
        }

        private MemoryStream CreateMockVolumeMetadata(Platform platform)
        {
            MemoryStream stream = new MemoryStream();

            var signature = BitConverter.GetBytes(Constants.VolumeSignature);
            var serialNumber = BitConverter.GetBytes((uint)0);
            var sectorsPerCluster = BitConverter.GetBytes((uint)0x20);
            var rootDirFirstCluster = BitConverter.GetBytes((uint)1);

            if (platform == Platform.X360)
            {
                Array.Reverse(signature);
                Array.Reverse(serialNumber);
                Array.Reverse(sectorsPerCluster);
                Array.Reverse(rootDirFirstCluster);
            }
            
            stream.Write(signature);
            stream.Write(serialNumber);
            stream.Write(sectorsPerCluster);
            stream.Write(rootDirFirstCluster);

            return stream;
        }
    }
}