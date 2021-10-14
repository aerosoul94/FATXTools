using System;

namespace FATXTools.Controls
{
    class CellHoveredEventArgs : EventArgs
    {
        public int Index { get; set; }

        public CellHoveredEventArgs(int index)
        {
            Index = index;
        }
    }
}
