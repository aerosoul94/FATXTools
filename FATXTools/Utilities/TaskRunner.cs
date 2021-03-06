﻿using FATXTools.Dialogs;
using System;
using System.Media;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FATXTools.Utilities
{
    public class TaskRunner
    {
        Form _owner;
        Task _task;
        ProgressDialog _progressDialog;

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

        public event EventHandler TaskStarted;

        public event EventHandler TaskCompleted;

        public async Task RunTaskAsync(string title, Action<CancellationToken, IProgress<int>> task, Action<int> progressUpdate, Action taskCompleted)
        {
            if (_task != null)
            {
                throw new Exception("A task is already running.");
            }

            cancellationTokenSource = new CancellationTokenSource();
            cancellationToken = cancellationTokenSource.Token;

            TaskStarted?.Invoke(this, null);

            _progressDialog = new ProgressDialog(this, _owner, $"Task - {title}", Maximum, Interval);
            _progressDialog.Show();

            var progress = new Progress<int>(percent =>
            {
                progressUpdate(percent);
            });

            try
            {
                _task = Task.Run(() =>
                {
                    task(cancellationToken, progress);
                }, cancellationToken);

                // wait for worker task to finish.
                await _task;
            }
            catch (TaskCanceledException)
            {
                Console.WriteLine("Task cancelled.");
            }
            catch (Exception exception)
            {
                MessageBox.Show(exception.Message);
            }

            SystemSounds.Beep.Play();

            taskCompleted();

            _progressDialog.Close();

            TaskCompleted?.Invoke(this, null);

            _progressDialog = null;
            _task = null;
        }

        public bool CancelTask()
        {
            cancellationTokenSource.Cancel();

            return (_task == null) || _task.IsCompleted;
        }

        public void UpdateProgress(long newValue)
        {
            _progressDialog.UpdateProgress(newValue);
        }

        public void UpdateLabel(string newLabel)
        {
            _progressDialog.UpdateLabel(newLabel);
        }
    }
}
