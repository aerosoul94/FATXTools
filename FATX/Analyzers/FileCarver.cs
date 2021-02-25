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

        public List<CarvedFile> Analyze(CancellationToken cancellationToken, IProgress<int> progress)
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
                    progress?.Report((int)(progressValue / interval));

                if (cancellationToken.IsCancellationRequested)
                    return Results;
            }

            progress?.Report((int)(Volume.FileAreaLength / interval));
            Console.WriteLine("Finished!");
            return Results;
        }

        // TODO: Get rid of this!
        public void LoadFromDatabase(JsonElement fileCarverList)
        {
            Results.Clear();

            foreach (var file in fileCarverList.EnumerateArray())
            {
                JsonElement offsetElement;
                if (!file.TryGetProperty("Offset", out offsetElement))
                {
                    Console.WriteLine("Failed to load signature from database: Missing offset field");
                    continue;
                }

                var carvedFile = new CarvedFile(offsetElement.GetInt64(), "");

                if (file.TryGetProperty("Name", out var nameElement))
                {
                    carvedFile.FileName = nameElement.GetString();
                }

                if (file.TryGetProperty("Size", out var sizeElement))
                {
                    carvedFile.FileSize = sizeElement.GetInt64();
                }

                Results.Add(carvedFile);
            }
        }
    }
}