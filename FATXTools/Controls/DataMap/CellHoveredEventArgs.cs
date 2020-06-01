using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FATXTools.Controls
{
    class CellHoveredEventArgs : EventArgs
    {
        public int Index { get; set; }

        public CellHoveredEventArgs(int index)
        {
            this.Index = index;
        }
    }
}
