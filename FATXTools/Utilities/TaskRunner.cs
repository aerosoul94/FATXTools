using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FATXTools.Utilities
{
    public class TaskRunner
    {
        Form _owner;
        Task _task;
        ProgressDialog _progress;

        CancellationToken cancellationToken;
        CancellationTokenSource cancellationTokenSource;

        public TaskRunner(Form owner)
        {
            _owner = owner;
        }

        public long Maximum
        {
            get;
            set;
        }

        public long Interval
        {
            get;
            set;
        }

        public async Task RunTaskAsync(string title, Action<CancellationToken> task, Action progressUpdate, Action taskCompleted)
        {
            if (_task != null)
            {
                throw new Exception("A task is already running.");
            }

            cancellationTokenSource = new CancellationTokenSource();
            cancellationToken = cancellationTokenSource.Token;

            _progress = new ProgressDialog(this, _owner, $"Task - {title}", Maximum, Interval);
            _progress.Show();

            _task = Task.Run(() =>
            {
                task(cancellationToken);
            }, cancellationToken);

            var progressTask = Task.Run(() =>
            {
                // This should only run as long as the task is running
                while (_task != null && _task.Status == TaskStatus.Running)
                {
                    Thread.Sleep(100);
                    _owner.BeginInvoke(progressUpdate);
                }
            });

            // wait for main task and progress task to finish.
            await Task.WhenAll(_task, progressTask);

            taskCompleted();

            _progress.Close();

            _progress = null;
            _task = null;
        }

        public void CancelTask()
        {
            cancellationTokenSource.Cancel();
        }

        public void UpdateProgress(long newValue)
        {
            _progress.UpdateProgress(newValue);
        }

        public void UpdateLabel(string newLabel)
        {
            _progress.UpdateLabel(newLabel);
        }
    }
}
