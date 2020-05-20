

namespace FATX
{
    public static class Constants
    {
        public const uint SectorSize = 0x200;
        public const uint PageSize = 0x1000;
        public const uint ReservedBytes = PageSize;
        public const uint ReservedClusters = 1;

        public const uint DirentNeverUsed = 0x00;
        public const uint DirentNeverUsed2 = 0xFF;
        public const uint DirentDeleted = 0xE5;

        public const uint ClusterAvailable = 0x00000000;
        public const uint ClusterReserved = 0xfffffff0;
        public const uint ClusterBad = 0xfffffff7;
        public const uint ClusterMedia = 0xfffffff8;
        public const uint ClusterLast = 0xffffffff;

        public const uint Cluster16Available = 0x0000;
        public const uint Cluster16Reserved = 0xfff0;
        public const uint Cluster16Bad = 0xfff7;
        public const uint Cluster16Media = 0xfff8;
        public const uint Cluster16Last = 0xffff;
    }
}
