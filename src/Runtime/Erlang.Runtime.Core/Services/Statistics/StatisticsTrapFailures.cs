using System.Runtime.ExceptionServices;

namespace Erlang;

internal sealed class StatisticsTrapFailures
{
    private readonly HashSet<Exception> seen = new(ReferenceEqualityComparer.Instance);
    private readonly List<ExceptionDispatchInfo> failures = new();

    internal void Add(Exception error)
    {
        if (seen.Add(error))
            failures.Add(ExceptionDispatchInfo.Capture(error));
    }

    internal void ThrowIfAny()
    {
        if (failures.Count == 1)
            failures[0].Throw();
        if (failures.Count > 1)
            throw new AggregateException(failures.Select(error => error.SourceException));
    }
}
