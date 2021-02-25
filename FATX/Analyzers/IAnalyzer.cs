using System;
using System.Threading;

using FATX.FileSystem;

namespace FATX.Analyzers
{
    public interface IAnalyzer<T>
    {
        Volume Volume { get; }
        string Name { get; }
        T Results { get; }
        T Analyze(CancellationToken cancellationToken, IProgress<int> progress);
    }
}