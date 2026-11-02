namespace Erlang;

// Synchronous exact-output seam only. Never store this scope in a queue or
// carry it across await. Existing ProcessTracing selection/matchers run first.
internal sealed class ReadonlyTraceOutputScope817 : IDisposable
{
    [ThreadStatic]
    private static ReadonlyTraceOutputScope817? current;
    private readonly ReadonlyTraceOutputScope817? previous;
    private readonly ProcessTracing owner;
    private readonly ReadonlyTraceOutput817 output;
    private bool consumed;
    private bool disposed;

    internal ReadonlyTraceOutputScope817(ProcessTracing owner, ReadonlyTraceOutput817 output)
    {
        this.owner = owner;
        this.output = output;
        previous = current;
        current = this;
    }

    internal bool Consumed817 => consumed;
    internal static bool HasActiveScope817 => current is { disposed: false };

    internal static bool TryPublish(
        ProcessTracing owner,
        Pid tracee,
        ProcessTraceSettings settings,
        IReadOnlyList<Term> details,
        Term? matchValue
    )
    {
        ReadonlyTraceOutputScope817? scope = current;
        if (scope is null || scope.disposed || scope.consumed || !ReferenceEquals(scope.owner, owner)
            || !scope.output.MatchesSelected817(
                tracee,
                settings,
                details,
                matchValue
            )) return false;
        scope.consumed = true;
        _ = scope.output.PublishSelected();

        return true;
    }

    public void Dispose()
    {
        if (disposed) return;
        if (!ReferenceEquals(current, this)) throw new InvalidOperationException(ReadonlyTrace817Names.Invalid);
        current = previous;
        disposed = true;
    }
}
