namespace Erlang;

internal sealed class CountersArrayLease421(CountersArray421 array) : IDisposable
{
    private CountersArray421? current = array;
    internal CountersArray421 Value => current ?? throw new ObjectDisposedException(nameof(CountersArrayLease421));

    public void Dispose() => Interlocked.Exchange(ref current, null)?.Release();
}
