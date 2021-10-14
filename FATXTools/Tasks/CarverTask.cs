using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

using FATX.Analyzers;
using FATX.FileSystem;

using FATXTools.Utilities;

namespace FATXTools.Tasks
{
    public class CarverTask
    {
        private Volume _volume;
        private CancellationToken _cancellationToken;
        private IProgress<(int, string)> _progress;

        public CarverTask(Volume volume, CancellationToken cancellationToken, IProgress<(int, string)> progress)
        {
            _volume = volume;
            _cancellationToken = cancellationToken;
            _progress = progress;
        }

        public static Action<CancellationToken, IProgress<(int, string)>> RunSaveAllTask(
            Volume volume, string path, List<CarvedFile> files)
        {
            return (cancellationToken, progress) =>
            {
                var task = new CarverTask(volume, cancellationToken, progress);

                task.SaveAll(path, files);
            };
        }

        public static Action<CancellationToken, IProgress<(int, string)>> RunSaveTask(
            Volume volume, string path, CarvedFile file)
        {
            return (cancellationToken, progress) =>
            {
                var task = new CarverTask(volume, cancellationToken, progress);

                task.Save(path, file);
            };
        }

        public void SaveAll(string path, List<CarvedFile> files)
        {
            var i = 1;

            foreach (var file in files)
            {
                WriteFile(path, file);

                ReportProgress(i++, files.Count, file);
            }
        }

        public void Save(string path, CarvedFile file)
        {
            WriteFile(path, file);
        }

        private void WriteFile(string path, CarvedFile file)
        {
            const int bufsize = 0x100000;
            var remains = file.FileSize;
            _volume.FileAreaStream.Seek(file.Offset, SeekOrigin.Begin);

            path = path + "/" + file.FileName;
            var uniquePath = Utility.UniqueFileName(path);

            using (FileStream stream = new FileStream(uniquePath, FileMode.Create))
            {
                while (remains > 0)
                {
                    var read = Math.Min(remains, bufsize);
                    remains -= read;
                    byte[] buf = new byte[read];
                    _volume.FileAreaStream.Read(buf, 0, (int)read);
                    stream.Write(buf, 0, (int)read);
                }
            }
        }

        private void ReportProgress(int progress, int total, CarvedFile currentFile)
        {
            var percent = (int)(((float)progress / (float)total) * 100);
            _progress.Report((percent, $"{progress}/{total}: {currentFile.FileName}"));
        }
    }
}
