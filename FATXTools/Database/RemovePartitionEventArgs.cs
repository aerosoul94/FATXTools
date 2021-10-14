namespace FATXTools.Database
{
    public class RemovePartitionEventArgs
    {
        private int _index;

        public RemovePartitionEventArgs(int index)
        {
            _index = index;
        }

        public int Index => _index;
    }
}