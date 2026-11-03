namespace Erlang;

internal sealed class BinaryProgramHost598 : IDisposable
{
    private BinaryProgramLease598? lease;

    internal BinaryProgramHost598(BinaryProgramBacking598 backing) => lease = backing.Retain598(BinaryProgramLeaseKind598.Host);

    internal bool Owns598(BinaryProgramBacking598 backing) => Volatile.Read(ref lease)?.Owns598(backing) == true;

    public void Dispose()
    {
        Interlocked.Exchange(ref lease, null)?.Dispose();
        GC.SuppressFinalize(this);
    }

    ~BinaryProgramHost598()
    {
        try
        {
            Dispose();
        }
        catch { /* Runtime owner retains failure evidence. */ }
    }
}
