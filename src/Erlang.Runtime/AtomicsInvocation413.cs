namespace Erlang;

internal sealed class AtomicsInvocation413(RuntimeAtomicsOwner413 owner) : IDisposable
{
    private RuntimeAtomicsOwner413? current = owner;

    public void Dispose() => Interlocked.Exchange(ref current, null)?.Leave();
}
