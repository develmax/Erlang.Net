namespace Erlang;

internal sealed class BinaryProgramOperation598(BinaryProgramOwner598 owner) : IDisposable
{
    private BinaryProgramOwner598? current = owner;

    public void Dispose() => Interlocked.Exchange(ref current, null)?.LeaveBuild598();
}
