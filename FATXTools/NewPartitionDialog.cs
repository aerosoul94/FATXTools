using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FATXTools
{
    public partial class NewPartitionDialog : Form
    {
        public NewPartitionDialog()
        {
            InitializeComponent();
        }

        public string PartitionName
        {
            get => textBox1.Text;
        }

        public long PartitionOffset
        {
            get => long.Parse(textBox2.Text, System.Globalization.NumberStyles.HexNumber);
        }

        public long PartitionLength
        {
            get => long.Parse(textBox3.Text, System.Globalization.NumberStyles.HexNumber);
        }

    }
}
