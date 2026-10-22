namespace Erlang;

public readonly struct TermMapComparisonScope : IDisposable
{
    private readonly Func<ITermMapComparisonLayout>? previous;
    private readonly int thread;

    internal TermMapComparisonScope(Func<ITermMapComparisonLayout>? previous, int thread)
    {
        this.previous = previous;
        this.thread = thread;
    }

    public void Dispose()
    {
        if (thread == 0)
            return;
        if (thread != Environment.CurrentManagedThreadId)
            throw new InvalidOperationException(TermMapComparisonNames.ThreadChanged);
        TermMapComparisonEnvironment.Restore(previous);
    }
}
