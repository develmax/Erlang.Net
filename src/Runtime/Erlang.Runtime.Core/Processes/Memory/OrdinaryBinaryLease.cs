namespace Erlang;

internal sealed class OrdinaryBinaryLease(OrdinaryBinaryBacking owner) : IDisposable
{
    private OrdinaryBinaryBacking? retained;

    internal void Admit() => retained = owner;

    public void Dispose() => Interlocked.Exchange(ref retained, null)?.Release();
}
