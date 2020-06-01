using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FATXTools.Controls
{
    public class DataMapCell
    {
        public Rectangle Rect { get; set; }

        public Color Color { get; set; }

        public bool Selected { get; set; }

        public int Index { get; set; }

        public DataMapCell()
        {
            Rect = new Rectangle();
            Color = Color.White;
            Selected = false;
        }
    }
}
