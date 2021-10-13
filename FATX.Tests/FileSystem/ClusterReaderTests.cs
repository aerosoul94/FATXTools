using System.IO;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FATX.FileSystem.Tests
{
    [TestClass]
    public class ClusterReaderTests
    {
        [TestMethod]
        public void TestReadCluster()
        {
            using (MemoryStream stream = CreateMockStream())
            {
                // Cluster indexes should be off by 1
                ClusterReader reader = new ClusterReader(stream, 0x100);
                byte[] cluster1 = reader.ReadCluster(1);
                Assert.AreEqual(0, cluster1[0]);
                byte[] cluster2 = reader.ReadCluster(2);
                Assert.AreEqual(1, cluster2[0]);
            }
        }

        [TestMethod]
        public void TestClusterToPhysicalOffset()
        {
            using (MemoryStream stream = CreateMockStream())
            {
                ClusterReader reader = new ClusterReader(stream, 0x100);

                var address = reader.ClusterToPhysicalOffset(1);
                Assert.AreEqual(address, 0);

                address = reader.ClusterToPhysicalOffset(2);
                Assert.AreEqual(address, 0x100);
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