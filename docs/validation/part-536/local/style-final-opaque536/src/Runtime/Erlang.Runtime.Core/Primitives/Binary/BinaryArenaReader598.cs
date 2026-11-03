namespace Erlang;

internal sealed class BinaryArenaReader598(BinaryProgramBacking598 owner) : IDisposable
{
    private BinaryProgramBacking598? backing = owner;

    internal void Require598(BinaryProgramArena598 arena)
    {
        BinaryProgramBacking598 current = Volatile.Read(ref backing) ?? throw new ObjectDisposedException(nameof(BinaryArenaReader598));
        if (!ReferenceEquals(current.Arena598, arena)) throw new InvalidOperationException(BinaryArenaNames598.Invalid);
    }

    public void Dispose() => Interlocked.Exchange(ref backing, null)?.ReleaseReader598();
}
