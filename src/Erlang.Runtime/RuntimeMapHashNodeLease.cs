namespace Erlang;

internal sealed class RuntimeMapHashNodeLease(ulong word, Action release) : IDisposable
{
    private Action? pending = release;
    internal ulong Word { get; } = word;

    public void Dispose() => Interlocked.Exchange(ref pending, null)?.Invoke();
}
