using Erlang;

namespace NifPrototype;

internal sealed class NifGenerationLease(NifGenerationLeaseTable table, ModuleCodeImage image) : IDisposable
{
    private NifGenerationLeaseTable? owner = table;

    public void Dispose() => Interlocked.Exchange(ref owner, null)?.Release(image);
}
