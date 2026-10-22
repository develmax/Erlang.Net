namespace Erlang;

internal sealed class AtomicsArrayLease413(AtomicsArray413 array) : IDisposable
{
    private AtomicsArray413? current = array;
    internal AtomicsArray413 Value => current ?? throw new ObjectDisposedException(nameof(AtomicsArrayLease413));

    public void Dispose() => Interlocked.Exchange(ref current, null)?.Release();
}
