using System.IO;
using System.Text;
using System.Windows.Forms;

namespace FATXTools.Forms
{
    public partial class MainWindow
    {
        public class LogWriter : TextWriter
        {
            private TextBox textBox;
            private delegate void SafeCallDelegate(string text);
            public LogWriter(TextBox textBox)
            {
                this.textBox = textBox;
            }

            public override void Write(char value)
            {
                textBox.Text += value;
            }

            public override void Write(string value)
            {
                textBox.AppendText(value);
            }

            public override void WriteLine()
            {
                textBox.AppendText(NewLine);
            }

            public override void WriteLine(string value)
            {
                if (textBox.InvokeRequired)
                {
                    var d = new SafeCallDelegate(WriteLine);
                    textBox.BeginInvoke(d, new object[] { value });
                }
                else
                {
                    textBox.AppendText(value + NewLine);
                }
            }

            public override Encoding Encoding
            {
                get { return Encoding.ASCII; }
            }
        }
    }
}
