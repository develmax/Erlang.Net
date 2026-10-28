namespace Erlang.Tests;

internal sealed class NifPhysicalCustody521TestLease(bool fail) : IDisposable
{
    internal int Disposals { get; private set; }

    public void Dispose()
    {
        Disposals++;
        if (fail)
            throw new IOException(nameof(NifPhysicalCustody521TestLease));
    }
}
