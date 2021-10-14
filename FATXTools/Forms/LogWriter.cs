using System.IO;
using System.Text;
using System.Windows.Forms;

namespace FATXTools.Forms
{
    public partial class MainWindow
    {
        public class LogWriter : TextWriter
        {
            private TextBox _textBox;
            private delegate void SafeCallDelegate(string text);

            public LogWriter(TextBox textBox)
            {
                _textBox = textBox;
            }

            public override void Write(char value)
            {
                _textBox.Text += value;
            }

            public override void Write(string value)
            {
                _textBox.AppendText(value);
            }

            public override void WriteLine()
            {
                _textBox.AppendText(NewLine);
            }

            public override void WriteLine(string value)
            {
                if (_textBox.InvokeRequired)
                {
                    var d = new SafeCallDelegate(WriteLine);
                    _textBox.BeginInvoke(d, new object[] { value });
                }
                else
                {
                    _textBox.AppendText(value + NewLine);
                }
            }

            public override Encoding Encoding
            {
                get { return Encoding.ASCII; }
            }
        }
    }
}
