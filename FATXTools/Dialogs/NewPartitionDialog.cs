using System.Windows.Forms;

namespace FATXTools.Dialogs
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
