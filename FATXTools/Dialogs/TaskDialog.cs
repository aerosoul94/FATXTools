using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

using FATXTools.Utilities;

namespace FATXTools.Dialogs
{
    public partial class TaskDialog : Form
    {
        public string Title { get; private set; }
        public IProgress<ValueTuple<int, string>> Progress { get; private set; }

        Task _task;
        CancellationTokenSource _cancellationTokenSource;

        public TaskDialog(Form owner, TaskDialogOptions options, ref Task task, CancellationTokenSource cancellationTokenSource)
        {
            InitializeComponent();

            Owner = owner;
            Text = options.Title;

            _task = task;
            _cancellationTokenSource = cancellationTokenSource;

            label1.Text = "";
            progressBar1.Maximum = 10000;

            Progress = new Progress<ValueTuple<int, string>>(UpdateProgress);
        }

        private void TaskDialog_FormClosing(object sender, FormClosingEventArgs e)
        {
            _cancellationTokenSource.Cancel();
        }

        private void UpdateProgress(ValueTuple<int, string> progress)
        {
            progressBar1.Value = progress.Item1 * 100;
            label1.Text = progress.Item2;
        }
    }
}
