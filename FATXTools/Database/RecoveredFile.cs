using FATX;
using System.Collections.Generic;

namespace FATXTools.Database
{
    public class RecoveredFile
    {
        private bool deleted;
        private int ranking;
        private DirectoryEntry dirent;
        private List<uint> collisions;
        private List<uint> clusterChain;

        public RecoveredFile(DirectoryEntry dirent, bool deleted)
        {
            this.deleted = deleted;
            this.dirent = dirent;
            this.clusterChain = null;
        }

        public void GiveRanking(int rank)
        {
            this.ranking = rank;
        }

        public DirectoryEntry GetDirent()
        {
            return dirent;
        }

        public bool IsDeleted
        {
            get => deleted;
        }

        public int Ranking
        {
            get => ranking;
            set => ranking = value;
        }

        public List<uint> Collisions
        {
            get => collisions;
            set => collisions = value;
        }

        public List<uint> ClusterChain
        {
            get => clusterChain;
            set => clusterChain = value;
        }
    }
}
