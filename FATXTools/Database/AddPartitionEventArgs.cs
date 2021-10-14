using FATX.Drive;

namespace FATXTools.Database
{
    public class AddPartitionEventArgs
    {
        public Partition Partition;

        public AddPartitionEventArgs(Partition partition)
        {
            Partition = partition;
        }
    }
}
