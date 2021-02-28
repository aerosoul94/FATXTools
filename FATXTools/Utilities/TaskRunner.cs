using System;
using System.Media;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

using FATXTools.Dialogs;

namespace FATXTools.Utilities
{
    public class TaskRunner
    {
        Task _task;

        private static TaskRunner _instance;

        TaskRunner()
        {
            _task = null;
        }

        public static TaskRunner Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new TaskRunner();

                return _instance;
            }
        }

        public async Task RunTaskAsync(Form owner, TaskDialogOptions options, 
            Action<CancellationToken, IProgress<ValueTuple<int, string>>> action)
        {
            if (_task != null) 
                throw new Exception("A task is already running.");

            using (var cancellationTokenSource = new CancellationTokenSource())
            {
                var cancellationToken = cancellationTokenSource.Token;

                var dialog = new TaskDialog(owner, options, ref _task, cancellationTokenSource);
                dialog.Show();

                _task = Task.Run(() => action(cancellationToken, dialog.Progress), cancellationToken);

                await _task;

                dialog.Close();

                SystemSounds.Beep.Play();

                _task = null;
            }
        }
    }
}
