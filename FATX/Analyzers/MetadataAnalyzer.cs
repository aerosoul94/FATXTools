using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

using FATX.FileSystem;

namespace FATX.Analyzers
{
    public class MetadataAnalyzer : IAnalyzer<List<DirectoryEntry>>
    {
        readonly long _interval;

        public string Name => "Metadata Analyzer";
        public Volume Volume { get; private set; }
        public List<DirectoryEntry> Results { get; } = new List<DirectoryEntry>();

        public MetadataAnalyzer(Volume volume, long interval)
        {
            Volume = volume;

            _interval = interval;
        }

        public List<DirectoryEntry> Analyze(CancellationToken cancellationToken, IProgress<(int, string)> progress)
        {
            var stopWatch = new Stopwatch();
            stopWatch.Start();

            RecoverMetadata(cancellationToken, progress);

            stopWatch.Stop();

            Console.WriteLine($"Execution Time: {stopWatch.ElapsedMilliseconds} ms");
            Console.WriteLine($"Found {Results.Count} dirents.");

            return Results;
        }

        private void RecoverMetadata(CancellationToken cancellationToken, IProgress<(int, string)> progress)
        {
            var validator = new DirectoryEntryValidator((int)Volume.MaxClusters, DateTime.Now.Year);
            var clusterReader = Volume.ClusterReader;

            for (uint cluster = 1; cluster < Volume.MaxClusters; cluster++)
            {
                var data = clusterReader.ReadCluster(cluster);
                var clusterOffset = (cluster - 1) * _interval;

                for (var offset = 0; offset < 256 * 0x40; offset += 0x40)
                {
                    try
                    {
                        // TODO: May be able to improve performance by doing doing simple preliminary checks
                        //   to avoid creating the DirectoryEntry object.

                        DirectoryEntry dirent = new DirectoryEntry(Volume.Platform, data, offset);

                        if (validator.IsValidDirent(dirent))
                        {
                            Console.WriteLine($"0x{clusterOffset + offset:X8}: {dirent.FileName}");

                            dirent.Cluster = cluster;
                            dirent.Offset = clusterReader.ClusterToPhysicalOffset(cluster) + offset;

                            Results.Add(dirent);
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.Message);
                        Console.WriteLine(e.StackTrace);
                    }
                }

                if (cluster % 0x100 == 0)
                {
                    var p = (int)(((float)cluster / (float)Volume.MaxClusters) * 100);
                    progress?.Report((p, $"Analyzing cluster {cluster} of {Volume.MaxClusters} ({p}%): Found {Results.Count} dirents."));
                }

                if (cancellationToken.IsCancellationRequested)
                {
                    // NOTE: even though this part of the analyzer returns,
                    //   it will continue on with the linking step to clean
                    //   up the results while keeping the progress bar
                    //   running to show that we are still working.
                    break;
                }
            }

            progress?.Report((100, "Finished"));
        }
    }
}