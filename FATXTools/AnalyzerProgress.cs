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
    public partial class AnalyzerProgress : Form
    {
        private long _maxValue;
        private long _interval;
        public AnalyzerProgress(Form owner, string title, long maxValue, long interval)
        {
            InitializeComponent();

            this.Owner = owner;
            this.Text = title;
            this._interval = interval;
            this._maxValue = maxValue / interval;
            progressBar1.Value = 0;
            progressBar1.Maximum = 10000;
        }

        public void SetText(string text)
        {
            label1.Text = text;
        }

        public void UpdateProgress(long currentValue)
        {
            if (currentValue > _maxValue)
            {
                currentValue = _maxValue;
            }
            var curValue = currentValue;
            var maxValue = _maxValue;
            var percentage = ((float)curValue / (float)maxValue) * 100;
            label1.Text = String.Format("Processing cluster {0}/{1} ({2}%)", curValue, maxValue, (int)percentage);
            var progress = ((float)curValue / (float)maxValue) * 10000;
            progressBar1.Value = (int)progress;
        }
    }
}
