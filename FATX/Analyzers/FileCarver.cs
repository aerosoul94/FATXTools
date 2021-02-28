using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;

using FATX.Analyzers.Signatures;
using FATX.FileSystem;
using FATX.Streams;

namespace FATX.Analyzers
{
    public class FileCarver : IAnalyzer<List<CarvedFile>>
    {
        FileCarverInterval _interval;

        public string Name => "File Carver";
        public Volume Volume { get; private set; }
        public List<CarvedFile> Results { get; } = new List<CarvedFile>();

        public FileCarver(Volume volume, FileCarverInterval interval)
        {
            Volume = volume;
            _interval = interval;
        }

        public List<CarvedFile> Analyze(CancellationToken cancellationToken, IProgress<(int, string)> progress)
        {
            int interval = (int)_interval;
            long progressValue = 0;
            long progressUpdate = interval * 0x200;

            ByteOrder byteOrder = Volume.Platform == Platform.Xbox ? ByteOrder.Little : ByteOrder.Big;
            SignatureMatcher scanner = new SignatureMatcher(Volume.FileAreaStream, byteOrder, interval);

            for (long offset = 0; offset < Volume.FileAreaLength; offset += interval)
            {
                CarvedFile carvedFile;

                scanner.Match(out carvedFile);

                if (carvedFile != null)
                {
                    Results.Add(carvedFile);
                }

                progressValue += interval;
                if (progressValue % progressUpdate == 0)
                {
                    var p = (int)(((float)offset / (float)Volume.FileAreaLength) * 100);
                    progress?.Report((p, $"Analyzing offset {offset} ({p}%)): Found {Results.Count} files"));
                }

                if (cancellationToken.IsCancellationRequested)
                    return Results;
            }

            progress?.Report((100, "Finished"));
            Console.WriteLine("Finished!");
            return Results;
        }
    }
}